using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WinFormsToAvalonia.Core.Semantics;

/// <summary>
/// One Form's (or UserControl's) class, as the semantic model sees it: what its fields, helper
/// signatures and method bodies mean once WinForms is taken away.
/// </summary>
/// <remarks>
/// Members are found by name in the class and in the project's own base Forms - the same place
/// visual inheritance folds them in from (<c>CodeBehindModel.WithInherited</c>). An overloaded
/// name is not resolved: every lookup here answers "no" rather than guess which one was meant.
/// </remarks>
public sealed class SemanticClassScope
{
    private readonly SemanticProjectContext _project;
    private readonly INamedTypeSymbol _owner;

    internal SemanticClassScope(SemanticProjectContext project, INamedTypeSymbol owner)
    {
        _project = project;
        _owner = owner;
    }

    /// <summary>
    /// A field of the Form whose type - and initializer, if it has one - is UI-neutral, rendered
    /// for the generated View with every non-implicit type namespace-qualified.
    /// </summary>
    public bool TryRenderField(string fieldName, out string typeText, out string? initializerText)
    {
        typeText = "";
        initializerText = null;

        if (FindSingle<IFieldSymbol>(fieldName) is not { } field
            || field.DeclaringSyntaxReferences is not [{ } reference]
            || reference.GetSyntax() is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } declarator)
        {
            return false;
        }

        var checker = CreateChecker(declarator.SyntaxTree, _ => false, refuseMethodParameters: false);
        if (!checker.TryRender(declaration.Type, out typeText))
        {
            return false;
        }

        if (declarator.Initializer is { } initializer)
        {
            if (!checker.TryRender(initializer.Value, out var rendered))
            {
                return false;
            }

            initializerText = rendered;
        }

        return true;
    }

    /// <summary>
    /// A helper method's return type and parameter list, rendered for the generated View - when
    /// every type in them is UI-neutral.
    /// </summary>
    public bool TryRenderSignature(string methodName, out string returnTypeText, out string parameterListText)
    {
        returnTypeText = "";
        parameterListText = "";

        if (FindMethodDeclaration(methodName) is not { } declaration)
        {
            return false;
        }

        var checker = CreateChecker(declaration.SyntaxTree, _ => false, refuseMethodParameters: false);
        return checker.TryRender(declaration.ReturnType, out returnTypeText)
            && checker.TryRender(declaration.ParameterList, out parameterListText);
    }

    /// <summary>
    /// Binds a freshly parsed copy of a method's body in the method's own context, so that the
    /// rewriter, which works on that copy, can ask what any node of it means.
    /// </summary>
    /// <param name="freshBody">The block the rewriter parsed - the method's body text, dedented.</param>
    /// <param name="isHandler">
    /// True for an event handler, whose parameters are Avalonia objects in the generated method.
    /// </param>
    /// <returns>Null when the method cannot be found unambiguously, or the body does not bind.</returns>
    public BoundMethodBody? BindBody(string methodName, BlockSyntax freshBody, bool isHandler)
    {
        BaseMethodDeclarationSyntax? declaration = methodName == ConstructorName
            ? FindSoleConstructor()
            : FindMethodDeclaration(methodName);

        if (declaration is not { Body: { } originalBody } || _project.Compilation is not { } compilation)
        {
            return null;
        }

        var replica = declaration
            .WithBody(freshBody)
            .WithExpressionBody(null)
            .WithSemicolonToken(default);

        var model = compilation.GetSemanticModel(declaration.SyntaxTree);
        // The replica is a tree of its own; the speculative model answers for *its* nodes.
        if (!model.TryGetSpeculativeSemanticModelForMethodBody(originalBody.SpanStart, replica, out var speculative)
            || replica.Body is not { } boundBody)
        {
            return null;
        }

        return new BoundMethodBody(this, speculative, boundBody, isHandler);
    }

    /// <summary>What <see cref="BindBody"/> takes as the method name to bind the class's constructor.</summary>
    public const string ConstructorName = ".ctor";

    /// <summary>The class's only instance constructor, when it is parameterless - the one the generated View has.</summary>
    private ConstructorDeclarationSyntax? FindSoleConstructor() =>
        _owner.InstanceConstructors is [{ Parameters.Length: 0, DeclaringSyntaxReferences: [{ } reference] }]
            && reference.GetSyntax() is ConstructorDeclarationSyntax constructor
            ? constructor
            : null;

    internal NeutralityChecker CreateChecker(
        SemanticModel model,
        Func<ISymbol, bool> ownMemberAllowed,
        bool refuseMethodParameters,
        Func<ILocalSymbol, SpecialType>? vouchedLocalType = null) =>
        new(model, _owner, ownMemberAllowed, refuseMethodParameters, _project.IsCarried, _project.GeneratedNamespacesInScope)
        {
            VouchedLocalType = vouchedLocalType,
        };

    private NeutralityChecker CreateChecker(SyntaxTree tree, Func<ISymbol, bool> ownMemberAllowed, bool refuseMethodParameters) =>
        CreateChecker(_project.Compilation!.GetSemanticModel(tree), ownMemberAllowed, refuseMethodParameters);

    private MethodDeclarationSyntax? FindMethodDeclaration(string methodName) =>
        FindSingle<IMethodSymbol>(methodName) is { MethodKind: MethodKind.Ordinary, DeclaringSyntaxReferences: [{ } reference] }
            && reference.GetSyntax() is MethodDeclarationSyntax declaration
            ? declaration
            : null;

    /// <summary>The one member of that name and kind on the class or its own base Forms, nearest first.</summary>
    private T? FindSingle<T>(string name) where T : class, ISymbol
    {
        for (INamedTypeSymbol? current = _owner; current is not null && current.TypeKind != TypeKind.Error; current = current.BaseType)
        {
            var members = current.GetMembers(name);
            if (members.IsEmpty)
            {
                continue;
            }

            return members is [T single] ? single : null;
        }

        return null;
    }
}

