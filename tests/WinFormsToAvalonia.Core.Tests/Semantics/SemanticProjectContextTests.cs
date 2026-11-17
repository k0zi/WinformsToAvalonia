using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Parsing;
using WinFormsToAvalonia.Core.Semantics;
using WinFormsToAvalonia.Core.Tests.TestSupport;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Semantics;

public class SemanticProjectContextTests
{
    private const string Csproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net9.0-windows</TargetFramework>
            <UseWindowsForms>true</UseWindowsForms>
          </PropertyGroup>
        </Project>
        """;

    private const string FormSource = """
        using System;
        using System.Collections.Generic;
        using System.Windows.Forms;
        using App.Models;

        namespace App
        {
            public partial class MainForm : Form
            {
                private int counter;
                private Customer current;

                public MainForm() { InitializeComponent(); }

                private void Handler(object sender, EventArgs e)
                {
                }
            }
        }
        """;

    private static SemanticProjectContext Build(TempProjectFixture fixture, params (string Path, string Text)[] files)
    {
        fixture.WriteFile("App.csproj", Csproj);
        foreach (var (path, text) in files)
        {
            fixture.WriteFile(path, text);
        }

        var project = new WinFormsProjectLoader().Load(fixture.PathTo("App.csproj"));
        return SemanticProjectContext.Build(project, "Out", new HashSet<string>(StringComparer.Ordinal) { "App.MainForm" });
    }

    [Fact]
    public void PlainFile_IsCarried_WithoutItsWinFormsUsing_AndUnderNullableDisable()
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(fixture, ("Models/Customer.cs", """
            using System;
            using System.Windows.Forms;