/// <summary>
/// A method body bound by <see cref="SemanticClassScope.BindBody"/>: the block the rewriter
/// iterates over, and the model that knows what each of its nodes means.
/// </summary>
public sealed class BoundMethodBody
{
    private readonly SemanticClassScope _scope;
    private readonly SemanticModel _model;
    private readonly bool _isHandler;

    internal BoundMethodBody(SemanticClassScope scope, SemanticModel model, BlockSyntax block, bool isHandler)
    {
        _scope = scope;
        _model = model;
        Block = block;
        _isHandler = isHandler;
    }

    /// <summary>The bound copy of the body. Same text as the one parsed; its nodes are the ones the model answers for.</summary>
    public BlockSyntax Block { get; }

    /// <summary>
    /// The node verbatim, type references qualified, when it is UI-neutral - see
    /// <see cref="NeutralityChecker"/>.
    /// </summary>
    /// <param name="ownMemberAllowed">
    /// Whether a member of the Form this body names exists on the generated View: a carried field,
    /// a helper that became real code. Asked per symbol, so it can say "a helper, but only a
    /// synchronous one".
    /// </param>
    /// <param name="vouchedLocalType">See <see cref="NeutralityChecker.VouchedLocalType"/>.</param>
    public bool TryPassThrough(
        SyntaxNode node,
        Func<ISymbol, bool> ownMemberAllowed,
        out string text,
        out bool usesAwait,
        Func<ILocalSymbol, SpecialType>? vouchedLocalType = null)
    {
        var checker = _scope.CreateChecker(_model, ownMemberAllowed, _isHandler, vouchedLocalType);
        var rendered = checker.TryRender(node, out text);
        usesAwait = checker.UsesAwait;
        LastProblem = checker.FirstProblem;
        return rendered;
    }

    /// <summary>
    /// Whether <paramref name="expression"/> is a value WinForms' complex data binding would read as
    /// a plain list of rows - and so one an Avalonia <c>ItemsSource</c> shows the same way.
    /// </summary>
    /// <remarks>
    /// WinForms accepts an <c>IList</c> or an <c>IListSource</c> and nothing else (a bare
    /// <c>IEnumerable</c> throws). An <c>IListSource</c> - a <c>DataTable</c>, a <c>DataSet</c> - is
    /// read through <c>GetList()</c> and a property descriptor per column, which Avalonia's grid
    /// has no equivalent of, so it is refused here, and so is anything from <c>System.Data</c>
    /// (a <c>DataView</c> is an <c>IList</c> of <c>DataRowView</c>s, whose columns are descriptors too).
    /// A <c>string</c> is an <c>IEnumerable</c> of characters, not a list of rows.
    /// </remarks>
    public bool IsPlainListDataSource(ExpressionSyntax expression)
    {
        if (expression.SyntaxTree != Block.SyntaxTree
            || _model.GetTypeInfo(expression).Type is not { TypeKind: not TypeKind.Error } type
            || type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        static bool IsNamed(ITypeSymbol symbol, string ns, string name) =>
            symbol.Name == name && symbol.ContainingNamespace?.ToDisplayString() == ns;

        static bool InSystemData(ITypeSymbol symbol) =>
            symbol.ContainingNamespace?.ToDisplayString() is { } ns
            && (ns == "System.Data" || ns.StartsWith("System.Data.", StringComparison.Ordinal));

        var self = new[] { type };
        var all = self.Concat(type.AllInterfaces).ToList();
        return all.Any(t => IsNamed(t, "System.Collections", "IList"))
            && !all.Any(t => IsNamed(t, "System.ComponentModel", "IListSource"))
            && !InSystemData(type)
            && !(type is IArrayTypeSymbol array && InSystemData(array.ElementType));
    }

    /// <summary>The part of the last node <see cref="TryPassThrough"/> refused that made it refuse.</summary>
    public string? LastProblem { get; private set; }

    /// <summary>The names a passed-through statement declares into the enclosing scope.</summary>
    public static IEnumerable<string> DeclaredNames(SyntaxNode statement) =>
        statement.DescendantNodesAndSelf(n => n is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
            .Select(n => n switch
            {
                VariableDeclaratorSyntax declarator => declarator.Identifier.ValueText,
                SingleVariableDesignationSyntax designation => designation.Identifier.ValueText,
                LocalFunctionStatementSyntax function => function.Identifier.ValueText,
                _ => null,
            })
            .OfType<string>();
}