            namespace App.Models
            {
                public class Customer
                {
                    public string Name { get; set; }
                }
            }
            """));

        var carried = Assert.Single(context.CarriedFiles);
        Assert.Equal("Models/Customer.cs", carried.RelativePath);
        Assert.StartsWith("#nullable disable", carried.Text);
        Assert.DoesNotContain("System.Windows.Forms", carried.Text);
        Assert.Contains("public string Name { get; set; }", carried.Text);
    }

    [Theory]
    // System.Drawing binds - it is in the BCL - but means something Avalonia spells differently.
    [InlineData("public System.Drawing.Color Tint => System.Drawing.Color.Red;", "System.Drawing")]
    // Windows-only: a CA1416 warning in a project that has to build warning-free.
    [InlineData("public object Key => Microsoft.Win32.Registry.CurrentUser;", "Windows-only")]
    // Obsolete: SYSLIB0014.
    [InlineData("public object Client => new System.Net.WebClient();", "obsolete")]
    // WinForms itself does not bind at all.
    [InlineData("public System.Windows.Forms.Button Button;", "'Forms'")]
    public void FileUsingSomethingThatWouldNotMeanTheSame_IsNotCarried_AndIsReported(string member, string reason)
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(fixture, ("Services/Thing.cs", $$"""
            namespace App.Services
            {
                public class Thing
                {
                    {{member}}
                }
            }
            """));

        Assert.Empty(context.CarriedFiles);
        Assert.True(
            context.Warnings.Any(w => w.Contains("Services/Thing.cs") && w.Contains(reason, StringComparison.OrdinalIgnoreCase)),
            string.Join("\n", context.Warnings));
    }

    [Fact]
    public void FileUsingATypeThatWasNotCarried_IsNotCarriedEither()
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(
            fixture,
            ("A.cs", "namespace App { public class A { public System.Drawing.Color C; } }"),
            ("B.cs", "namespace App { public class B { public A Inner = new A(); } }"),
            ("C.cs", "namespace App { public class C { public int Value = 1; } }"));

        Assert.Equal(["C.cs"], context.CarriedFiles.Select(f => f.RelativePath));
    }

    [Fact]
    public void TheFormsOwnFiles_AndProgram_AndAssemblyInfo_AreNeverCarried()
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(
            fixture,
            ("MainForm.cs", FormSource),
            ("Program.cs", "namespace App { static class Program { static void Main() { } } }"),
            ("Properties/AssemblyInfo.cs", "[assembly: System.Reflection.AssemblyTitle(\"x\")]\nnamespace App { class Marker { } }"));

        Assert.Empty(context.CarriedFiles);
    }

    [Fact]
    public void ATypeWhoseNameTheGeneratedProjectUses_IsNotCarried()
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(fixture, ("ViewModels/Thing.cs", "namespace Out.ViewModels { public class Thing { } }"));

        Assert.Empty(context.CarriedFiles);
        Assert.Contains(context.Warnings, w => w.Contains("a name the generated project uses itself"));
    }

    [Theory]
    // Plain BCL: the System and implicit namespaces stay simple names.
    [InlineData("var list = new List<int> { 1, 2 }; int n = Math.Max(list.Count, 3);", true, "var list = new List<int> { 1, 2 }; int n = Math.Max(list.Count, 3);")]
    // A carried type is written namespace-qualified, wherever it appears.
    [InlineData("var c = new Customer(); var all = new List<Customer>();", true, "var c = new App.Models.Customer(); var all = new List<App.Models.Customer>();")]
    // Not an implicit namespace: qualified too, so it cannot bind to an Avalonia type of the same name.
    [InlineData("var sb = new System.Text.StringBuilder(); var c = System.Globalization.CultureInfo.InvariantCulture;", true, null)]
    // The handler's own parameters are Avalonia objects in the generated method.
    [InlineData("var s = sender.ToString();", false, null)]
    // `this` is the Form, and the generated View is a different object.
    [InlineData("var t = this.GetType();", false, null)]
    [InlineData("var t = GetType();", false, null)]
    // A field the View carries is fine; the predicate below allows `counter` and nothing else.
    [InlineData("counter++; this.counter += 2;", true, "counter++; this.counter += 2;")]
    [InlineData("current = null;", false, null)]
    // An object initializer names the members it sets - not a lone `this`, not a Form member.
    [InlineData("var c = new Customer { Id = 1 };", true, "var c = new App.Models.Customer { Id = 1 };")]
    // A local declared earlier in the same body is in scope.
    [InlineData("int x = 1; x = x + 1;", true, null)]
    public void BoundBody_PassesThroughOnlyWhatIsUiNeutral(string body, bool expected, string? expectedText)
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(
            fixture,
            ("MainForm.cs", FormSource),
            ("Models/Customer.cs", "namespace App.Models { public class Customer { public int Id { get; set; } } }"));

        var scope = context.ForClass("App.MainForm");
        Assert.NotNull(scope);

        var block = (BlockSyntax)SyntaxFactory.ParseStatement("{\n" + body + "\n}");
        var bound = scope.BindBody("Handler", block, isHandler: true);
        Assert.NotNull(bound);

        var texts = new List<string>();
        var allPassed = true;
        foreach (var statement in bound.Block.Statements)
        {
            if (!bound.TryPassThrough(statement, s => s.Name == "counter", out var text, out _))
            {
                allPassed = false;
                break;
            }

            texts.Add(text);
        }

        Assert.True(expected == allPassed, bound.LastProblem);
        if (expectedText is not null)
        {
            Assert.Equal(expectedText, string.Join(" ", texts));
        }

        if (body.Contains("StringBuilder", StringComparison.Ordinal))
        {
            Assert.Contains("new System.Text.StringBuilder()", texts[0]);
        }
    }

    [Theory]
    [InlineData("new List<Customer>()", true)]
    [InlineData("new Customer[0]", true)]
    [InlineData("new System.Collections.ObjectModel.ObservableCollection<Customer>()", true)]
    // WinForms refuses a bare IEnumerable; a string is a sequence of characters, not of rows.
    [InlineData("new List<Customer>().Where(c => c.Id > 0)", false)]
    [InlineData("\"abc\"", false)]
    // An IListSource is read through property descriptors Avalonia's grid has no counterpart for.
    [InlineData("new System.Data.DataTable()", false)]
    [InlineData("new System.Data.DataTable().DefaultView", false)]
    // A WinForms type binds to an error type.
    [InlineData("new BindingSource()", false)]
    public void IsPlainListDataSource_AcceptsOnlyAPlainIList(string expression, bool expected)
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(
            fixture,
            ("MainForm.cs", FormSource.Replace("using System.Collections.Generic;", "using System.Collections.Generic;\nusing System.Linq;")),
            ("Models/Customer.cs", "namespace App.Models { public class Customer { public int Id { get; set; } } }"));

        var block = (BlockSyntax)SyntaxFactory.ParseStatement("{\nvar x = " + expression + ";\n}");
        var bound = context.ForClass("App.MainForm")!.BindBody("Handler", block, isHandler: true)!;
        var value = bound.Block.DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value;

        Assert.Equal(expected, bound.IsPlainListDataSource(value));
    }

    [Theory]
    // A whole argument of a call that resolved, to a parameter an int converts to.
    [InlineData("board.Get(column, 1);", true)]
    [InlineData("board.Wide(column);", true)]
    [InlineData("var b = new Board(column);", true)]
    // Narrowing is not implicit; a string parameter takes no int.
    [InlineData("board.Narrow(column);", false)]
    [InlineData("board.Text(column);", false)]
    // Two overloads an error-typed argument fits equally: the model resolves neither.
    [InlineData("board.Over(column);", false)]
    // Anything but a plain argument: the value's type would decide what the expression means.
    [InlineData("var x = column + 1;", false)]
    [InlineData("board.Get(column + 1, 1);", false)]
    [InlineData("var s = column.ToString();", false)]
    // Not vouched for at all.
    [InlineData("board.Get(other, 1);", false)]
    public void VouchedLocal_PassesOnlyAsAPlainArgument(string statement, bool expected)
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(
            fixture,
            ("MainForm.cs", FormSource),
            ("Models/Customer.cs", """
                namespace App.Models
                {
                    public class Customer { }
                    public class Board
                    {
                        public Board() { }
                        public Board(int size) { }
                        public int Get(int c, int r) => c;
                        public void Wide(long a) { }
                        public void Narrow(short a) { }
                        public void Text(string a) { }
                        public void Over(int a) { }
                        public void Over(string a) { }
                    }
                }
                """));

        // `Unknown` binds to nothing - the WinForms call the rewriter translated.
        var block = (BlockSyntax)SyntaxFactory.ParseStatement(
            "{\nvar board = new Board();\nvar column = Unknown.GetColumn(this);\nvar other = Unknown.GetRow(this);\n" + statement + "\n}");
        var bound = context.ForClass("App.MainForm")!.BindBody("Handler", block, isHandler: true)!;

        var passed = bound.TryPassThrough(
            bound.Block.Statements[^1],
            _ => false,
            out _,
            out _,
            local => local.Name == "column" ? SpecialType.System_Int32 : SpecialType.None);

        Assert.True(expected == passed, bound.LastProblem);
    }

    [Fact]
    public void Field_IsRenderedWithItsTypeQualified_OnlyWhenTypeAndInitializerAreNeutral()
    {
        using var fixture = TempProjectFixture.Create();
        var context = Build(
            fixture,
            ("MainForm.cs", FormSource.Replace(
                "private Customer current;",
                "private Customer current;\n private List<Customer> all = new List<Customer>();\n private System.Drawing.Color tint;")),
            ("Models/Customer.cs", "namespace App.Models { public class Customer { } }"));

        var scope = context.ForClass("App.MainForm")!;

        Assert.True(scope.TryRenderField("current", out var type, out var initializer));
        Assert.Equal("App.Models.Customer", type);
        Assert.Null(initializer);

        Assert.True(scope.TryRenderField("all", out type, out initializer));
        Assert.Equal("List<App.Models.Customer>", type);
        Assert.Equal("new List<App.Models.Customer>()", initializer);

        Assert.False(scope.TryRenderField("tint", out _, out _));
    }

    [Fact]
    public void WithoutAnyCompilation_NothingIsKnown()
    {
        Assert.Empty(SemanticProjectContext.None.CarriedFiles);
        Assert.Null(SemanticProjectContext.None.ForClass("App.MainForm"));
    }
}
