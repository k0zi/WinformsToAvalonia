using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsToAvalonia.Core.Emission;
using WinFormsToAvalonia.Core.Mapping;
using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Parsing;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>
/// Translates the statements of one WinForms event handler into real Avalonia code.
/// </summary>
/// <remarks>
/// <para>
/// The rule is the project's usual one, applied a statement at a time: emit real code only for
/// shapes that are <em>provably</em> equivalent, and leave everything else in the comment block
/// it lives in today. That is why this recognizes a small, closed set of statement forms rather
/// than attempting general WinForms-to-Avalonia expression translation.
/// </para>
/// <para>
/// Migration stops at the <b>first</b> statement it cannot translate, and everything from there
/// on stays commented. Translating statement 1 and 3 while silently dropping statement 2 would
/// produce a method that looks migrated but quietly skips work; a prefix, by contrast, is a
/// faithful partial execution of the original, and <c>MigrationTodo.NotMigrated</c> still fires
/// to say the handler is unfinished.
/// </para>
/// <para>
/// Two targets, because the same body means different things in the two places a handler can
/// land: in a View, <c>this.counterLabel.Text</c> is still a control property (<c>x:Name</c>
/// generates the field); in a ViewModel there is no control at all, and the same expression is
/// the generated [ObservableProperty] the plan bound to it.
/// </para>
/// </remarks>
public sealed class HandlerBodyRewriter
{
    /// <summary>
    /// Types whose static members are plain .NET and compile unchanged in an Avalonia project.
    /// A conservative list on purpose: an unrecognized receiver stops the migration rather than
    /// risking a WinForms API that does not exist any more.
    /// </summary>
    private static readonly HashSet<string> SafeStaticReceivers = new(StringComparer.Ordinal)
    {
        "int", "Int32", "long", "Int64", "short", "Int16", "byte", "Byte",
        "double", "Double", "float", "Single", "decimal", "Decimal",
        "bool", "Boolean", "char", "Char", "string", "String",
        "Math", "Convert", "DateTime", "DateTimeOffset", "TimeSpan", "Guid", "Environment",

        // System.Threading: `Thread.Sleep` blocks the UI thread in Avalonia exactly as it did in
        // WinForms. Faithful, which is this converter's bar - not wise, which is not.
        "Thread",

        // System.IO: plain .NET, and exactly what a file-dialog handler reaches for next.
        "File", "Directory", "Path",
    };

    /// <summary>
    /// Enum types both frameworks declare, member for member, under the same name - so a value
    /// of one can be written through untranslated.
    /// </summary>
    /// <remarks>
    /// Only the members that exist on <em>both</em> sides are listed. WinForms'
    /// <c>DragDropEffects</c> also has <c>Scroll</c> and <c>All</c>, which Avalonia does not, and
    /// emitting one of those would be a compile error in the generated project rather than a
    /// wrong-but-compiling translation.
    /// </remarks>
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PassThroughEnums =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["DragDropEffects"] = new HashSet<string>(StringComparer.Ordinal) { "None", "Copy", "Move", "Link" },
        };

    /// <summary>
    /// The drag payload formats both frameworks name, across Avalonia 12's rework of them:
    /// <c>DataFormats.FileDrop</c> is <c>DataFormat.File</c>, and the class lost its plural.
    /// Kept here rather than in a Mapping table because exactly one call shape consults it.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> DataFormatNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["FileDrop"] = "File",
            ["Text"] = "Text",
        };

    private readonly ControlMappingRegistry _controlMappings;

    public HandlerBodyRewriter(ControlMappingRegistry controlMappings)
    {
        _controlMappings = controlMappings;
    }

    /// <summary>
    /// Rewrites for a View's code-behind, where the controls still exist as fields.
    /// </summary>
    public RewrittenBody RewriteForView(
        string body,
        FormModel formModel,
        ViewNavigationContext? navigation = null,
        HandlerSignature? signature = null,
        IReadOnlySet<string>? dispatcherTimerFields = null,
        IReadOnlySet<string>? componentFields = null,
        IReadOnlyDictionary<string, HelperCallInfo>? promotedHelpers = null,
        IReadOnlySet<string>? promotedFields = null,
        IReadOnlyDictionary<string, IReadOnlyList<ViewPropertyInfo>>? viewProperties = null,
        IReadOnlySet<string>? trayIconFields = null,
        IReadOnlyList<DataSourceBindingPlan>? dataSourceBindings = null,
        IReadOnlyList<ListViewRowsPlan>? listViewRows = null,
        IReadOnlyList<CheckedListPlan>? checkedLists = null) =>
        Rewrite(
            body,
            new ViewTarget(
                formModel,
                _controlMappings,
                navigation ?? ViewNavigationContext.None,
                signature ?? HandlerSignature.None,
                dispatcherTimerFields ?? new HashSet<string>(StringComparer.Ordinal),
                componentFields ?? new HashSet<string>(StringComparer.Ordinal),
                promotedHelpers ?? new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal),
                promotedFields ?? new HashSet<string>(StringComparer.Ordinal),
                viewProperties ?? new Dictionary<string, IReadOnlyList<ViewPropertyInfo>>(StringComparer.Ordinal),
                trayIconFields ?? new HashSet<string>(StringComparer.Ordinal),
                dataSourceBindings ?? [],
                listViewRows ?? [],
                checkedLists ?? []));

    /// <summary>
    /// Rewrites the body of a code-behind <em>helper</em> method, against the same View the
    /// handlers see - its parameters seeded as ordinary locals, and no sender/EventArgs, which a
    /// helper does not have.
    /// </summary>
    /// <remarks>
    /// The caller decides what to do with a partial result, and the answer is always "nothing":
    /// see <c>FormMigrationPlanner.PlanHelpers</c>. The prefix rule that makes a partly-migrated
    /// *handler* honest does not carry over to a helper, because the un-migrated remainder would
    /// end up nowhere near the call site.
    /// </remarks>
    public RewrittenBody RewriteForHelper(
        HelperMethodSignature signature,
        FormModel formModel,
        ViewNavigationContext navigation,
        IReadOnlySet<string> dispatcherTimerFields,
        IReadOnlySet<string> componentFields,
        IReadOnlyDictionary<string, HelperCallInfo> promotedHelpers,
        IReadOnlySet<string> promotedFields,
        IReadOnlyDictionary<string, IReadOnlyList<ViewPropertyInfo>>? viewProperties = null,
        IReadOnlySet<string>? trayIconFields = null,
        IReadOnlyList<DataSourceBindingPlan>? dataSourceBindings = null,
        IReadOnlyList<ListViewRowsPlan>? listViewRows = null,
        IReadOnlyList<CheckedListPlan>? checkedLists = null)
    {
        var target = new ViewTarget(
            formModel, _controlMappings, navigation, HandlerSignature.None,
            dispatcherTimerFields, componentFields, promotedHelpers, promotedFields,
            viewProperties ?? new Dictionary<string, IReadOnlyList<ViewPropertyInfo>>(StringComparer.Ordinal),
            trayIconFields ?? new HashSet<string>(StringComparer.Ordinal),
            dataSourceBindings ?? [],
            listViewRows ?? [],
            checkedLists ?? []);

        foreach (var parameterName in signature.ParameterNames)
        {
            target.Locals.Declare(parameterName, LocalKind.Value);
        }

        return Rewrite(signature.BodyText, target);
    }

    /// <summary>
    /// Rewrites for a promoted [RelayCommand], where every control property the body may touch
    /// has already been proved bindable and planned as an [ObservableProperty].
    /// </summary>
    public RewrittenBody RewriteForViewModel(
        string body,
        FormModel formModel,
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        IReadOnlyDictionary<string, HelperCallInfo>? promotedHelpers = null) =>
        Rewrite(body, new ViewModelTarget(
            boundProperties, formModel, promotedHelpers ?? new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal)));

    /// <summary>
    /// A helper method's body against a ViewModel - the shape needed when a promoted command
    /// calls it, since the helper has to move there too.
    /// </summary>
    public RewrittenBody RewriteForViewModelHelper(
        HelperMethodSignature signature,
        FormModel formModel,
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        IReadOnlyDictionary<string, HelperCallInfo> promotedHelpers)
    {
        var target = new ViewModelTarget(boundProperties, formModel, promotedHelpers);

        foreach (var parameterName in signature.ParameterNames)
        {
            target.Locals.Declare(parameterName, LocalKind.Value);
        }

        return Rewrite(signature.BodyText, target);
    }

    /// <summary>
    /// Translates a single *expression* against a ViewModel's properties - what a derived
    /// <c>CanExecute</c> guard needs, since it is a condition rather than a statement.
    /// </summary>
    public static bool TryRewriteConditionForViewModel(
        string expressionText,
        FormModel formModel,
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        out string rewritten)
    {
        var expression = SyntaxFactory.ParseExpression(expressionText);

        if (expression.ContainsDiagnostics)
        {
            rewritten = "";
            return false;
        }

        return TryRewriteExpression(
            expression,
            new ViewModelTarget(boundProperties, formModel, new Dictionary<string, HelperCallInfo>(StringComparer.Ordinal)),
            out rewritten);
    }

    private static RewrittenBody Rewrite(string body, IRewriteTarget target)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return RewrittenBody.NothingMigrated(body);
        }

        // The stored body is dedented and brace-stripped, so it parses as a block once wrapped.
        if (SyntaxFactory.ParseStatement("{\n" + body + "\n}") is not BlockSyntax block
            || block.ContainsDiagnostics)
        {
            return RewrittenBody.NothingMigrated(body);
        }

        var statements = block.Statements;

        // The confirm-on-close handler is a whole-body shape rather than a sequence of
        // statements, so it is matched before the loop that would otherwise refuse it.
        if (TryMatchCloseConfirmation(body, block, statements, target, out var confirmation))
        {
            return confirmation;
        }

        var migrated = new List<string>();
        var usings = new HashSet<string>(StringComparer.Ordinal);
        var fallbackKeys = new HashSet<string>(StringComparer.Ordinal);
        var requiresAsync = false;

        // A trailing `DialogResult = ...;` is how a hand-written dialog closes itself, and it is
        // one Avalonia statement even when the original spelled it as two. Matched off the end of
        // the body before the loop, so the loop never sees those statements at all.
        var hasDialogResultTail = TryMatchDialogResultTail(statements, target, out var tailCount, out var tailText);
        var translatableCount = statements.Count - (hasDialogResultTail ? tailCount : 0);

        var migratedCount = 0;
        for (var i = 0; i < translatableCount; i++)
        {
            var snapshot = target.Requirements.Snapshot();

            // A guard clause is matched off the body rather than through the statement grammar,
            // for the same reason the DialogResult tail above is: what it does - leave a value in
            // scope for everything after it - is a property of being at the top level, and the
            // grammar has no way to say that.
            if (!TryRewriteDialogGuard(statements[i], target, out var rewritten)
                && !TryRewriteStatement(statements[i], target, out rewritten))
            {
                // Undo anything a half-translated expression recorded, or the method could end up
                // `async` with nothing to await.
                target.Requirements.Restore(snapshot);
                break;
            }

            // An `async` statement in a handler whose result is read the moment it returns would
            // compile and quietly do nothing. Undone like any other untranslatable statement, so
            // the prefix before it still comes across.
            if (!target.AllowsAsync && (rewritten.RequiresAsync || target.Requirements.RequiresAsync))
            {
                target.Requirements.Restore(snapshot);
                break;
            }

            migrated.Add(rewritten.Text);
            usings.UnionWith(rewritten.RequiredUsings);
            fallbackKeys.UnionWith(rewritten.RequiredFallbackKeys);
            requiresAsync |= rewritten.RequiresAsync;
            migratedCount++;
        }

        // The tail closes the window, so it may only follow a *complete* prefix: appending it to
        // a partial one would close the dialog before the work the un-migrated statements
        // represent had a chance to be written in.
        if (hasDialogResultTail && migratedCount == translatableCount)
        {
            migrated.Add(tailText);
            migratedCount = statements.Count;
        }

        // A local left at the very end of a *partial* prefix can have no user: the statements that
        // would have used it are exactly the ones that did not translate. Dropping it keeps the
        // generated method free of dead declarations - and of CS0219, which a constant
        // initializer would raise in a project that builds warning-free.
        if (migratedCount < statements.Count)
        {
            while (migratedCount > 0 && statements[migratedCount - 1] is LocalDeclarationStatementSyntax)
            {
                migratedCount--;
                migrated.RemoveAt(migrated.Count - 1);
            }
        }

        if (migratedCount == 0)
        {
            return RewrittenBody.NothingMigrated(body, statements.Count);
        }

        // The untouched suffix, taken verbatim from the original text rather than from the
        // syntax tree, so comments and formatting inside it survive exactly as before.
        var remaining = migratedCount == statements.Count
            ? ""
            : RemainingText(body, block, statements[migratedCount]);

        usings.UnionWith(target.Requirements.RequiredUsings);
        fallbackKeys.UnionWith(target.Requirements.RequiredFallbackKeys);
        requiresAsync |= target.Requirements.RequiresAsync;

        return new RewrittenBody(
            // A statement can be *absorbed* rather than translated - `var b = (Button)sender;`
            // only renames a control this View already has a field for, and emits nothing. It is
            // carried through the loop as an empty entry so the list stays aligned with the
            // statements (the trailing-local trim above indexes into both), and dropped here.
            [.. migrated.Where(text => text.Length > 0)],
            remaining, statements.Count, usings, fallbackKeys, requiresAsync,
            target.Requirements.InlinedDialogFields);
    }

    /// <summary>The field name the close-confirmation template declares and reads.</summary>
    internal const string CloseGuardFieldName = "w2aForceClose";

    /// <summary>
    /// The generated View's typed field for its own ViewModel, for the handlers that populate a
    /// ViewModel collection. Named here rather than in the emitter for the same reason
    /// <see cref="CloseGuardFieldName"/> is: the rewriter writes the reference, the emitter writes
    /// the declaration, and one of them has to own the spelling.
    /// </summary>
    internal const string ViewModelFieldName = "w2aViewModel";

    /// <summary>
    /// <c>e.Cancel = MessageBox.Show(..., YesNo) == DialogResult.No;</c> - the canonical WinForms
    /// confirm-on-close - rewritten into the Avalonia shape for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one place the converter restructures a handler instead of translating it
    /// statement by statement, and it does so because there is no statement-level answer: Avalonia
    /// reads <c>e.Cancel</c> when the synchronous part of the handler returns - that is, at the
    /// first <c>await</c> - and there is no synchronous message box to ask before then. So the
    /// close is cancelled, the answer awaited, and on "yes" the window closed again from code,
    /// guarded by a field so the second pass falls straight through.
    /// </para>
    /// <para>
    /// What that changes is *when* the window closes - one turn of the loop later. What it does
    /// not change is anything else, and the shape is narrow enough to say so: the guard returns
    /// immediately on the second pass, so the statements around the confirmation still run exactly
    /// once per close attempt, in their original order, whether the user confirms or cancels.
    /// </para>
    /// <para>
    /// Recognised: any prefix of statements that translate without awaiting, then
    /// <c>e.Cancel = &lt;expr&gt;</c> - bare or as the single statement of an <c>if</c> with no
    /// <c>else</c> - whose expression translates and does await, then any tail that translates
    /// without awaiting. Anything else is not this shape and keeps refusing, which is what the
    /// narrowness buys: a handler that merely happens to await stays a comment rather than being
    /// restructured into something it never said.
    /// </para>
    /// </remarks>
    private static bool TryMatchCloseConfirmation(
        string body,
        BlockSyntax block,
        SyntaxList<StatementSyntax> statements,
        IRewriteTarget target,
        out RewrittenBody rewritten)
    {
        rewritten = null!;

        // Only where turning the handler async is what blocked it in the first place.
        if (target.AllowsAsync || statements.Count == 0)
        {
            return false;
        }

        var snapshot = target.Requirements.Snapshot();
        var usings = new HashSet<string>(StringComparer.Ordinal);
        var fallbackKeys = new HashSet<string>(StringComparer.Ordinal);

        // The confirmation is whichever statement is a cancel assignment; everything before it is
        // the prefix and everything after the tail.
        var index = 0;
        while (index < statements.Count && !IsCancelAssignment(statements[index], target))
        {
            index++;
        }

        if (index == statements.Count
            || !TryTranslatePlainStatements(statements.Take(index), target, usings, fallbackKeys, out var prefix))
        {
            target.Requirements.Restore(snapshot);
            return false;
        }

        var (condition, assignment) = statements[index] switch
        {
            IfStatementSyntax { Else: null, Statement: var branch } ifStatement
                => (ifStatement.Condition, SingleStatementOf(branch)),
            var plain => (null, plain),
        };

        if (assignment is not ExpressionStatementSyntax
            {
                Expression: AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax cancelTarget } cancel,
            }
            || !target.TryResolveEventArgsParameter(
                cancelTarget.Expression, "WindowClosingEventArgs", out var argsParameterName))
        {
            target.Requirements.Restore(snapshot);
            return false;
        }

        var conditionText = "";
        if (condition is not null && !TryRewriteExpression(condition, target, out conditionText))
        {
            target.Requirements.Restore(snapshot);
            return false;
        }

        var beforeCancel = target.Requirements.Snapshot();
        if (!TryRewriteExpression(cancel.Right, target, out var cancelText)
            || !target.Requirements.RequiresAsync)
        {
            // Nothing awaited: the ordinary statement loop handles this body perfectly well, and
            // restructuring it would only add a guard field nobody needs.
            target.Requirements.Restore(snapshot);
            return false;
        }

        _ = beforeCancel;

        // The tail runs on every close attempt, on both paths - so a statement in it that cannot
        // be translated does not take the whole shape with it. It goes into a local function that
        // both paths call, which is the only arrangement where a human fixing it fixes both.
        var tail = TranslatePrefix(
            statements.Skip(index + 1).ToList(), target, usings, fallbackKeys, out var firstUntranslated);

        var remainder = firstUntranslated is null
            ? null
            : new BodyRemainder(RemainderFunctionName, tail);
        var remainingBody = firstUntranslated is null ? "" : RemainingText(body, block, firstUntranslated);
        var tailStatements = remainder is null ? tail : [$"{RemainderFunctionName}();"];

        usings.UnionWith(target.Requirements.RequiredUsings);
        fallbackKeys.UnionWith(target.Requirements.RequiredFallbackKeys);

        var confirmationBody = new List<string>
        {
            $"// The confirmation was synchronous in WinForms. Avalonia reads e.Cancel when this",
            $"// method first awaits, so the close is cancelled, the answer awaited, and the window",
            $"// closed again from code - one turn of the loop later, and only if you said yes.",
            $"if ({CloseGuardFieldName})",
            "{",
            "    return;",
            "}",
            "",
        };

        confirmationBody.AddRange(prefix);

        var confirmed = new List<string>
        {
            $"{argsParameterName}.Cancel = true;",
            $"var w2aClosing = {Negate(cancelText)};",
        };
        confirmed.AddRange(tailStatements);
        confirmed.Add("if (w2aClosing)");
        confirmed.Add("{");
        confirmed.Add($"    {CloseGuardFieldName} = true;");
        confirmed.Add($"    {target.WindowMemberPrefix}Close();");
        confirmed.Add("}");

        if (condition is null)
        {
            confirmationBody.AddRange(confirmed);
        }
        else
        {
            confirmationBody.Add($"if ({conditionText})");
            confirmationBody.Add("{");
            confirmationBody.AddRange(confirmed.Select(line => "    " + line));
            confirmationBody.Add("");
            confirmationBody.Add("    return;");
            confirmationBody.Add("}");
            confirmationBody.Add("");
            confirmationBody.AddRange(tailStatements);
        }

        // One entry standing for the whole body, so the emitter indents it as the block it is -
        // and an explicit count, since the original's statements all came across.
        rewritten = new RewrittenBody(
            [string.Join("\n", confirmationBody)],
            remainingBody,
            statements.Count,
            usings,
            fallbackKeys,
            RequiresAsync: true,
            RequiresCloseGuard: true,
            // Everything the original said came across - what is left is inside the local
            // function, not a suffix the loop stopped before.
            MigratedStatementCountOverride: statements.Count,
            Remainder: remainder);
        return true;
    }

    /// <summary>
    /// The translated cancel expression says whether to *stay*; the template needs whether to
    /// close. An expression that is already a negation loses it rather than gaining a second one.
    /// </summary>
    private static string Negate(string expression) =>
        SyntaxFactory.ParseExpression(expression) is PrefixUnaryExpressionSyntax
        {
            RawKind: (int)SyntaxKind.LogicalNotExpression,
            Operand: var operand,
        }
            ? operand.ToString()
            : $"!({expression})";

    /// <summary>`e.Cancel = ...` on the handler's own EventArgs parameter.</summary>
    private static bool IsCancelAssignment(StatementSyntax statement, IRewriteTarget target)
    {
        var inner = statement switch
        {
            IfStatementSyntax { Else: null, Statement: var branch } => SingleStatementOf(branch),
            var plain => plain,
        };

        return inner is ExpressionStatementSyntax
            {
                Expression: AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
                    Left: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Cancel" } left,
                },
            }
            && target.TryResolveEventArgsParameter(left.Expression, "WindowClosingEventArgs", out _);
    }

    private static StatementSyntax? SingleStatementOf(StatementSyntax statement) =>
        statement switch
        {
            BlockSyntax { Statements: [var only] } => only,
            BlockSyntax => null,
            var single => single,
        };

    /// <summary>The local function a close confirmation's un-migrated tail lives in.</summary>
    /// <remarks>
    /// Prefixed like every other name this conversion invents, so it cannot collide with anything
    /// the original body declared.
    /// </remarks>
    private const string RemainderFunctionName = "w2aRemaining";

    /// <summary>
    /// Translates as far as it can and says where it stopped - the ordinary prefix rule, applied
    /// to the tail of a close confirmation rather than to a whole body.
    /// </summary>
    private static List<string> TranslatePrefix(
        IReadOnlyList<StatementSyntax> statements,
        IRewriteTarget target,
        HashSet<string> usings,
        HashSet<string> fallbackKeys,
        out StatementSyntax? firstUntranslated)
    {
        var translated = new List<string>();
        firstUntranslated = null;

        foreach (var statement in statements)
        {
            // As in TryTranslatePlainStatements: what matters is whether *this* statement awaits,
            // which is the difference across it - the confirmation has already set the flag.
            var snapshot = target.Requirements.Snapshot();
            var wasAsync = target.Requirements.RequiresAsync;
            if (!TryRewriteStatement(statement, target, out var rewritten)
                || rewritten.RequiresAsync
                || (target.Requirements.RequiresAsync && !wasAsync))
            {
                target.Requirements.Restore(snapshot);
                firstUntranslated = statement;
                return translated;
            }

            usings.UnionWith(rewritten.RequiredUsings);
            fallbackKeys.UnionWith(rewritten.RequiredFallbackKeys);
            if (rewritten.Text.Length > 0)
            {
                translated.Add(rewritten.Text);
            }
        }

        return translated;
    }

    /// <summary>
    /// Statements that translate and do <em>not</em> await - what may sit either side of the
    /// confirmation without changing when anything happens.
    /// </summary>
    private static bool TryTranslatePlainStatements(
        IEnumerable<StatementSyntax> statements,
        IRewriteTarget target,
        HashSet<string> usings,
        HashSet<string> fallbackKeys,
        out List<string> translated)
    {
        translated = [];

        foreach (var statement in statements)
        {
            // Requirements accumulate across the whole body, so "did *this* statement await?" is
            // the difference across it - not the flag's absolute value, which the confirmation
            // itself has already set by the time the tail is translated.
            var snapshot = target.Requirements.Snapshot();
            var wasAsync = target.Requirements.RequiresAsync;
            if (!TryRewriteStatement(statement, target, out var rewritten)
                || rewritten.RequiresAsync
                || (target.Requirements.RequiresAsync && !wasAsync))
            {
                target.Requirements.Restore(snapshot);
                return false;
            }

            usings.UnionWith(rewritten.RequiredUsings);
            fallbackKeys.UnionWith(rewritten.RequiredFallbackKeys);
            if (rewritten.Text.Length > 0)
            {
                translated.Add(rewritten.Text);
            }
        }

        return true;
    }

    /// <summary>
    /// The WinForms way a hand-written dialog reports its outcome: <c>DialogResult =
    /// DialogResult.OK;</c>, on its own or followed by <c>Close();</c>. Both spellings become the
    /// single <c>Close(true)</c> the caller's <c>ShowDialog&lt;bool&gt;</c> is waiting for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matching the <em>pair</em> is what makes this correct rather than convenient. Translated
    /// one statement at a time, the trailing <c>Close();</c> would come out as a bare
    /// <c>Close()</c> - which in Avalonia closes the window with <c>default(bool)</c> and so
    /// overwrites the result the line above had just set. The two statements are one act.
    /// </para>
    /// <para>
    /// Only at the very end of the body. In WinForms, assigning <c>DialogResult</c> on a modal
    /// form closes it but the handler keeps running; Avalonia's <c>Close</c> is the last thing
    /// that happens. Where the original has more to do afterwards those two are not equivalent,
    /// so the assignment simply fails to translate and the prefix stops there.
    /// </para>
    /// </remarks>
    private static bool TryMatchDialogResultTail(
        SyntaxList<StatementSyntax> statements, IRewriteTarget target, out int tailCount, out string tailText)
    {
        tailCount = 0;
        tailText = "";

        // A ViewModel has no window to close, and a converted UserControl cannot reach one.
        if (!target.AllowsWindowApis || !target.ReachesWindow || statements.Count == 0)
        {
            return false;
        }

        var index = statements.Count - 1;
        var closeStatements = 0;

        if (IsBareClose(statements[index]))
        {
            index--;
            closeStatements = 1;
        }

        if (index < 0
            || statements[index] is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
            || !assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken)
            || !IsFormDialogResult(assignment.Left)
            || assignment.Right is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "DialogResult" },
                Name.Identifier.ValueText: var resultName,
            }
            || !DialogResultCatalog.TryGetBool(resultName, out var accepted))
        {
            return false;
        }

        tailCount = 1 + closeStatements;
        tailText = $"Close({(accepted ? "true" : "false")});";
        return true;
    }

    /// <summary>The form's own <c>DialogResult</c> property, written bare or through `this`.</summary>
    private static bool IsFormDialogResult(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax { Identifier.ValueText: "DialogResult" } => true,
        MemberAccessExpressionSyntax
        {
            Expression: ThisExpressionSyntax,
            Name.Identifier.ValueText: "DialogResult",
        } => true,
        _ => false,
    };

    /// <summary>`Close();` / `this.Close();` with no arguments - the form closing itself.</summary>
    private static bool IsBareClose(StatementSyntax statement) =>
        statement is ExpressionStatementSyntax
        {
            Expression: InvocationExpressionSyntax
            {
                ArgumentList.Arguments.Count: 0,
                Expression: IdentifierNameSyntax { Identifier.ValueText: "Close" }
                    or MemberAccessExpressionSyntax
                    {
                        Expression: ThisExpressionSyntax,
                        Name.Identifier.ValueText: "Close",
                    },
            },
        };

    /// <summary>The original body text from the first un-migrated statement onwards.</summary>
    private static string RemainingText(string body, BlockSyntax block, StatementSyntax firstRemaining)
    {
        // ParseStatement was handed "{\n" + body + "\n}", so a span offset in the block maps back
        // to the body by subtracting that two-character prefix.
        const int prefixLength = 2;
        var start = firstRemaining.SpanStart - prefixLength;

        return start >= 0 && start < body.Length ? body[start..].TrimEnd() : body;
    }

    private static bool TryRewriteStatement(StatementSyntax statement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        switch (statement)
        {
            case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }:
                return TryRewriteAssignment(assignment, target, out rewritten);

            case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation }:
                return TryRewriteInvocation(invocation, target, out rewritten);

            // `i++;` on its own line, not just as a `for` incrementor.
            case ExpressionStatementSyntax { Expression: PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax } increment
                when TryRewriteIncrement(increment.Expression, target, out var incrementText):
                rewritten = new RewrittenStatement($"{incrementText};");
                return true;

            case IfStatementSyntax ifStatement:
                // The print-dialog shape is matched whole: there is no printer to choose, so the
                // dialog moves into the destination picker the call it guarded now performs.
                return TryRewritePrintDialogIf(ifStatement, target, out rewritten)
                    || TryRewriteIf(ifStatement, target, out rewritten);

            case BlockSyntax block:
                return TryRewriteBlock(block, target, out rewritten);

            case ForEachStatementSyntax forEachStatement:
                return TryRewriteForEach(forEachStatement, target, out rewritten);

            case ForStatementSyntax forStatement:
                return TryRewriteFor(forStatement, target, out rewritten);

            case WhileStatementSyntax whileStatement:
                return TryRewriteWhile(whileStatement, target, out rewritten);

            case LocalDeclarationStatementSyntax declaration:
                return TryRewriteLocalDeclaration(declaration, target, out rewritten);

            // `return;` in a void handler - an early exit with nothing to translate.
            case ReturnStatementSyntax { Expression: null }:
                rewritten = new RewrittenStatement("return;");
                return true;

            // `return count + " items";` - never seen in a handler, which is void, but it is how
            // a value-returning code-behind helper ends.
            case ReturnStatementSyntax { Expression: { } returned }
                when TryRewriteExpression(returned, target, out var returnedText):
                rewritten = new RewrittenStatement($"return {returnedText};");
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// An <c>if</c> whose condition and every branch translate. Braces are always emitted, even
    /// where the original had none, so a rewritten branch can never change what the `else` binds to.
    /// </summary>
    /// <summary>
    /// `if (openFileDialog1.ShowDialog(this) == DialogResult.OK) { ... openFileDialog1.FileName ... }`
    /// - the one translation in this rewriter that changes an expression's *shape* rather than
    /// re-spelling it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia has no dialog object to ask afterwards: the picker returns the selection itself.
    /// A list pattern keeps that inline - <c>is [var f, ..]</c> both tests for a selection and
    /// binds it - so no statement has to be inserted before the `if`, and the binding is scoped
    /// exactly to the branch that used to read the dialog's property.
    /// </para>
    /// <para>
    /// The picker is opened with default options: the designer's `Filter` string would have to be
    /// parsed into `FileTypeFilter` entries, and getting that subtly wrong is worse than leaving
    /// it out. Only the path property is translated, not `FileNames` (multi-select) or the
    /// dialogs with no Avalonia equivalent at all (Color/Font/Print).
    /// </para>
    /// </remarks>
    /// <summary>
    /// <c>dlg.ShowDialog() == DialogResult.OK</c>, in either operand order.
    /// </summary>
    /// <remarks>
    /// Extracted because both dialog families spelled the same eighteen lines out separately, and
    /// both accepted only one operand order - while the form-navigation matcher next door
    /// (<see cref="TryMatchDialogResultCondition"/>) has always read both. That asymmetry was
    /// forgetfulness rather than a decision: <c>DialogResult.OK == dlg.ShowDialog()</c> is
    /// textually different and means exactly the same thing.
    /// </remarks>
    /// <param name="expectedKind">
    /// <c>EqualsExpression</c> for the <c>if (ok) { ... }</c> shape,
    /// <c>NotEqualsExpression</c> for the guard clause that returns instead.
    /// </param>
    private static bool TryMatchShowDialogOkComparison(
        BinaryExpressionSyntax comparison, SyntaxKind expectedKind, out MemberAccessExpressionSyntax call)
    {
        call = null!;

        if (!comparison.IsKind(expectedKind))
        {
            return false;
        }

        var (dialogSide, resultSide) = IsDialogResultOk(comparison.Right)
            ? (comparison.Left, comparison.Right)
            : (comparison.Right, comparison.Left);

        if (!IsDialogResultOk(resultSide)
            || dialogSide is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ShowDialog" } showDialog,
            } invocation
            || invocation.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        call = showDialog;
        return true;
    }

    private static bool IsDialogResultOk(ExpressionSyntax expression) =>
        expression is MemberAccessExpressionSyntax
        {
            Expression: IdentifierNameSyntax { Identifier.ValueText: "DialogResult" },
            Name.Identifier.ValueText: "OK",
        };

    /// <summary>
    /// The guard-clause dialog shape: <c>if (dlg.ShowDialog() != DialogResult.OK) { return; }</c>
    /// followed by the rest of the handler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Equivalent for a reason worth stating, because the rest of this class refuses anything that
    /// needs a value to outlive the statement that produced it: C# definite assignment guarantees
    /// the picked value is assigned at every statement the guard falls through to, because the
    /// then-branch is an unconditional <c>return</c>. So <c>is not { } colour</c> followed by a
    /// return leaves <c>colour</c> usable for the remainder of the body, and the compiler is the
    /// thing enforcing it rather than this rewriter.
    /// </para>
    /// <para>
    /// Matched only at the top level of the body, and that is what keeps it small.
    /// <see cref="IRewriteTarget.DialogSelections"/> is otherwise scoped to a branch by an
    /// add/<c>finally</c>-remove pair; a guard needs the selection to live for the rest of the
    /// enclosing block, and at the top level "the rest of the block" is "the rest of the body", so
    /// the entry is simply added and never removed. Inside a nested block it would leak into
    /// statements that come after that block, so there it refuses.
    /// </para>
    /// <para>
    /// The then-branch must be exactly a bare <c>return;</c> - not a return with a value, not a
    /// return plus a log line, and no <c>else</c>. Anything more and the branch is doing work that
    /// would have to be translated too, at which point this is not a guard.
    /// </para>
    /// </remarks>
    private static bool TryRewriteDialogGuard(
        StatementSyntax statement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!target.AllowsWindowApis
            || statement is not IfStatementSyntax { Else: null } ifStatement
            || ifStatement.Condition is not BinaryExpressionSyntax comparison
            || !TryMatchShowDialogOkComparison(comparison, SyntaxKind.NotEqualsExpression, out var call)
            || !IsBareReturn(ifStatement.Statement)
            || !target.TryResolveControlField(call.Expression, out var dialogField))
        {
            return false;
        }

        string opener;
        string variableName;
        string selectionKey;
        string selectionValue;
        var parts = new List<RewrittenStatement>();

        if (target.TryResolveFileDialog(dialogField, out var kind))
        {
            variableName = $"{dialogField}{kind.SelectionSuffix}";
            selectionKey = $"{dialogField}.{kind.PathMemberName}";
            selectionValue = $"{variableName}.Path.LocalPath";
            opener =
                $"if (await {target.StorageProviderExpression}.{kind.PickerMethodName}(new {kind.OptionsTypeName}()) is not "
                + string.Format(kind.SelectionPattern, variableName) + ")";
            target.Requirements.RequiredUsings.Add("Avalonia.Platform.Storage");
        }
        else if (target.TryResolveComponentTypeName(dialogField, out var componentType)
            && VisualDialogs.TryGetValue(componentType, out var dialog))
        {
            variableName = $"{dialogField}{dialog.ResultMember}";
            selectionKey = $"{dialogField}.{dialog.ResultMember}";
            selectionValue = variableName;
            opener = $"if (await {dialog.TemplateKey}.ShowAsync(this{TakeSeedArgument(dialogField, target)}) is not {{ }} {variableName})";
            parts.Add(new RewrittenStatement("", RequiredFallbackKeys: [dialog.TemplateKey]));
        }
        else
        {
            return false;
        }

        // Never removed - see the remarks. The statements after the guard are exactly the ones
        // entitled to name it.
        target.DialogSelections[selectionKey] = selectionValue;
        target.Requirements.RequiresAsync = true;
        target.Requirements.InlinedDialogFields.Add(dialogField);

        rewritten = Merge($"{opener}\n{{\n    return;\n}}", parts) with { RequiresAsync = true };
        return true;
    }

    /// <summary>A <c>return;</c> with nothing else in the branch, braced or not.</summary>
    private static bool IsBareReturn(StatementSyntax branch) => branch switch
    {
        ReturnStatementSyntax { Expression: null } => true,
        BlockSyntax { Statements: [ReturnStatementSyntax { Expression: null }] } => true,
        _ => false,
    };

    private static bool TryRewriteFileDialogIf(
        IfStatementSyntax ifStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!target.AllowsWindowApis
            || ifStatement.Condition is not BinaryExpressionSyntax comparison
            || !TryMatchShowDialogOkComparison(comparison, SyntaxKind.EqualsExpression, out var call)
            || !target.TryResolveControlField(call.Expression, out var dialogField)
            || !target.TryResolveFileDialog(dialogField, out var kind))
        {
            return false;
        }

        var variableName = $"{dialogField}{kind.SelectionSuffix}";
        var selectionKey = $"{dialogField}.{kind.PathMemberName}";

        target.DialogSelections[selectionKey] = $"{variableName}.Path.LocalPath";
        try
        {
            if (!TryRewriteBranch(ifStatement.Statement, target, out var thenBranch))
            {
                return false;
            }

            // An `else` runs when the user cancelled; nothing about that needs the selection.
            var parts = new List<RewrittenStatement> { thenBranch };
            var pattern = string.Format(kind.SelectionPattern, variableName);
            var text =
                $"if (await {target.StorageProviderExpression}.{kind.PickerMethodName}(new {kind.OptionsTypeName}()) is {pattern})"
                + $"\n{{\n{Indent(thenBranch.Text)}\n}}";

            if (ifStatement.Else is { } elseClause)
            {
                target.DialogSelections.Remove(selectionKey);
                if (!TryRewriteBranch(elseClause.Statement, target, out var elseBranch))
                {
                    return false;
                }

                parts.Add(elseBranch);
                text += $"\nelse\n{{\n{Indent(elseBranch.Text)}\n}}";
            }

            target.Requirements.RequiresAsync = true;
            target.Requirements.RequiredUsings.Add("Avalonia.Platform.Storage");
            target.Requirements.InlinedDialogFields.Add(dialogField);

            rewritten = Merge(text, parts);
            return true;
        }
        finally
        {
            target.DialogSelections.Remove(selectionKey);
        }
    }

    /// <summary>
    /// The two WinForms dialogs Avalonia has nothing at all for, and the bundled window this
    /// converter ships in their place. Kept here rather than in a Mapping table because exactly
    /// one call shape consults it - the same reason <c>DataFormatNames</c> lives here.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string TemplateKey, string ResultMember)> VisualDialogs =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["ColorDialog"] = ("ColorDialogFallback", "Color"),
            ["FontDialog"] = ("FontDialogFallback", "Font"),
        };

    /// <summary>
    /// <c>colorDialog1.Color = Color.Red;</c> / <c>fontDialog1.Font = label1.Font;</c> - what
    /// WinForms uses to open a dialog on a starting value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The statement emits nothing of its own: Avalonia's replacement takes the seed as an
    /// argument to the call that replaces <c>ShowDialog</c>, so it is recorded here and spent
    /// there. This is the one assignment in the translation that legitimately disappears, and it
    /// is why it may only absorb a value it can actually translate - absorbing one it cannot
    /// would drop it in silence, and the rewriter has no way to report that.
    /// </para>
    /// <para>
    /// Refusing costs more than the seed: the body translation is a prefix, so one
    /// un-translatable statement takes every statement after it - the dialog included - with it.
    /// </para>
    /// </remarks>
    private static bool TryAbsorbDialogSeed(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken)
            || assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var member } left
            || !target.TryResolveControlField(left.Expression, out var dialogField)
            || !target.TryResolveComponentTypeName(dialogField, out var componentType)
            || !VisualDialogs.TryGetValue(componentType, out var dialog)
            || member != dialog.ResultMember
            || !TryTranslateDialogSeed(assignment.Right, dialog.ResultMember, target, out var seed))
        {
            return false;
        }

        target.DialogSeeds[dialogField] = seed;
        rewritten = new RewrittenStatement("");
        return true;
    }

    /// <summary>The seed value, in the type the bundled dialog's parameter takes.</summary>
    private static bool TryTranslateDialogSeed(
        ExpressionSyntax expression, string resultMember, IRewriteTarget target, out string seed)
    {
        seed = "";

        if (resultMember == "Color")
        {
            // The same evaluator the designer path uses, so `Color.Red`, `SystemColors.Control`
            // and `Color.FromArgb(...)` all resolve - and agree with the AXAML by construction.
            if (PropertyValueFormatters.AsBrush(ExpressionEvaluator.Evaluate(expression)) is not { } hex)
            {
                return false;
            }

            seed = $"Color.Parse(\"{hex}\")";
            return true;
        }

        // `fontDialog1.Font = someControl.Font;` - a WinForms Font is one value, and the bundled
        // FontChoice is the four Avalonia properties it becomes.
        if (expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Font" } fontRead
            && target.TryResolveControlField(fontRead.Expression, out var sourceField)
            && target.SupportsStyleProperty(sourceField, AvaloniaStyleProperties.Font))
        {
            seed = $"new FontChoice({sourceField}.FontFamily, {sourceField}.FontSize, "
                + $"{sourceField}.FontWeight, {sourceField}.FontStyle)";
            return true;
        }

        return false;
    }

    /// <summary>The seed argument for a dialog call, spent once so a later call cannot reuse it.</summary>
    private static string TakeSeedArgument(string dialogField, IRewriteTarget target) =>
        target.DialogSeeds.Remove(dialogField, out var seed) ? $", {seed}" : "";

    /// <summary>
    /// <c>if (colorDialog1.ShowDialog(this) == DialogResult.OK) { … colorDialog1.Color … }</c>,
    /// translated **inline** onto a bundled dialog - the same shape as the file dialogs, and for
    /// the same reason: the Avalonia replacement returns the choice instead of being an object you
    /// ask afterwards.
    /// </summary>
    /// <remarks>
    /// A plain <c>is { }</c> pattern rather than the file dialogs' list pattern, because these
    /// return one nullable value. The binding is scoped to exactly the branch that used to read
    /// the dialog's property, which is also the only place the translation can honour - a use
    /// after the branch has nothing to refer to.
    /// </remarks>
    private static bool TryRewriteVisualDialogIf(
        IfStatementSyntax ifStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!target.AllowsWindowApis
            || ifStatement.Condition is not BinaryExpressionSyntax comparison
            || !TryMatchShowDialogOkComparison(comparison, SyntaxKind.EqualsExpression, out var call)
            || !target.TryResolveControlField(call.Expression, out var dialogField)
            || !target.TryResolveComponentTypeName(dialogField, out var componentType)
            || !VisualDialogs.TryGetValue(componentType, out var dialog))
        {
            return false;
        }

        var variableName = $"{dialogField}{dialog.ResultMember}";
        var selectionKey = $"{dialogField}.{dialog.ResultMember}";

        target.DialogSelections[selectionKey] = variableName;
        try
        {
            if (!TryRewriteBranch(ifStatement.Statement, target, out var thenBranch))
            {
                return false;
            }

            var parts = new List<RewrittenStatement> { thenBranch };
            var text =
                $"if (await {dialog.TemplateKey}.ShowAsync(this{TakeSeedArgument(dialogField, target)}) is {{ }} {variableName})"
                + $"\n{{\n{Indent(thenBranch.Text)}\n}}";

            if (ifStatement.Else is { } elseClause)
            {
                // An `else` runs when the user cancelled; nothing about that needs the selection.
                target.DialogSelections.Remove(selectionKey);
                if (!TryRewriteBranch(elseClause.Statement, target, out var elseBranch))
                {
                    return false;
                }

                parts.Add(elseBranch);
                text += $"\nelse\n{{\n{Indent(elseBranch.Text)}\n}}";
            }

            target.Requirements.RequiresAsync = true;
            target.Requirements.InlinedDialogFields.Add(dialogField);

            rewritten = Merge(text, [.. parts, new RewrittenStatement("", RequiredFallbackKeys: [dialog.TemplateKey])]);
            return true;
        }
        finally
        {
            target.DialogSelections.Remove(selectionKey);
        }
    }

    private static bool TryRewriteIf(IfStatementSyntax ifStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        if (TryRewriteFileDialogIf(ifStatement, target, out rewritten))
        {
            return true;
        }

        if (TryRewriteVisualDialogIf(ifStatement, target, out rewritten))
        {
            return true;
        }

        rewritten = default;

        if (!TryRewriteExpression(ifStatement.Condition, target, out var condition)
            || !TryRewriteBranch(ifStatement.Statement, target, out var thenBranch))
        {
            return false;
        }

        var parts = new List<RewrittenStatement> { thenBranch };
        var text = $"if ({condition})\n{{\n{Indent(thenBranch.Text)}\n}}";

        if (ifStatement.Else is { } elseClause)
        {
            if (!TryRewriteBranch(elseClause.Statement, target, out var elseBranch))
            {
                return false;
            }

            parts.Add(elseBranch);

            // `else if` reads better than a nested braced if, and the recursion already produced
            // exactly that text.
            text += elseClause.Statement is IfStatementSyntax
                ? $"\nelse {elseBranch.Text}"
                : $"\nelse\n{{\n{Indent(elseBranch.Text)}\n}}";
        }

        rewritten = Merge(text, parts);
        return true;
    }

    /// <summary>
    /// `foreach (var item in <collection>)`. The loop variable is a
    /// <see cref="LocalKind.Value"/> for the same reason any other local is: the collection
    /// expression had to translate, so it is a plain .NET value, and so are its elements.
    /// </summary>
    private static bool TryRewriteForEach(
        ForEachStatementSyntax forEach, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (DeclaredTypeText(forEach.Type) is not { } typeText
            || !TryRewriteExpression(forEach.Expression, target, out var collection))
        {
            return false;
        }

        var name = forEach.Identifier.ValueText;
        return TryRewriteLoopBody(
            $"foreach ({typeText} {name} in {collection})",
            forEach.Statement,
            target,
            [(name, LocalKind.Value)],
            out rewritten);
    }

    /// <summary>
    /// `for (var i = 0; i < n; i++)`. Only the single-declaration shape is taken - a comma-separated
    /// initializer list is rare enough in designer-era handler code not to be worth the ambiguity.
    /// </summary>
    private static bool TryRewriteFor(ForStatementSyntax forStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (forStatement.Initializers.Count > 0)
        {
            return false;
        }

        // The loop variable is in scope for the condition and incrementors too, so it has to be
        // declared before either is translated - and popped again by TryRewriteLoopBody.
        var declared = new List<(string Name, LocalKind Kind)>();
        var header = "for (";

        if (forStatement.Declaration is { Variables: [{ Initializer.Value: { } initial } variable] } declaration)
        {
            if (DeclaredTypeText(declaration.Type) is not { } typeText
                || !TryRewriteExpression(initial, target, out var initialValue))
            {
                return false;
            }

            declared.Add((variable.Identifier.ValueText, LocalKind.Value));
            header += $"{typeText} {variable.Identifier.ValueText} = {initialValue}";
        }
        else if (forStatement.Declaration is not null)
        {
            return false;
        }

        // Declared up front so the condition and incrementors below can see the loop variable;
        // TryRewriteLoopBody pushes its own scope and re-declares them for the body.
        target.Locals.Push();
        try
        {
            foreach (var (name, kind) in declared)
            {
                target.Locals.Declare(name, kind);
            }

            var condition = "";
            if (forStatement.Condition is { } conditionExpression
                && !TryRewriteExpression(conditionExpression, target, out condition))
            {
                return false;
            }

            var incrementors = new List<string>();
            foreach (var incrementor in forStatement.Incrementors)
            {
                if (!TryRewriteIncrement(incrementor, target, out var text)
                    && !TryRewriteSideEffect(incrementor, target, out text))
                {
                    return false;
                }

                incrementors.Add(text);
            }

            header += $"; {condition}; {string.Join(", ", incrementors)})";
        }
        finally
        {
            target.Locals.Pop();
        }

        return TryRewriteLoopBody(header, forStatement.Statement, target, declared, out rewritten);
    }

    private static bool TryRewriteWhile(WhileStatementSyntax whileStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        return TryRewriteExpression(whileStatement.Condition, target, out var condition)
            && TryRewriteLoopBody($"while ({condition})", whileStatement.Statement, target, [], out rewritten);
    }

    /// <summary>
    /// The shared tail of every loop: declare the loop variables, translate the body
    /// all-or-nothing (as with an <c>if</c> branch), and brace it.
    /// </summary>
    private static bool TryRewriteLoopBody(
        string header,
        StatementSyntax body,
        IRewriteTarget target,
        IReadOnlyList<(string Name, LocalKind Kind)> loopVariables,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        target.Locals.Push();
        try
        {
            foreach (var (name, kind) in loopVariables)
            {
                target.Locals.Declare(name, kind);
            }

            if (!TryRewriteStatement(body, target, out var translatedBody))
            {
                return false;
            }

            rewritten = Merge($"{header}\n{{\n{Indent(translatedBody.Text)}\n}}", [translatedBody]);
            return true;
        }
        finally
        {
            target.Locals.Pop();
        }
    }

    /// <summary>An expression used for its side effect - an assignment, in a `for` incrementor.</summary>
    private static bool TryRewriteSideEffect(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        if (expression is not AssignmentExpressionSyntax assignment
            || !TryRewriteAssignment(assignment, target, out var statement))
        {
            return false;
        }

        text = statement.Text.TrimEnd(';');
        return true;
    }

    /// <summary>
    /// `var` or a keyword type is kept; a named type could be a WinForms type whose translation
    /// is a different type entirely, so it is refused.
    /// </summary>
    private static string? DeclaredTypeText(TypeSyntax type) =>
        type.IsVar || type is PredefinedTypeSyntax ? type.ToString() : null;

    /// <summary>
    /// One branch of an <c>if</c>, or the contents of a block - translated <b>whole or not at
    /// all</b>, unlike the top level.
    /// </summary>
    /// <remarks>
    /// The prefix rule cannot apply inside a compound statement: the un-migrated remainder is
    /// emitted as a comment *after* the whole statement, so a partially translated branch would
    /// silently drop its own tail with nothing at that spot to say so. All-or-nothing keeps the
    /// emitted code a faithful whole.
    /// </remarks>
    private static bool TryRewriteBranch(StatementSyntax branch, IRewriteTarget target, out RewrittenStatement rewritten) =>
        branch is IfStatementSyntax nestedIf
            ? TryRewriteIf(nestedIf, target, out rewritten)
            : TryRewriteStatement(branch, target, out rewritten);

    private static bool TryRewriteBlock(BlockSyntax block, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;
        var parts = new List<RewrittenStatement>();

        // Locals declared inside the block go out of scope with it, exactly as in C#.
        target.Locals.Push();
        try
        {
            foreach (var statement in block.Statements)
            {
                if (!TryRewriteStatement(statement, target, out var part))
                {
                    return false;
                }

                parts.Add(part);
            }
        }
        finally
        {
            target.Locals.Pop();
        }

        rewritten = Merge(string.Join("\n", parts.Select(p => p.Text)), parts);
        return true;
    }

    /// <summary>
    /// `var x = <expr>;` - the local becomes usable by every later statement in its scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two initializer shapes are accepted. A translatable *expression* yields a
    /// <see cref="LocalKind.Value"/>: since such an expression can only be built from literals,
    /// catalog properties and safe BCL statics, its result is always a plain .NET value, so
    /// members of it are plain .NET too. `new SomeForm()` yields a
    /// <see cref="LocalKind.FormView"/>, which only the navigation calls accept - that shape is
    /// handled here rather than in the general expression grammar so `new` cannot leak into
    /// arbitrary expressions.
    /// </para>
    /// <para>
    /// `using var dialog = new SomeForm();` drops the `using`: an Avalonia Window is not
    /// IDisposable, so there is no disposal to preserve. On any other initializer a `using` is
    /// refused rather than silently dropped, since that would discard a real Dispose call.
    /// </para>
    /// </remarks>
    private static bool TryRewriteLocalDeclaration(
        LocalDeclarationStatementSyntax declaration, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (declaration.Declaration.Variables is not [{ Initializer.Value: { } initializer } variable]
            || declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
        {
            return false;
        }

        var name = variable.Identifier.ValueText;
        var isUsing = !declaration.UsingKeyword.IsKind(SyntaxKind.None);

        // `new SomeForm()` - the only construction this rewriter understands.
        if (initializer is ObjectCreationExpressionSyntax { ArgumentList: null or { Arguments.Count: 0 } } creation
            && target.TryResolveFormView(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type), out var view))
        {
            target.Locals.Declare(
                name, LocalKind.FormView, winFormsTypeName: RoslynTypeNameHelper.GetSimpleTypeName(creation.Type));
            rewritten = new RewrittenStatement(
                $"var {name} = new {view.ViewClassName}();",
                RequiredUsings: [view.ViewNamespace]);
            return true;
        }

        // `var button = (Button)sender!;` - in a handler wired to exactly one control, `sender`
        // provably *is* that control, so the local becomes another name for its field and the
        // cast disappears. Casting it to the Avalonia element type instead would need the type
        // this converter deliberately does not have a semantic model for.
        // `var root = this.treeView1.Nodes.Add("Reloaded");` - WinForms hands back the node it
        // just made. Avalonia has no such call, so the one statement becomes the two it stood for:
        // make the item, then add it. The local is the same node either way.
        if (!isUsing
            && initializer is InvocationExpressionSyntax nodeAdd
            && TrySplitInvocation(nodeAdd, out var nodeReceiver, out var nodeMethod)
            && nodeMethod == "Add"
            && nodeReceiver is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Nodes" } nodesAccess
            && TryResolveTreeReceiver(nodesAccess.Expression, target, out var nodeOwner)
            && nodeAdd.ArgumentList.Arguments is [{ Expression: { } nodeHeader }]
            && TryRewriteExpression(nodeHeader, target, out var nodeHeaderText))
        {
            target.Locals.Declare(name, LocalKind.TreeNode);
            rewritten = new RewrittenStatement(
                $"var {name} = new TreeViewItem {{ Header = {nodeHeaderText} }};\n"
                + $"{nodeOwner}.Items.Add({name});");
            return true;
        }

        if (!isUsing
            && StripSuppression(initializer) is CastExpressionSyntax cast
            && StripSuppression(cast.Expression) is IdentifierNameSyntax { Identifier.ValueText: var castOperand }
            && castOperand == "sender"
            && target.TryResolveSenderCast(cast.Type, name, out var senderField, out var senderStatement))
        {
            target.Locals.Declare(name, LocalKind.Control, senderField);
            rewritten = new RewrittenStatement(senderStatement);
            return true;
        }

        if (isUsing || !TryRewriteExpression(initializer, target, out var value))
        {
            return false;
        }

        // The declared type is kept only when it is a keyword type; a named one could be a
        // WinForms type whose translation is a different type entirely.
        if (DeclaredTypeText(declaration.Declaration.Type) is not { } typeText)
        {
            return false;
        }

        target.Locals.Declare(name, LocalKind.Value);
        rewritten = new RewrittenStatement($"{typeText} {name} = {value};");
        return true;
    }

    /// <summary>Combines the children's text with the union of everything they need.</summary>
    private static RewrittenStatement Merge(string text, IReadOnlyList<RewrittenStatement> parts) =>
        new(
            text,
            [.. parts.SelectMany(p => p.RequiredUsings).Distinct(StringComparer.Ordinal)],
            [.. parts.SelectMany(p => p.RequiredFallbackKeys).Distinct(StringComparer.Ordinal)],
            parts.Any(p => p.RequiresAsync));

    private static string Indent(string text) =>
        string.Join("\n", text.Split('\n').Select(l => l.Length == 0 ? l : "    " + l));

    /// <summary>`this.label1.Text = ...;` - the single most common statement in WinForms handlers.</summary>
    /// <summary>
    /// The collection wrappers a <c>BindingSource.DataSource</c> may be assigned, whose element
    /// order and duplicate handling an <c>ObservableCollection</c> reproduces exactly.
    /// </summary>
    /// <remarks>
    /// A whitelist rather than "any generic type": a <c>HashSet&lt;T&gt;</c> assigned here would
    /// drop duplicates and lose the order, so copying it element by element into an ordered
    /// collection is not the same program.
    /// </remarks>
    private static readonly HashSet<string> DataSourceCollectionTypes = new(StringComparer.Ordinal)
    {
        "BindingList", "List", "ObservableCollection", "Collection",
    };

    /// <summary>
    /// <c>bindingSource1.DataSource = new BindingList&lt;Row&gt; { new Row { A = 1 }, ... };</c> -
    /// the rows a WinForms form put behind its grid, translated into the ViewModel collection
    /// that replaced the BindingSource.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the one place an object initializer is read, and it is not the rewriter "learning
    /// initializers": every degree of freedom is closed by a fact this run already proved.
    /// It matches only as the right-hand side of a <c>DataSource</c> assignment the *designer*
    /// already turned into an <c>ItemsSource</c> binding; the element type must be one this run
    /// lifted into <c>Models/</c>, so its settable properties are read off the parsed declaration
    /// rather than guessed; every initializer name must be one of them; and every value must
    /// already translate on its own. <see cref="TryRewriteExpression"/> gains nothing.
    /// </para>
    /// <para>
    /// Anything else still refuses, and still stops the handler at that statement: a row type from
    /// a referenced assembly, a constructor argument, a nested initializer, a list built with
    /// <c>Add</c> calls or a loop, an initializer anywhere else in a body, and any DataSource that
    /// is not this literal shape.
    /// </para>
    /// </remarks>
    private static bool TryRewriteBindingSourceDataSource(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            || assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "DataSource" } left
            || !target.TryResolveControlField(left.Expression, out var sourceField)
            || !target.TryResolveDataSourceCollections(sourceField, out var collections)
            || assignment.Right is not ObjectCreationExpressionSyntax
            {
                Type: GenericNameSyntax generic,
                ArgumentList: null or { Arguments.Count: 0 },
                Initializer: { } initializer,
            }
            || !DataSourceCollectionTypes.Contains(generic.Identifier.ValueText)
            || generic.TypeArgumentList.Arguments is not [{ } elementType]
            || !initializer.IsKind(SyntaxKind.CollectionInitializerExpression))
        {
            return false;
        }

        var elementTypeName = RoslynTypeNameHelper.GetSimpleTypeName(elementType);

        // The plan settled the element type before any body was translated. Requiring exact
        // agreement is what keeps the emitted collection's declared type and this population from
        // ever drifting apart - a mismatch would be a CS0029 in the generated project.
        if (collections.Any(c => c.ElementTypeName != elementTypeName))
        {
            return false;
        }

        var settableProperties = collections[0].ElementPropertyNames.ToHashSet(StringComparer.Ordinal);

        var rows = new List<string>();
        foreach (var element in initializer.Expressions)
        {
            if (!TryRewriteModelConstruction(element, elementTypeName, settableProperties, target, out var rowText))
            {
                return false;
            }

            rows.Add(rowText);
        }

        var lines = new List<string>();
        foreach (var collection in collections)
        {
            var receiver = $"{ViewModelFieldName}.{collection.ViewModelPropertyName}";
            lines.Add($"{receiver}.Clear();");
            lines.AddRange(rows.Select(r => $"{receiver}.Add({r});"));
        }

        rewritten = new RewrittenStatement(
            string.Join("\n", lines),
            // The row type lives in the generated Models/ folder, which the View code-behind has
            // no reason to reference otherwise.
            RequiredUsings: collections[0].ElementTypeNamespace is { } modelNamespace ? [modelNamespace] : []);
        return true;
    }

    /// <summary>
    /// <c>new Row { A = x, B = y }</c> for a row type this run carried over - and nothing else.
    /// </summary>
    private static bool TryRewriteModelConstruction(
        ExpressionSyntax expression,
        string elementTypeName,
        IReadOnlySet<string> settable,
        IRewriteTarget target,
        out string text)
    {
        text = "";

        if (expression is not ObjectCreationExpressionSyntax
            {
                ArgumentList: null or { Arguments.Count: 0 },
                Initializer: { } initializer,
            } creation
            || RoslynTypeNameHelper.GetSimpleTypeName(creation.Type) != elementTypeName
            || !initializer.IsKind(SyntaxKind.ObjectInitializerExpression))
        {
            return false;
        }

        var assignments = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in initializer.Expressions)
        {
            if (member is not AssignmentExpressionSyntax
                {
                    Left: IdentifierNameSyntax { Identifier.ValueText: var propertyName },
                } memberAssignment
                || !memberAssignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || !settable.Contains(propertyName)
                || !seen.Add(propertyName)
                || !TryRewriteExpression(memberAssignment.Right, target, out var valueText))
            {
                return false;
            }

            assignments.Add($"{propertyName} = {valueText}");
        }

        text = assignments.Count == 0
            ? $"new {elementTypeName}()"
            : $"new {elementTypeName} {{ {string.Join(", ", assignments)} }}";
        return true;
    }

    private static bool TryRewriteAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        // `colorDialog1.Color = Color.Red;` before the dialog is shown - absorbed rather than
        // emitted, and spent as an argument when the ShowAsync call is written.
        if (TryAbsorbDialogSeed(assignment, target, out rewritten))
        {
            return true;
        }

        // `this.bindingSource1.DataSource = new BindingList<Row> { new Row { ... }, ... };` -
        // the rows themselves. Checked before the right-hand side is translated, because the
        // generic expression path has no case for object creation and would (correctly) refuse.
        if (TryRewriteBindingSourceDataSource(assignment, target, out rewritten))
        {
            return true;
        }

        // `this.notesRichTextBox.Font = fontDialog1.Font;` - one WinForms value, four Avalonia
        // properties.
        if (TryRewriteFontAssignment(assignment, target, out rewritten))
        {
            return true;
        }

        // `this.plainPanel.BackColor = Color.Red;` - a colour, which Avalonia spells as a brush.
        // Checked before the right-hand side is translated, for the same reason the window
        // properties are: `Color.Red` is a System.Drawing name the generic expression path would
        // (correctly) refuse.
        if (TryRewriteStyleColorAssignment(assignment, target, out rewritten))
        {
            return true;
        }

        // `this.Text = "..."` / `dialog.WindowState = ...` - a Form property that is really a
        // Window property. Checked before the right-hand side is translated, because an enum
        // value like `FormWindowState.Maximized` is a WinForms name the generic expression path
        // would (correctly) refuse.
        if (TryRewriteWindowPropertyAssignment(assignment, target, out rewritten))
        {
            return true;
        }

        if (!TryRewriteExpression(assignment.Right, target, out var right))
        {
            return false;
        }

        // A local holds a plain .NET value, so any compound operator on it is ordinary .NET.
        // A control property is only assigned with `=`: a compound operator would read it too,
        // and the read path (which null-guards) cannot be spliced into the left of an assignment.
        if (assignment.Left is IdentifierNameSyntax { Identifier.ValueText: var localName }
            && target.Locals.TryGet(localName, out var localKind)
            && localKind == LocalKind.Value)
        {
            rewritten = new RewrittenStatement($"{localName} {assignment.OperatorToken} {right};");
            return true;
        }

        // `this.isBusy = busy;` / `isBusy = busy;` - a Form field this run carries over. Any
        // operator: it holds a plain .NET value, the same argument that allows one on a local.
        if (FormFieldNameOf(assignment.Left) is { } assignedField && target.IsPromotedField(assignedField))
        {
            rewritten = new RewrittenStatement($"{assignedField} {assignment.OperatorToken} {right};");
            return true;
        }

        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken))
        {
            return false;
        }

        // `e.Cancel = true;` - settable only through a plain member path; a translation that
        // computes something (the pointer position) is a read, and cannot be assigned to.
        if (assignment.Left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var argsMemberName } argsAccess
            && target.TryResolveEventArgsMember(argsAccess.Expression, argsMemberName, out var argsTarget)
            && !argsTarget.Contains('(', StringComparison.Ordinal))
        {
            rewritten = new RewrittenStatement($"{argsTarget} = {right};");
            return true;
        }

        // `fileSystemWatcher1.Path = ...` / `process1.StartInfo.FileName = ...` - a property of
        // a component this run emits unchanged, so the path survives verbatim. Only `=`: a
        // compound operator on this shape could just as easily be an event subscription
        // (`worker.DoWork += Foo`), which is a different thing entirely.
        if (assignment.Left is MemberAccessExpressionSyntax componentLeftAccess
            && TryRewriteComponentPath(componentLeftAccess, target, out var componentLeft))
        {
            rewritten = new RewrittenStatement($"{componentLeft} = {right};");
            return true;
        }

        // `clockTimer.Enabled = false;` on a Timer this run emits as a DispatcherTimer field.
        // A whole statement rather than a left-hand side, because Interval changes type.
        if (assignment.Left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var timerMember } timerAccess
            && target.TryResolveControlField(timerAccess.Expression, out var timerField)
            && target.IsDispatcherTimerField(timerField)
            && DispatcherTimerMemberCatalog.TryGetWrite(timerField, timerMember, right, out var timerWrite))
        {
            rewritten = new RewrittenStatement(timerWrite, RequiredUsings: ["System"]);
            return true;
        }

        if (!TryResolveControlProperty(assignment.Left, target, forWrite: true, out var left))
        {
            return false;
        }

        // A property whose value has to be rewritten to cross - WinForms' WordWrap bool against
        // Avalonia's TextWrapping enum - is written through the catalog rather than assigned as
        // it stands. Only with `=`: a compound operator would read it too, and reading is the
        // other half of the same conversion, which cannot be spliced into a left-hand side.
        if (target.TryResolveWrittenProperty(assignment.Left, out var writtenProperty)
            && writtenProperty.ValueShape != BindableValueShape.Same)
        {
            if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken))
            {
                return false;
            }

            rewritten = new RewrittenStatement(
                $"{left} = {BindablePropertyCatalog.WriteExpression(right, writtenProperty)};",
                RequiredUsings: ["Avalonia.Media"]);
            return true;
        }

        rewritten = new RewrittenStatement($"{left} = {right};");
        return true;
    }

    /// <summary>
    /// <c>control.BackColor = &lt;colour&gt;</c> / <c>ForeColor</c>, which Avalonia spells as a
    /// <c>Background</c>/<c>Foreground</c> <em>brush</em>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The colour itself goes through <see cref="ExpressionEvaluator"/> and
    /// <see cref="PropertyValueFormatters.AsBrush"/> - the very same pair the designer path uses -
    /// so a colour written in a handler and the same colour written in the designer can never come
    /// out differently. Anything those two cannot resolve to a literal (a computed colour, another
    /// control's <c>BackColor</c>) is refused rather than guessed at, exactly as in the AXAML.
    /// </para>
    /// <para>
    /// Gated on the *element*, through the same table <c>AxamlEmitter</c> consults: a Panel has a
    /// Background but no Foreground, an Image has neither, and writing one that is not there is a
    /// compile error in the generated project.
    /// </para>
    /// </remarks>
    private static bool TryRewriteStyleColorAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken)
            || assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var winFormsName } access
            || !StyleColorProperties.TryGetValue(winFormsName, out var style)
            || !target.TryResolveControlField(access.Expression, out var fieldName)
            || !target.SupportsStyleProperty(fieldName, style.Surface))
        {
            return false;
        }

        if (!TryRewriteColorExpression(assignment.Right, target, out var brush))
        {
            return false;
        }

        rewritten = new RewrittenStatement(
            $"{fieldName}.{style.AvaloniaName} = {brush};",
            RequiredUsings: ["Avalonia.Media"]);
        return true;
    }

    /// <summary>
    /// <c>control.Font = fontDialog1.Font</c> - the one font assignment this converter takes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One statement becomes four, which is a change of shape rather than of spelling: WinForms'
    /// <c>Font</c> is a single object where Avalonia has <c>FontFamily</c>, <c>FontSize</c>,
    /// <c>FontWeight</c> and <c>FontStyle</c>. Faithful because all four are written together, so
    /// nothing can observe a half-applied font.
    /// </para>
    /// <para>
    /// Only from a font-dialog result, and that restriction is what makes it provable: the value
    /// comes from <c>FontDialogFallback</c>, a record this repo ships, so its four members are a
    /// known fact rather than something inferred about an arbitrary WinForms <c>Font</c>
    /// expression - which would need the family and size resolved to literals first.
    /// </para>
    /// </remarks>
    private static bool TryRewriteFontAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken)
            || assignment.Left is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Font" } access
            || !target.TryResolveControlField(access.Expression, out var fieldName)
            || !target.SupportsStyleProperty(fieldName, AvaloniaStyleProperties.Font)
            || assignment.Right is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Font" } picked
            || !target.TryResolveControlField(picked.Expression, out var dialogField)
            || !target.DialogSelections.TryGetValue($"{dialogField}.Font", out var chosen))
        {
            return false;
        }

        rewritten = new RewrittenStatement(
            $"{fieldName}.FontFamily = {chosen}.Family;\n"
            + $"{fieldName}.FontSize = {chosen}.Size;\n"
            + $"{fieldName}.FontWeight = {chosen}.Weight;\n"
            + $"{fieldName}.FontStyle = {chosen}.Style;",
            RequiredUsings: ["Avalonia.Media"]);
        return true;
    }

    /// <summary>
    /// <c>e.Graphics.DrawEllipse(Pens.SteelBlue, 10, 10, 200, 120);</c> - the body of a WinForms
    /// <c>Paint</c> handler, onto Avalonia's <c>DrawingContext</c>.
    /// </summary>
    /// <remarks>
    /// Reachable only inside a handler whose args type is the bundled paint surface's, because
    /// that is the one place <c>e.Graphics</c> resolves to anything - see
    /// <c>EventArgsMemberCatalog</c>. Every argument still has to translate on its own: the pen or
    /// brush through the same colour pipeline the designer path uses, the coordinates through
    /// <see cref="TryRewriteExpression"/>. A call the catalog does not list, or an overload with a
    /// different arity, refuses - and the prefix rule then leaves the remainder to a human, which
    /// is what happens to <c>DrawString</c>.
    /// </remarks>
    private static bool TryRewriteGraphicsCall(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (receiver is null
            || StripSuppression(receiver) is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Graphics" } graphics
            || !target.TryResolveEventArgsMember(graphics.Expression, "Graphics", out var context)
            || !GraphicsMemberCatalog.TryGet(methodName, out var call)
            || invocation.ArgumentList.Arguments.Count != call.ArgumentCount
            || !TryRewriteStroke(invocation.ArgumentList.Arguments[0].Expression, call.Stroke, target, out var stroke))
        {
            return false;
        }

        var values = new List<object> { stroke };

        foreach (var argument in invocation.ArgumentList.Arguments.Skip(1))
        {
            if (!TryRewriteExpression(argument.Expression, target, out var text))
            {
                return false;
            }

            values.Add(text);
        }

        rewritten = new RewrittenStatement(
            $"{context}.{string.Format(CultureInfo.InvariantCulture, call.Format, [.. values])};",
            // Point/Rect live in Avalonia, the brushes and pens in Avalonia.Media.
            RequiredUsings: ["Avalonia", "Avalonia.Media"]);
        return true;
    }

    /// <summary>
    /// <c>e.Graphics.DrawString(text, font, brush, x, y);</c> - the one drawing call whose
    /// arguments are not in the catalog's shape, and the only one that needs a font.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Its own matcher rather than a <see cref="GraphicsMemberCatalog"/> row for two reasons: the
    /// brush is the third argument rather than the first, and a WinForms <c>Font</c> is one object
    /// where Avalonia wants two values - a <c>Typeface</c> and an em size.
    /// </para>
    /// <para>
    /// Arity cannot tell the overloads apart - <c>(x, y)</c> and <c>(point, format)</c> are both
    /// five arguments - so the dispatch is on the *shape* of the fourth: a point or rectangle
    /// construction, <c>e.ClipRectangle</c>, or otherwise the two-coordinate form.
    /// </para>
    /// <para>
    /// Emitted with <c>CultureInfo.CurrentCulture</c> and <c>FlowDirection.LeftToRight</c>, which
    /// is what WinForms' <c>DrawString</c> did without being asked - it had no parameter for
    /// either.
    /// </para>
    /// </remarks>
    private static bool TryRewriteDrawString(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        // `e.Graphics!.DrawString(...)` - the null-forgiving operator asserts something about the
        // WinForms expression, and the translated one is a different expression whose nullability
        // this converter decides itself.
        if (methodName != "DrawString"
            || receiver is null
            || StripSuppression(receiver) is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Graphics" } graphics
            || !target.TryResolveEventArgsMember(graphics.Expression, "Graphics", out var context)
            || invocation.ArgumentList.Arguments.Count < 4)
        {
            return false;
        }

        var arguments = invocation.ArgumentList.Arguments;

        // Everything after the brush is the placement, and optionally a StringFormat.
        if (!TryRewriteExpression(arguments[0].Expression, target, out var text)
            || !TryResolveTypeface(arguments[1].Expression, target, out var typeface, out var emSize)
            || !TryRewriteColorExpression(arguments[2].Expression, target, out var brush)
            || !TryResolveTextLayout([.. arguments.Skip(3)], target, out var origin, out var bounded, out var format)
            || !TryResolveStringFormat(format, bounded, out var settings))
        {
            return false;
        }

        var initializer = settings.Count == 0
            ? ""
            : "\n    {\n" + string.Join("", settings.Select(setting => $"        {setting},\n")) + "    }";

        rewritten = new RewrittenStatement(
            $"{context}.DrawText(" + "\n"
            + "    new FormattedText(" + "\n"
            + $"        {text}, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, {typeface}, {emSize}, {brush})"
            + initializer + "," + "\n"
            + $"    new Point({origin}));",
            RequiredUsings: ["System.Globalization", "Avalonia", "Avalonia.Media"]);
        return true;
    }

    /// <summary>
    /// Where the text goes: the arguments after the brush, as an origin plus - when WinForms gave
    /// a layout rectangle - the box to lay it out in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WinForms has three placements and Avalonia's <c>DrawText</c> has one, an origin. A point is
    /// that origin directly. A rectangle is the origin plus a size, and the size is not decoration:
    /// it is what made the text wrap, and it becomes <c>MaxTextWidth</c>/<c>MaxTextHeight</c> on
    /// the FormattedText, which is the same instruction.
    /// </para>
    /// <para>
    /// A point or rectangle has to be *constructed* in the call, or be <c>e.ClipRectangle</c>: a
    /// local of type <c>PointF</c> could not survive the conversion at all, since
    /// <c>System.Drawing</c> is not there to declare it.
    /// </para>
    /// </remarks>
    private static bool TryResolveTextLayout(
        IReadOnlyList<ArgumentSyntax> arguments,
        IRewriteTarget target,
        out string origin,
        out (string Width, string Height)? bounded,
        out ExpressionSyntax? format)
    {
        origin = "";
        bounded = null;
        format = null;

        if (arguments.Count is 0 or > 3)
        {
            return false;
        }

        var first = arguments[0].Expression;

        // `new RectangleF(x, y, w, h)` / `new Rectangle(...)`, or the surface's own clip.
        if (TryResolveLayoutRectangle(first, target, out var rectangleOrigin, out var size))
        {
            origin = rectangleOrigin;
            bounded = size;
            format = arguments.Count > 1 ? arguments[1].Expression : null;
            return arguments.Count <= 2;
        }

        // `new PointF(x, y)` / `new Point(x, y)`.
        if (first is ObjectCreationExpressionSyntax { ArgumentList.Arguments: [{ } px, { } py] } point
            && RoslynTypeNameHelper.GetSimpleTypeName(point.Type) is "PointF" or "Point"
            && TryRewriteExpression(px.Expression, target, out var pointX)
            && TryRewriteExpression(py.Expression, target, out var pointY))
        {
            origin = $"{pointX}, {pointY}";
            format = arguments.Count > 1 ? arguments[1].Expression : null;
            return arguments.Count <= 2;
        }

        // Otherwise the two-coordinate form, which needs both of them.
        if (arguments.Count < 2
            || !TryRewriteExpression(first, target, out var x)
            || !TryRewriteExpression(arguments[1].Expression, target, out var y))
        {
            return false;
        }

        origin = $"{x}, {y}";
        format = arguments.Count > 2 ? arguments[2].Expression : null;
        return true;
    }

    /// <summary>A WinForms layout rectangle, as an Avalonia origin and size.</summary>
    private static bool TryResolveLayoutRectangle(
        ExpressionSyntax expression,
        IRewriteTarget target,
        out string origin,
        out (string Width, string Height) size)
    {
        origin = "";
        size = default;

        // `e.ClipRectangle` / `e.MarginBounds` / `e.PageBounds` - already Avalonia Rects on the
        // bundled surfaces' args, so their members are read straight off them rather than
        // reconstructed. Named individually because being a Rect is what makes this safe, and
        // nothing else in the args catalog says so.
        if (expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var rectMember } clip
            && rectMember is "ClipRectangle" or "MarginBounds" or "PageBounds"
            && target.TryResolveEventArgsMember(clip.Expression, rectMember, out var rect))
        {
            origin = $"{rect}.X, {rect}.Y";
            size = ($"{rect}.Width", $"{rect}.Height");
            return true;
        }

        if (expression is not ObjectCreationExpressionSyntax
                { ArgumentList.Arguments: [{ } rx, { } ry, { } rw, { } rh] } rectangle
            || RoslynTypeNameHelper.GetSimpleTypeName(rectangle.Type) is not ("RectangleF" or "Rectangle")
            || !TryRewriteExpression(rx.Expression, target, out var x)
            || !TryRewriteExpression(ry.Expression, target, out var y)
            || !TryRewriteExpression(rw.Expression, target, out var width)
            || !TryRewriteExpression(rh.Expression, target, out var height))
        {
            return false;
        }

        origin = $"{x}, {y}";
        size = (width, height);
        return true;
    }

    /// <summary>
    /// The FormattedText initializer settings a call's layout - and its <c>StringFormat</c>, if it
    /// has one - add up to.
    /// </summary>
    /// <remarks>
    /// A StringFormat is only accepted alongside a layout rectangle, and that is the substance of
    /// the rule rather than a simplification: alignment and trimming both describe how text
    /// behaves *inside a box*, and Avalonia applies neither without a <c>MaxTextWidth</c>. On the
    /// point and two-coordinate overloads there is no box, so an alignment would be emitted and
    /// silently do nothing - see <see cref="TextFormatCatalog"/> for the rest.
    /// </remarks>
    private static bool TryResolveStringFormat(
        ExpressionSyntax? format,
        (string Width, string Height)? bounded,
        out List<string> settings)
    {
        settings = [];

        if (bounded is { } box)
        {
            settings.Add($"MaxTextWidth = {box.Width}");
            settings.Add($"MaxTextHeight = {box.Height}");
        }

        if (format is null)
        {
            return true;
        }

        if (bounded is null
            || format is not ObjectCreationExpressionSyntax
                { ArgumentList: null or { Arguments.Count: 0 } } created
            || RoslynTypeNameHelper.GetSimpleTypeName(created.Type) != "StringFormat")
        {
            return false;
        }

        foreach (var member in created.Initializer?.Expressions ?? default)
        {
            if (member is not AssignmentExpressionSyntax
                {
                    Left: IdentifierNameSyntax { Identifier.ValueText: var settingName },
                    Right: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var valueName },
                })
            {
                return false;
            }

            switch (settingName)
            {
                case "Alignment" when TextFormatCatalog.TryGetAlignment(valueName, out var alignment):
                    settings.Add($"TextAlignment = TextAlignment.{alignment}");
                    break;

                case "Trimming" when TextFormatCatalog.TryGetTrimming(valueName, out var trimming):
                    settings.Add($"Trimming = TextTrimming.{trimming}");
                    break;

                default:
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// A WinForms <c>Font</c> as the two values Avalonia wants: a <c>Typeface</c> and an em size.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three shapes, and nothing else. A <c>new Font(...)</c> literal goes through the same
    /// evaluator and formatters the designer path uses, so the point-to-pixel conversion is the
    /// one already written down rather than a second copy of it. A control's <c>Font</c> - or the
    /// Form's own - is read back off the four Avalonia properties this converter split it into
    /// everywhere else, and only where that element really carries them.
    /// </para>
    /// <para>
    /// <c>Underline</c> and <c>Strikeout</c> refuse. They are not slant or weight in Avalonia but
    /// a <c>TextDecorations</c> collection set on the FormattedText afterwards, and a Typeface
    /// cannot express them - so emitting one would silently drop the decoration.
    /// </para>
    /// </remarks>
    private static bool TryResolveTypeface(
        ExpressionSyntax expression, IRewriteTarget target, out string typeface, out string emSize)
    {
        typeface = "";
        emSize = "";

        // `new Font("Arial", 12f, FontStyle.Bold)`.
        if (expression is ObjectCreationExpressionSyntax
            && ExpressionEvaluator.Evaluate(expression) is PropertyValue.FontValue font)
        {
            if (PropertyValueFormatters.AsFontFamily(font) is not { } family
                || PropertyValueFormatters.AsFontSize(font) is not { } size
                || PropertyValueFormatters.AsTextDecorations(font) is not null)
            {
                return false;
            }

            var style = PropertyValueFormatters.AsFontStyle(font) is { } slant ? $"FontStyle.{slant}" : null;
            var weight = PropertyValueFormatters.AsFontWeight(font) is { } bold ? $"FontWeight.{bold}" : null;

            typeface = (style, weight) switch
            {
                (null, null) => $"new Typeface(\"{family}\")",
                ({ } s, null) => $"new Typeface(\"{family}\", {s})",
                (null, { } w) => $"new Typeface(\"{family}\", FontStyle.Normal, {w})",
                ({ } s, { } w) => $"new Typeface(\"{family}\", {s}, {w})",
            };

            emSize = size;
            return true;
        }

        if (expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Font" } access
            && expression is not IdentifierNameSyntax { Identifier.ValueText: "Font" })
        {
            return false;
        }

        // `someControl.Font` - only where that element carries all four font properties.
        if (expression is MemberAccessExpressionSyntax owner
            && target.TryResolveControlField(owner.Expression, out var fieldName))
        {
            if (!target.SupportsStyleProperty(fieldName, AvaloniaStyleProperties.Font))
            {
                return false;
            }

            typeface = $"new Typeface({fieldName}.FontFamily, {fieldName}.FontStyle, {fieldName}.FontWeight)";
            emSize = $"{fieldName}.FontSize";
            return true;
        }

        // `this.Font` / the bare `Font` designer code usually writes - the View's own, which is a
        // Window or a UserControl and therefore a TemplatedControl, where all four live.
        if (!target.AllowsWindowApis
            || (expression is MemberAccessExpressionSyntax { Expression: not ThisExpressionSyntax }))
        {
            return false;
        }

        typeface = "new Typeface(FontFamily, FontStyle, FontWeight)";
        emSize = "FontSize";
        return true;
    }

    /// <summary>
    /// The pen or brush a drawing call leads with. WinForms has a <c>Pens</c> and a
    /// <c>Brushes</c> palette; Avalonia has neither, so both resolve to an explicit colour and a
    /// pen is built around the brush.
    /// </summary>
    private static bool TryRewriteStroke(
        ExpressionSyntax expression, GraphicsStrokeKind kind, IRewriteTarget target, out string text)
    {
        text = "";

        if (!TryRewriteColorExpression(expression, target, out var brush))
        {
            return false;
        }

        text = kind == GraphicsStrokeKind.Pen ? $"new Pen({brush})" : brush;
        return true;
    }

    /// <summary>The two WinForms colour properties, and what each becomes.</summary>
    private static readonly IReadOnlyDictionary<string, (string AvaloniaName, AvaloniaStyleProperties Surface)> StyleColorProperties =
        new Dictionary<string, (string, AvaloniaStyleProperties)>(StringComparer.Ordinal)
        {
            ["BackColor"] = ("Background", AvaloniaStyleProperties.Background),
            ["ForeColor"] = ("Foreground", AvaloniaStyleProperties.Foreground),
        };

    /// <summary>A colour-valued expression, as an Avalonia brush.</summary>
    private static bool TryRewriteColorExpression(ExpressionSyntax expression, IRewriteTarget target, out string brush)
    {
        brush = "";

        // `colorDialog1.Color` inside an inlined colour-picker branch: the pattern variable is
        // already an Avalonia Color, so it only needs wrapping.
        if (expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Color" } picked
            && target.TryResolveControlField(picked.Expression, out var pickerField)
            && target.DialogSelections.TryGetValue($"{pickerField}.Color", out var chosen))
        {
            brush = $"new SolidColorBrush({chosen})";
            return true;
        }

        // `Color.Red`, `SystemColors.Control`, `Color.FromArgb(...)` - resolved by the same
        // evaluator the designer path uses, so the two agree by construction.
        if (PropertyValueFormatters.AsBrush(ExpressionEvaluator.Evaluate(expression)) is { } hex)
        {
            brush = $"new SolidColorBrush(Color.Parse(\"{hex}\"))";
            return true;
        }

        return false;
    }

    /// <summary>
    /// A <c>Form</c> property with an exact <c>Window</c> counterpart, per
    /// <see cref="WindowPropertyCatalog"/> - written either on the form itself (<c>this.Text</c>,
    /// or the bare <c>Text</c> designer code usually uses) or on a local holding another
    /// converted Form's View (<c>dialog.Text = "About";</c>).
    /// </summary>
    private static bool TryRewriteWindowPropertyAssignment(
        AssignmentExpressionSyntax assignment, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken)
            || !TryResolveWindowProperty(assignment.Left, target, out var receiverPrefix, out var property))
        {
            return false;
        }

        string value;
        if (property.EnumTypeName is { } enumTypeName)
        {
            // `FormWindowState.Maximized` -> `WindowState.Maximized`. Only the members both
            // frameworks spell the same way, and the receiver is not checked beyond being a name:
            // what identifies the value is the member, and an unlisted member refuses anyway.
            if (assignment.Right is not MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax,
                    Name.Identifier.ValueText: var enumMember,
                }
                || property.EnumMemberNames?.Contains(enumMember) != true)
            {
                return false;
            }

            value = $"{enumTypeName}.{enumMember}";
        }
        else if (!TryRewriteExpression(assignment.Right, target, out value))
        {
            return false;
        }

        rewritten = new RewrittenStatement($"{receiverPrefix}{property.AvaloniaPropertyName} = {value};");
        return true;
    }

    /// <summary>
    /// Reading a window property. Strings are null-guarded like every other string read here:
    /// WinForms' <c>Form.Text</c> never returns null, Avalonia's <c>Window.Title</c> is
    /// <c>string?</c>, and the generated project enables nullable.
    /// </summary>
    private static bool TryRewriteWindowPropertyRead(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        if (!TryResolveWindowProperty(expression, target, out var receiverPrefix, out var property))
        {
            return false;
        }

        var access = $"{receiverPrefix}{property.AvaloniaPropertyName}";
        text = property.ClrTypeName == "string" ? $"({access} ?? string.Empty)" : access;
        return true;
    }

    /// <summary>
    /// Resolves the left-hand side of a window-property access, yielding the receiver text to
    /// prefix the Avalonia property with - empty for the View itself, <c>"dialog."</c> for a
    /// local that holds one.
    /// </summary>
    private static bool TryResolveWindowProperty(
        ExpressionSyntax expression, IRewriteTarget target, out string receiverPrefix, out WindowProperty property)
    {
        receiverPrefix = "";
        property = null!;

        // `dialog.Text` on a local holding another converted Form's View. That one is a Window
        // whatever this host is, so it needs no reachability check of its own.
        if (expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: var localName },
                Name.Identifier.ValueText: var localMember,
            }
            && target.Locals.TryGet(localName, out var kind)
            && kind == LocalKind.FormView)
        {
            receiverPrefix = $"{localName}.";
            return WindowPropertyCatalog.TryGet(localMember, out property);
        }

        // `this.Text` or the bare `Text` - only where a Window is reachable. A converted
        // UserControl has no Title, and a ViewModel has no window at all.
        if (!target.AllowsWindowApis || !target.ReachesWindow)
        {
            return false;
        }

        var ownName = expression switch
        {
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,

            // A bare name could just as well be a local the body declared, and that shadows the
            // form's own property exactly as it would in the original.
            IdentifierNameSyntax { Identifier.ValueText: var name } when !target.Locals.TryGet(name, out _) => name,

            _ => "",
        };

        if (ownName.Length == 0 || !WindowPropertyCatalog.TryGet(ownName, out property))
        {
            return false;
        }

        // Empty where the View is the Window, so the emitted text is the bare `Title` it has
        // always been; the walk up to the Window otherwise.
        receiverPrefix = target.WindowMemberPrefix;
        return true;
    }

    /// <summary>`i++` / `--i` on a local - the shape a `for` incrementor almost always is.</summary>
    private static bool TryRewriteIncrement(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        var (operand, format) = expression switch
        {
            PostfixUnaryExpressionSyntax postfix => (postfix.Operand, $"{{0}}{postfix.OperatorToken}"),
            PrefixUnaryExpressionSyntax prefix when prefix.OperatorToken.IsKind(SyntaxKind.PlusPlusToken)
                || prefix.OperatorToken.IsKind(SyntaxKind.MinusMinusToken) => (prefix.Operand, $"{prefix.OperatorToken}{{0}}"),
            _ => (null, ""),
        };

        if (operand is not IdentifierNameSyntax { Identifier.ValueText: var name }
            || !target.Locals.TryGet(name, out var kind)
            || kind != LocalKind.Value)
        {
            return false;
        }

        text = string.Format(format, name);
        return true;
    }

    private static bool TryRewriteInvocation(
        InvocationExpressionSyntax invocation, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!TrySplitInvocation(invocation, out var receiver, out var methodName))
        {
            return false;
        }

        // `new SettingsForm().ShowDialog();` / `.Show();` - opening another converted Form.
        if (receiver is ObjectCreationExpressionSyntax creation)
        {
            return TryRewriteFormNavigation(creation, methodName, invocation, target, out rewritten);
        }

        // The same call on a local the pass already resolved to a View - the
        // `var dialog = new SettingsForm(); dialog.ShowDialog(this);` shape.
        if (receiver is IdentifierNameSyntax { Identifier.ValueText: var dialogLocal }
            && target.Locals.TryGet(dialogLocal, out var dialogKind)
            && dialogKind == LocalKind.FormView)
        {
            return TryRewriteViewNavigationCall(dialogLocal, methodName, invocation, target, out rewritten);
        }

        // `itemsTreeView.Nodes.Add("Documents");` / `.Nodes.Clear();` - building a tree at run
        // time, which is most of what a Form_Load does to one.
        if (TryRewriteTreeNodeCall(receiver, methodName, invocation, target, out rewritten))
        {
            return true;
        }

        // `treeView1.ExpandAll();` - one WinForms call, one Avalonia loop.
        if (TryRewriteExpandAll(receiver, methodName, invocation, target, out rewritten))
        {
            return true;
        }

        // `listView1.Items.Add(new ListViewItem("x"));` - only where the ListView became a
        // ListBox, which is the only shape with a faithful answer.
        // `printDocument1.Print();` / a print dialog's ShowDialog - see TryRewritePrintCall.
        if (TryRewritePrintCall(receiver, methodName, invocation, target, out rewritten))
        {
            return true;
        }

        // `checkedListBox1.SetItemChecked(0, true);` - the per-item tick.
        if (TryRewriteCheckedListItem(receiver, methodName, invocation, target, out var checkedItem))
        {
            rewritten = new RewrittenStatement($"{checkedItem};");
            return true;
        }

        // `e.Graphics.DrawString(...)` - its own shape, see TryRewriteDrawString.
        if (TryRewriteDrawString(receiver, methodName, invocation, target, out rewritten))
        {
            return true;
        }

        // `e.Graphics.DrawEllipse(...)` inside a Paint handler.
        if (TryRewriteGraphicsCall(receiver, methodName, invocation, target, out rewritten))
        {
            return true;
        }

        if (TryRewriteListViewItemCall(receiver, methodName, invocation, target, out rewritten))
        {
            return true;
        }

        // `clockTimer.Start();` - a DispatcherTimer keeps the same two verbs.
        if (invocation.ArgumentList.Arguments.Count == 0
            && receiver is not null
            && target.TryResolveControlField(receiver, out var timerField)
            && target.IsDispatcherTimerField(timerField)
            && DispatcherTimerMemberCatalog.TryGetMethod(methodName, out var timerMethod))
        {
            rewritten = new RewrittenStatement($"{timerField}.{timerMethod}();");
            return true;
        }

        // `control.Clear();` / `control.AppendText(x);` - a method with an exact Avalonia
        // equivalent, per ControlMethodCatalog. The arguments go through the ordinary expression
        // path first, so one reaching for a WinForms API refuses the whole call.
        if (receiver is not null
            && target.TryResolveControlField(receiver, out var controlField)
            && TryRewriteArguments(invocation, target, out var methodArguments)
            && target.TryResolveControlMethod(controlField, methodName, methodArguments, out var call))
        {
            rewritten = new RewrittenStatement(call);
            return true;
        }

        // `SetBusy(false);` / `this.SetBusy(false);` - a code-behind helper this run emitted as
        // real code. An async one is only callable here, as a statement: awaiting it inside a
        // larger expression would need parenthesizing rules this rewriter does not model.
        if ((receiver is null || receiver is ThisExpressionSyntax)
            && target.TryResolveHelperCall(methodName, invocation.ArgumentList.Arguments.Count, out var helper)
            && TryRewriteArguments(invocation, target, out var helperArguments))
        {
            var await_ = helper.IsAsync ? "await " : "";
            rewritten = new RewrittenStatement(
                $"{await_}{methodName}({string.Join(", ", helperArguments)});",
                RequiresAsync: helper.IsAsync);
            return true;
        }

        // `errorProvider1.SetError(this.nameTextBox, "…")` - see TryRewriteSetError.
        if (TryRewriteSetError(invocation, receiver, methodName, target, out rewritten))
        {
            return true;
        }

        // `Thread.Sleep(100);` / `File.WriteAllText(path, text);` - a call on a safe BCL type used
        // for its side effect. The expression path already accepts these; a statement is the other
        // half, and the shape a save-dialog handler reaches for on its very next line.
        if (receiver is not null
            && IsSafeStaticReceiver(receiver, out _)
            && TryRewriteCallExpression(invocation, target, out var staticCall))
        {
            rewritten = new RewrittenStatement($"{staticCall};");
            return true;
        }

        // `soundPlayer1.Play();` / `eventLog1.WriteEntry("...");` - a call on an unchanged .NET
        // component. The arguments still go through the ordinary expression path, so one that
        // names a control is translated and one that names a WinForms API refuses.
        if (receiver is not null
            && TryRewriteComponentPath(receiver, target, out _)
            && TryRewriteCallExpression(invocation, target, out var componentCall))
        {
            rewritten = new RewrittenStatement($"{componentCall};");
            return true;
        }

        if (!target.AllowsWindowApis)
        {
            return false;
        }

        // Form lifetime members, called bare or through `this`. The View *is* the Window.
        if (receiver is null || receiver is ThisExpressionSyntax)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                return false;
            }

            switch (methodName)
            {
                // Only a Window has these. A converted UserControl is not one and cannot reach
                // one - Avalonia's UserControl has no Close/Show/Activate at all - so emitting
                // them there would not compile.
                case "Close" when target.ReachesWindow:
                    rewritten = new RewrittenStatement($"{target.WindowMemberPrefix}Close();");
                    return true;
                case "Activate" when target.ReachesWindow:
                    rewritten = new RewrittenStatement($"{target.WindowMemberPrefix}Activate();");
                    return true;
                case "Show" when target.ReachesWindow:
                    rewritten = new RewrittenStatement($"{target.WindowMemberPrefix}Show();");
                    return true;

                // On a UserControl the same call means what it meant in WinForms: make me
                // visible. Which is also why Hide() needs no host check at all.
                case "Show":
                    rewritten = new RewrittenStatement("IsVisible = true;");
                    return true;
                case "Hide":
                    // Avalonia's Window.Hide() exists, but WinForms' Hide() on a control means
                    // Visible = false; using IsVisible keeps one meaning for both.
                    rewritten = new RewrittenStatement("IsVisible = false;");
                    return true;
            }

            return false;
        }

        if (receiver is IdentifierNameSyntax { Identifier.ValueText: "MessageBox" } && methodName == "Show")
        {
            return TryRewriteMessageBox(invocation, target, out rewritten);
        }

        // `Clipboard.SetText(x)`. Avalonia's clipboard hangs off the TopLevel and is async, so
        // this makes the handler async - the same consequence a message box has.
        if (receiver is IdentifierNameSyntax { Identifier.ValueText: "Clipboard" }
            && methodName == "SetText"
            && invocation.ArgumentList.Arguments is [{ Expression: var clipboardText }]
            && TryRewriteExpression(clipboardText, target, out var clipboardValue))
        {
            rewritten = new RewrittenStatement(
                // SetTextAsync is an extension method on IClipboard, so the namespace matters -
                // and TopLevel.Clipboard is nullable, so the argument needs the second `!` to
                // keep the generated project warning-free under nullable.
                $"await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync({clipboardValue});",
                RequiredUsings: ["Avalonia.Controls", "Avalonia.Input.Platform"],
                RequiresAsync: true);
            return true;
        }

        if (receiver is IdentifierNameSyntax { Identifier.ValueText: "Application" }
            && methodName == "Exit"
            && invocation.ArgumentList.Arguments.Count == 0)
        {
            rewritten = new RewrittenStatement(
                "(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();",
                RequiredUsings: ["Avalonia", "Avalonia.Controls.ApplicationLifetimes"]);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Opening another converted Form. The generated View sets its own DataContext in its
    /// constructor, so the translated call needs nothing the original did not have.
    /// </summary>
    /// <remarks>
    /// Only the bare statement forms are taken. `if (new F().ShowDialog() == DialogResult.OK)`
    /// - the shape most WinForms code actually uses - is left alone on purpose: Avalonia's
    /// ShowDialog returns a Task whose result is whatever the dialog passed to Close(), so
    /// translating the call without the branch around it would silently change the control flow.
    /// </remarks>
    private static bool TryRewriteFormNavigation(
        ObjectCreationExpressionSyntax creation,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        // The generated View constructor takes no arguments, so a Form built with any would not
        // survive the translation intact.
        if (creation.ArgumentList is { Arguments.Count: > 0 }
            || !target.TryResolveFormView(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type), out var view))
        {
            return false;
        }

        return TryRewriteViewNavigationCall(
            $"new {view.ViewClassName}()", methodName, invocation, target, out rewritten, view.ViewNamespace);
    }

    /// <summary>
    /// Emits the navigation call itself, given whatever already evaluates to the target View -
    /// a fresh `new SomeView()` or a local that holds one.
    /// </summary>
    private static bool TryRewriteViewNavigationCall(
        string viewExpression,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten,
        string? requiredUsing = null)
    {
        rewritten = default;
        IReadOnlyList<string> usings = requiredUsing is null ? [] : [requiredUsing];

        // ShowDialog's only argument is the owner, which the translated call supplies itself.
        if (invocation.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        switch (methodName)
        {
            case "ShowDialog" when target.ReachesWindow:
                rewritten = new RewrittenStatement(
                    $"await {viewExpression}.ShowDialog({target.WindowExpression});",
                    RequiredUsings: usings,
                    RequiresAsync: true);
                return true;

            case "Show" when invocation.ArgumentList.Arguments.Count == 0:
                rewritten = new RewrittenStatement($"{viewExpression}.Show();", RequiredUsings: usings);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// `MessageBox.Show(...)` has no Avalonia equivalent, so it maps to the bundled
    /// MessageBoxFallback - which is a dialog, and therefore async. Only the two argument shapes
    /// whose meaning is unambiguous are taken; the overloads carrying buttons/icons/defaults
    /// would need a return value the caller usually inspects, so they stay for a human.
    /// </summary>
    private static bool TryRewriteMessageBox(
        InvocationExpressionSyntax invocation, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        IEnumerable<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // `MessageBox.Show(this, text, caption)`: the owner overloads put the form first, and
        // the translated call supplies its own owner. Only a literal `this` is stripped - that is
        // what makes the arity unambiguous, since `Show(text, caption)` and `Show(owner, text)`
        // are otherwise the same shape, as are `Show(owner, text, caption)` and
        // `Show(text, caption, buttons)`. The buttons overloads return a DialogResult the caller
        // branches on, and must keep being refused.
        if (invocation.ArgumentList.Arguments is [{ Expression: ThisExpressionSyntax }, ..])
        {
            arguments = arguments.Skip(1);
        }

        var rewrittenArguments = new List<string>();
        foreach (var argument in arguments)
        {
            if (!TryRewriteExpression(argument.Expression, target, out var text))
            {
                return false;
            }

            rewrittenArguments.Add(text);
        }

        if (rewrittenArguments.Count is not (1 or 2))
        {
            return false;
        }

        var caption = rewrittenArguments.Count == 2 ? rewrittenArguments[1] : "\"\"";
        rewritten = new RewrittenStatement(
            $"await MessageBoxFallback.ShowAsync(this, {rewrittenArguments[0]}, {caption});",
            RequiredFallbackKeys: ["MessageBoxFallback"],
            RequiresAsync: true);
        return true;
    }

    /// <summary>
    /// Rebuilds an expression with every control-property access replaced by its target-side
    /// equivalent, refusing anything whose meaning cannot be established syntactically.
    /// </summary>
    private static bool TryRewriteExpression(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        switch (expression)
        {
            case LiteralExpressionSyntax:
                text = expression.ToString();
                return true;

            // `e.Data!` - see StripSuppression: the assertion is about the WinForms expression.
            case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed:
                return TryRewriteExpression(suppressed.Operand, target, out text);

            case ParenthesizedExpressionSyntax parenthesized:
                if (!TryRewriteExpression(parenthesized.Expression, target, out var inner))
                {
                    return false;
                }

                text = $"({inner})";
                return true;

            case PrefixUnaryExpressionSyntax prefix:
                if (!TryRewriteExpression(prefix.Operand, target, out var operand))
                {
                    return false;
                }

                text = $"{prefix.OperatorToken}{operand}";
                return true;

            // `dlg.ShowDialog(this) == DialogResult.OK` - checked before the generic binary case,
            // because neither operand translates on its own.
            case BinaryExpressionSyntax comparison
                when TryRewriteDialogResultComparison(comparison, target, out text):
                return true;

            // `MessageBox.Show(..., MessageBoxButtons.YesNo) == DialogResult.No` - the same shape,
            // against the bundled dialog rather than a converted Form.
            case BinaryExpressionSyntax comparison
                when TryRewriteMessageBoxComparison(comparison, target, out text):
                return true;

            case BinaryExpressionSyntax binary:
                if (!TryRewriteExpression(binary.Left, target, out var binaryLeft)
                    || !TryRewriteExpression(binary.Right, target, out var binaryRight))
                {
                    return false;
                }

                text = $"{binaryLeft} {binary.OperatorToken} {binaryRight}";
                return true;

            case ConditionalExpressionSyntax conditional:
                if (!TryRewriteExpression(conditional.Condition, target, out var condition)
                    || !TryRewriteExpression(conditional.WhenTrue, target, out var whenTrue)
                    || !TryRewriteExpression(conditional.WhenFalse, target, out var whenFalse))
                {
                    return false;
                }

                text = $"{condition} ? {whenTrue} : {whenFalse}";
                return true;

            // A local this pass declared earlier. Only Value locals are usable as expressions -
            // a FormView is not a value, it is something you open.
            case IdentifierNameSyntax identifier
                when target.Locals.TryGet(identifier.Identifier.ValueText, out var kind) && kind == LocalKind.Value:
                text = identifier.Identifier.ValueText;
                return true;

            // `isBusy` - a private field of the original Form that this run carries over. After
            // the local case above, so a local of that name shadows it, exactly as in C#.
            case IdentifierNameSyntax { Identifier.ValueText: var fieldName } when target.IsPromotedField(fieldName):
                text = fieldName;
                return true;

            // The bare `Text` a Form's own code uses for its title. After the local case above,
            // so a local of that name shadows it.
            case IdentifierNameSyntax when TryRewriteWindowPropertyRead(expression, target, out text):
                return true;

            case ConditionalAccessExpressionSyntax conditional:
                return TryRewriteConditionalAccess(conditional, target, out text);

            case InterpolatedStringExpressionSyntax interpolated:
                return TryRewriteInterpolatedString(interpolated, target, out text);

            case MemberAccessExpressionSyntax memberAccess:
                return TryRewriteMemberAccess(memberAccess, target, out text);

            case InvocationExpressionSyntax invocation:
                return TryRewriteCallExpression(invocation, target, out text);

            // `(string[])e.Data.GetData(DataFormats.FileDrop)` - the one cast this rewriter
            // understands, because the thing being cast is a payload it knows the shape of.
            case CastExpressionSyntax payloadCast
                when TryRewriteDragPayloadRead(payloadCast, target, out text):
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// <c>x?.Trim()</c> / <c>x?.Text.Length</c> - a null-conditional access on something that
    /// already translates to a value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The receiver has to translate as an <em>expression</em>, which is exactly the condition
    /// that makes the rest safe: everything this rewriter can produce as a value is a plain BCL
    /// value, so the members hanging off it are ordinary .NET and survive verbatim. A control
    /// field does not translate as an expression, so <c>textBox1?.Text</c> falls through to the
    /// ordinary refusal rather than being quietly reinterpreted.
    /// </para>
    /// <para>
    /// Only member accesses and zero-argument calls in the chain. An argument could name a
    /// control (<c>s?.StartsWith(this.prefixBox.Text)</c>), and emitting the chain verbatim would
    /// leave it untranslated - so the whole thing is refused rather than half-rewritten. Lifting
    /// that means rebuilding the chain rather than copying it, which is more machinery than the
    /// shape has earned.
    /// </para>
    /// </remarks>
    private static bool TryRewriteConditionalAccess(
        ConditionalAccessExpressionSyntax conditional, IRewriteTarget target, out string text)
    {
        text = "";

        // `tabControl1.SelectedTab?.Text` - the selected tab is a whole shape rather than a
        // property, so it is matched before the general chain rule.
        if (TryRewriteSelectedTabHeader(conditional, target, out text))
        {
            return true;
        }

        if (!TryRewriteExpression(conditional.Expression, target, out var receiver)
            || !IsVerbatimBindingChain(conditional.WhenNotNull))
        {
            return false;
        }

        // The `?` lives on the operator token, not on WhenNotNull, so it has to be put back
        // explicitly. Keeping it matters: the receiver is often provably non-null here (a
        // null-guarded string read), but a plain local is not, and dropping the operator there
        // would turn a safe call into a NullReferenceException.
        text = $"{receiver}{conditional.OperatorToken}{conditional.WhenNotNull}";
        return true;
    }

    /// <summary>
    /// <c>treeView.Nodes.Add("x")</c> and <c>Nodes.Clear()</c>, on the control or on a node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia's <c>ItemsControl.Items</c> is a real, mutable collection, and a
    /// <c>TreeViewItem.Header</c> is an <c>object</c> - so a WinForms tree built at run time has
    /// an exact counterpart, which is worth saying because this converter refused it for a long
    /// time as "an application design decision". Populating an <c>ObservableCollection</c> and
    /// binding <c>ItemsSource</c> is the better *end state*; it is not what the original said.
    /// </para>
    /// <para>
    /// Only a string header. <c>Nodes.Add(new TreeNode(...))</c> carries an image index, a tag and
    /// child nodes of its own, none of which has a counterpart on a bare TreeViewItem.
    /// </para>
    /// </remarks>
    private static bool TryRewriteTreeNodeCall(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (receiver is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Nodes" } nodes
            || !TryResolveTreeReceiver(nodes.Expression, target, out var owner))
        {
            return false;
        }

        if (methodName == "Clear" && invocation.ArgumentList.Arguments.Count == 0)
        {
            rewritten = new RewrittenStatement($"{owner}.Items.Clear();");
            return true;
        }

        if (methodName == "Add"
            && invocation.ArgumentList.Arguments is [{ Expression: { } header }]
            && TryRewriteExpression(header, target, out var headerText))
        {
            rewritten = new RewrittenStatement($"{owner}.Items.Add(new TreeViewItem {{ Header = {headerText} }});");
            return true;
        }

        return false;
    }

    /// <summary>
    /// <c>treeView1.ExpandAll()</c>, as the loop Avalonia needs for it.
    /// </summary>
    /// <remarks>
    /// Avalonia has no single call, which is why this was written off as having no counterpart -
    /// but <c>ExpandSubTree</c> expands the item <em>and every descendant</em>, so running it over
    /// the root items expands the whole tree. That is what ExpandAll means, and it is the reason
    /// "no one-call equivalent" and "no equivalent" are not the same answer.
    /// </remarks>
    private static bool TryRewriteExpandAll(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (methodName != "ExpandAll"
            || invocation.ArgumentList.Arguments.Count != 0
            || receiver is null
            || !target.TryResolveControlField(receiver, out var fieldName)
            || !target.TryResolveControlTypeName(fieldName, out var typeName)
            || typeName != "TreeView")
        {
            return false;
        }

        // OfType rather than a cast: this conversion only ever puts TreeViewItems in there, and
        // anything a human adds later is skipped rather than throwing.
        rewritten = new RewrittenStatement(
            $"foreach (var w2aNode in {fieldName}.Items.OfType<TreeViewItem>())\n"
            + "{\n"
            + $"    {fieldName}.ExpandSubTree(w2aNode);\n"
            + "}",
            RequiredUsings: ["System.Linq"]);
        return true;
    }

    /// <summary>
    /// <c>listView1.Items.Add(new ListViewItem("x"))</c> and <c>Items.Clear()</c>, on a ListView
    /// that became a <c>ListBox</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both halves of the ListView mapping, which are genuinely different programs. On a
    /// <c>ListBox</c> the item is the control's own, so <c>Items</c> is mutated directly. In
    /// <c>View.Details</c> a ListView becomes a <c>DataGrid</c>, whose rows are data objects
    /// bound through columns - so the row goes into the ViewModel collection
    /// <see cref="ListViewRowsPlan"/> created, as the <c>string[]</c> of sub-item texts a
    /// <c>ListViewItem</c> already is. No type is invented: column <i>i</i> binds to <c>[i]</c>.
    /// </para>
    /// <para>
    /// The array length must equal the designer's column count. A mismatch is refused rather than
    /// padded or truncated - and a Details ListView with no columns at all gets no plan, so it is
    /// refused too, because there is no row shape to translate into.
    /// </para>
    /// </remarks>
    private static bool TryRewriteListViewItemCall(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (receiver is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Items" } items
            || !target.TryResolveControlField(items.Expression, out var fieldName)
            || !target.TryResolveControlTypeName(fieldName, out var typeName)
            || typeName != "ListView"
            || !target.TryResolveMappedElementName(fieldName, out var elementName))
        {
            return false;
        }

        if (elementName == "DataGrid")
        {
            return TryRewriteListViewRowCall(fieldName, methodName, invocation, target, out rewritten);
        }

        if (elementName != "ListBox")
        {
            return false;
        }

        if (methodName == "Clear" && invocation.ArgumentList.Arguments.Count == 0)
        {
            rewritten = new RewrittenStatement($"{fieldName}.Items.Clear();");
            return true;
        }

        // `new ListViewItem("text")` only - the one-argument, one-column form.
        if (methodName == "Add"
            && invocation.ArgumentList.Arguments is
                [{ Expression: ObjectCreationExpressionSyntax { ArgumentList.Arguments: [{ Expression: { } content }] } creation }]
            && RoslynTypeNameHelper.GetSimpleTypeName(creation.Type) == "ListViewItem"
            && TryRewriteExpression(content, target, out var contentText))
        {
            rewritten = new RewrittenStatement($"{fieldName}.Items.Add(new ListBoxItem {{ Content = {contentText} }});");
            return true;
        }

        return false;
    }

    /// <summary>
    /// The three WinForms print calls that have a bundled counterpart, and what each becomes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// None of these is printing, and the templates say so. Avalonia has no printing API at all;
    /// what it has is a way to draw a page, so a <c>PrintDocument</c> becomes a page that is really
    /// drawn - by the handler the original wrote - and then previewed, laid out, or written to a
    /// file the user picks. Sending it to a printer is what is left for a library.
    /// </para>
    /// <para>
    /// A dialog resolves its document from the designer's <c>Document</c> property, so nothing is
    /// inferred from the handler.
    /// </para>
    /// </remarks>
    private static bool TryRewritePrintCall(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (receiver is null
            || !target.AllowsWindowApis
            || !target.TryResolveControlField(receiver, out var fieldName)
            || !target.TryResolveControlTypeName(fieldName, out var typeName)
            || !target.TryResolvePrintDocument(fieldName, out var documentField))
        {
            return false;
        }

        // `printDocument1.Print();` - render the page and write it where the user says.
        if (typeName == "PrintDocument" && methodName == "Print" && invocation.ArgumentList.Arguments.Count == 0)
        {
            rewritten = new RewrittenStatement(
                $"await {documentField}.PrintAsync(this);",
                RequiredFallbackKeys: ["PrintDocumentFallback"],
                RequiresAsync: true);
            return true;
        }

        if (methodName != "ShowDialog" || invocation.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        // `printPreviewDialog1.ShowDialog(this);` / `pageSetupDialog1.ShowDialog(this);`
        var templateKey = typeName switch
        {
            "PrintPreviewDialog" => "PrintPreviewDialogFallback",
            "PageSetupDialog" => "PageSetupDialogFallback",
            _ => null,
        };

        if (templateKey is null)
        {
            return false;
        }

        rewritten = new RewrittenStatement(
            $"await {templateKey}.ShowAsync(this, {documentField});",
            RequiredFallbackKeys: [templateKey, "PrintDocumentFallback"],
            RequiresAsync: true);
        return true;
    }

    /// <summary>
    /// <c>if (printDialog1.ShowDialog(this) == DialogResult.OK) { printDocument1.Print(); }</c> -
    /// the one shape a <c>PrintDialog</c> has, translated whole.
    /// </summary>
    /// <remarks>
    /// A whole-statement rewrite rather than two independent ones, because there is no
    /// statement-level answer: a PrintDialog chose a printer, and there is no printer to choose.
    /// What replaces it is the destination picker inside <c>PrintAsync</c> - so the dialog does not
    /// vanish, it moves into the call the branch was guarding. Matched narrowly: the branch has to
    /// be exactly one <c>Print()</c> on a document, or this refuses and the prefix rule leaves the
    /// whole handler alone.
    /// </remarks>
    private static bool TryRewritePrintDialogIf(
        IfStatementSyntax ifStatement, IRewriteTarget target, out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (ifStatement.Else is not null
            || !target.AllowsWindowApis
            || ifStatement.Condition is not BinaryExpressionSyntax comparison
            || !TryMatchShowDialogOkComparison(comparison, SyntaxKind.EqualsExpression, out var call)
            || !target.TryResolveControlField(call.Expression, out var dialogField)
            || !target.TryResolveControlTypeName(dialogField, out var dialogType)
            || dialogType != "PrintDialog")
        {
            return false;
        }

        var body = ifStatement.Statement is BlockSyntax { Statements: [{ } only] } ? only : ifStatement.Statement;

        if (body is not ExpressionStatementSyntax
            {
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Print" } print,
                    ArgumentList.Arguments.Count: 0,
                },
            }
            || !target.TryResolveControlField(print.Expression, out var documentField)
            || !target.TryResolveControlTypeName(documentField, out var documentType)
            || documentType != "PrintDocument")
        {
            return false;
        }

        rewritten = new RewrittenStatement(
            $"await {documentField}.PrintAsync(this);",
            RequiredFallbackKeys: ["PrintDocumentFallback"],
            RequiresAsync: true);
        return true;
    }

    /// <summary>
    /// <c>checkedListBox1.SetItemChecked(0, true)</c> and <c>GetItemChecked(0)</c> - the per-item
    /// tick, onto the row object that now carries it.
    /// </summary>
    /// <remarks>
    /// One shape, both directions, and both are exact: the WinForms call named an index and a
    /// bool, and so does the translation. <c>CheckedItems</c>/<c>CheckedIndices</c> deliberately
    /// do not go through here - they are WinForms collection types, and handing back a LINQ query
    /// that merely looks like one would let <c>.Add</c> or <c>.Count</c> compile against something
    /// that is not the same object.
    /// </remarks>
    private static bool TryRewriteCheckedListItem(
        ExpressionSyntax? receiver,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out string text)
    {
        text = "";

        if (methodName is not ("SetItemChecked" or "GetItemChecked")
            || receiver is null
            || !target.TryResolveControlField(receiver, out var fieldName)
            || !target.TryResolveCheckedList(fieldName, out var plan))
        {
            return false;
        }

        var arguments = invocation.ArgumentList.Arguments;
        var expected = methodName == "SetItemChecked" ? 2 : 1;

        if (arguments.Count != expected
            || !TryRewriteExpression(arguments[0].Expression, target, out var index))
        {
            return false;
        }

        var row = $"{ViewModelFieldName}.{plan.ViewModelPropertyName}[{index}].IsChecked";

        if (methodName == "GetItemChecked")
        {
            text = row;
            return true;
        }

        if (!TryRewriteExpression(arguments[1].Expression, target, out var value))
        {
            return false;
        }

        text = $"{row} = {value}";
        return true;
    }

    /// <summary>
    /// The DataGrid half: rows go into the ViewModel collection, one <c>string[]</c> per row.
    /// </summary>
    private static bool TryRewriteListViewRowCall(
        string fieldName,
        string methodName,
        InvocationExpressionSyntax invocation,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (!target.TryResolveListViewRows(fieldName, out var plan))
        {
            return false;
        }

        var receiver = $"{ViewModelFieldName}.{plan.ViewModelPropertyName}";

        if (methodName == "Clear" && invocation.ArgumentList.Arguments.Count == 0)
        {
            rewritten = new RewrittenStatement($"{receiver}.Clear();");
            return true;
        }

        if (methodName != "Add"
            || invocation.ArgumentList.Arguments is not [{ Expression: ObjectCreationExpressionSyntax creation }]
            || RoslynTypeNameHelper.GetSimpleTypeName(creation.Type) != "ListViewItem"
            || creation.Initializer is not null
            || creation.ArgumentList?.Arguments is not [{ Expression: { } argument }])
        {
            return false;
        }

        // `new ListViewItem(new[] { "a", "b" })` - the sub-item texts, in column order. The
        // one-string form is the same thing on a one-column grid, and the count check below is
        // what makes it nothing at all on a wider one.
        List<ExpressionSyntax> cells = argument switch
        {
            ImplicitArrayCreationExpressionSyntax { Initializer: { } values } => [.. values.Expressions],
            ArrayCreationExpressionSyntax { Initializer: { } values } => [.. values.Expressions],
            _ => [argument],
        };

        if (cells.Count != plan.ColumnFieldNames.Count)
        {
            return false;
        }

        var texts = new List<string>();
        foreach (var cell in cells)
        {
            if (!TryRewriteExpression(cell, target, out var cellText))
            {
                return false;
            }

            texts.Add(cellText);
        }

        rewritten = new RewrittenStatement($"{receiver}.Add(new[] {{ {string.Join(", ", texts)} }});");
        return true;
    }

    /// <summary>
    /// What a <c>.Nodes</c> hangs off: a TreeView this View has a field for, or a local holding a
    /// node that an earlier <c>Nodes.Add</c> returned.
    /// </summary>
    private static bool TryResolveTreeReceiver(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        if (expression is IdentifierNameSyntax { Identifier.ValueText: var localName }
            && target.Locals.TryGet(localName, out var kind)
            && kind == LocalKind.TreeNode)
        {
            text = localName;
            return true;
        }

        if (target.TryResolveControlField(expression, out var fieldName)
            && target.TryResolveControlTypeName(fieldName, out var typeName)
            && typeName == "TreeView")
        {
            text = fieldName;
            return true;
        }

        return false;
    }

    /// <summary>
    /// <c>tabControl1.SelectedTab?.Text</c> - the one thing WinForms code usually asks a
    /// TabControl.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provable because the <c>TabPage</c> → <c>TabItem</c> mapping is this converter's own: if
    /// the <c>SelectedItem</c> is not null it <em>is</em> a TabItem, because this conversion made
    /// every page one. The header is an <c>object</c>, so it is read back as a string the same way
    /// a Button's <c>Content</c> is.
    /// </para>
    /// <para>
    /// Only the <c>?.</c> form. WinForms' <c>SelectedTab</c> is non-null whenever the control has
    /// pages, so <c>SelectedTab.Text</c> throws on an empty TabControl - and any translation of it
    /// would quietly return an empty string instead. The conditional form says what to do when
    /// there is no selection, so it is the only one with an answer.
    /// </para>
    /// </remarks>
    private static bool TryRewriteSelectedTabHeader(
        ConditionalAccessExpressionSyntax conditional, IRewriteTarget target, out string text)
    {
        text = "";

        if (conditional.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SelectedTab" } selected
            || conditional.WhenNotNull is not MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Text" }
            || !target.TryResolveControlField(selected.Expression, out var fieldName)
            || !target.TryResolveControlTypeName(fieldName, out var controlTypeName)
            || controlTypeName != "TabControl")
        {
            return false;
        }

        text = $"(({fieldName}.SelectedItem as TabItem)?.Header as string)";
        return true;
    }

    /// <summary>The part after `?.`, when it can be copied across unchanged.</summary>
    private static bool IsVerbatimBindingChain(ExpressionSyntax chain) => chain switch
    {
        MemberBindingExpressionSyntax => true,
        MemberAccessExpressionSyntax access => IsVerbatimBindingChain(access.Expression),
        InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } call => IsVerbatimBindingChain(call.Expression),
        _ => false,
    };

    /// <summary>
    /// `$"TrackBar value: {this.trackBar1.Value}"` - the literal parts pass through untouched and
    /// each hole is rewritten like any other expression, so one un-translatable hole rejects the
    /// whole string rather than producing a half-converted message.
    /// </summary>
    /// <remarks>
    /// The delimiters are taken from the original tokens rather than rebuilt, so verbatim
    /// (<c>$@"..."</c>) and raw (<c>$"""..."""</c>) forms survive, as does <c>{{</c> escaping
    /// inside the text. Alignment and format clauses (<c>{x,5:N2}</c>) are plain .NET and are
    /// copied verbatim.
    /// </remarks>
    private static bool TryRewriteInterpolatedString(
        InterpolatedStringExpressionSyntax interpolated, IRewriteTarget target, out string text)
    {
        text = "";
        var builder = new System.Text.StringBuilder(interpolated.StringStartToken.ToString());

        foreach (var content in interpolated.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax literalText:
                    builder.Append(literalText.ToString());
                    break;

                case InterpolationSyntax interpolation:
                    if (!TryRewriteExpression(interpolation.Expression, target, out var hole))
                    {
                        return false;
                    }

                    builder.Append('{').Append(hole)
                        .Append(interpolation.AlignmentClause?.ToString())
                        .Append(interpolation.FormatClause?.ToString())
                        .Append('}');
                    break;

                default:
                    return false;
            }
        }

        text = builder.Append(interpolated.StringEndToken.ToString()).ToString();
        return true;
    }

    /// <summary>
    /// The WinForms modal-dialog idiom: `if (new SettingsForm().ShowDialog(this) == DialogResult.OK)`.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Avalonia's <c>ShowDialog&lt;T&gt;</c> returns whatever the dialog passed to
    /// <c>Close(result)</c>, and a converted dialog closes with a <c>bool</c> - see
    /// <c>FormMigrationPlanner.PlanDialogResultButtons</c>, which synthesizes that from the
    /// designer-set <c>DialogResult</c>. So the whole comparison collapses into the awaited call,
    /// negated when the test was for Cancel.
    /// </para>
    /// <para>
    /// Only OK and Cancel are taken. A three-way Yes/No/Cancel dialog cannot be expressed as a
    /// bool, and inventing a wider result type would change what the converted dialog returns.
    /// </para>
    /// </remarks>
    private static bool TryRewriteDialogResultComparison(
        BinaryExpressionSyntax comparison, IRewriteTarget target, out string text)
    {
        text = "";

        var isEquals = comparison.IsKind(SyntaxKind.EqualsExpression);
        if ((!isEquals && !comparison.IsKind(SyntaxKind.NotEqualsExpression)) || !target.ReachesWindow)
        {
            return false;
        }

        // Either order: `dlg.ShowDialog() == DialogResult.OK` or the reverse.
        var (call, expected) = comparison.Left is InvocationExpressionSyntax leftCall
            ? (leftCall, comparison.Right)
            : comparison.Right is InvocationExpressionSyntax rightCall ? (rightCall, comparison.Left) : (null, null);

        if (call is null
            || expected is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "DialogResult" },
                Name.Identifier.ValueText: var resultName,
            }
            || resultName is not ("OK" or "Cancel")
            || !TryResolveShowDialogTarget(call, target, out var viewExpression, out var viewNamespace))
        {
            return false;
        }

        // `== OK` and `!= Cancel` both mean "the user accepted"; the other two mean the opposite.
        var meansAccepted = isEquals == (resultName == "OK");

        target.Requirements.RequiresAsync = true;
        if (viewNamespace is not null)
        {
            target.Requirements.RequiredUsings.Add(viewNamespace);
        }

        text = $"{(meansAccepted ? "" : "!")}await {viewExpression}.ShowDialog<bool>({target.WindowExpression})";
        return true;
    }

    /// <summary>
    /// The two-button <c>MessageBox</c> overloads, whose result the caller branches on:
    /// <c>MessageBox.Show(text, caption, MessageBoxButtons.YesNo) == DialogResult.No</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Structurally the same translation as the converted-dialog contract above, and it works for
    /// the same reason: the whole comparison collapses into one awaited call returning a bool,
    /// because the dialog on the other end is one this repo ships and can therefore be given that
    /// return type. Neither operand means anything on its own, which is why it has to be matched
    /// as a comparison rather than as a call.
    /// </para>
    /// <para>
    /// Only the two-way overloads. <c>YesNoCancel</c> and <c>AbortRetryIgnore</c> have no bool
    /// answer, and widening the result would change what every bundled dialog returns - the same
    /// argument that keeps <c>ShowDialog&lt;bool&gt;</c> two-valued. The icon overloads are out
    /// too: the bundled dialog draws no icon, so accepting them would silently drop a cue the
    /// original showed.
    /// </para>
    /// </remarks>
    private static bool TryRewriteMessageBoxComparison(
        BinaryExpressionSyntax comparison, IRewriteTarget target, out string text)
    {
        text = "";

        var isEquals = comparison.IsKind(SyntaxKind.EqualsExpression);
        if ((!isEquals && !comparison.IsKind(SyntaxKind.NotEqualsExpression)) || !target.AllowsWindowApis)
        {
            return false;
        }

        var (call, expected) = comparison.Left is InvocationExpressionSyntax leftCall
            ? (leftCall, comparison.Right)
            : comparison.Right is InvocationExpressionSyntax rightCall ? (rightCall, comparison.Left) : (null, null);

        if (call is null
            || call.Expression is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "MessageBox" },
                Name.Identifier.ValueText: "Show",
            }
            || expected is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "DialogResult" },
                Name.Identifier.ValueText: var resultName,
            })
        {
            return false;
        }

        // The owner overloads put the form first, and the translated call supplies its own owner.
        var arguments = call.ArgumentList.Arguments;
        if (arguments is [{ Expression: ThisExpressionSyntax }, ..])
        {
            arguments = SyntaxFactory.SeparatedList(arguments.Skip(1));
        }

        if (arguments is not [{ Expression: var textArgument }, { Expression: var captionArgument }, { Expression: var buttonsArgument }]
            || buttonsArgument is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "MessageBoxButtons" },
                Name.Identifier.ValueText: var buttonsName,
            }
            || !MessageBoxAnswers.TryGetValue(buttonsName, out var answer)
            || (resultName != answer.AcceptedResult && resultName != answer.RejectedResult)
            || !TryRewriteExpression(textArgument, target, out var messageText)
            || !TryRewriteExpression(captionArgument, target, out var caption))
        {
            return false;
        }

        // `== Yes` and `!= No` both mean "the user accepted"; the other two mean the opposite.
        var meansAccepted = isEquals == (resultName == answer.AcceptedResult);

        target.Requirements.RequiresAsync = true;
        target.Requirements.RequiredFallbackKeys.Add("MessageBoxFallback");

        text = $"{(meansAccepted ? "" : "!")}await MessageBoxFallback.{answer.MethodName}(this, {messageText}, {caption})";
        return true;
    }

    /// <summary>The <c>MessageBoxButtons</c> values with a two-valued answer, and nothing else.</summary>
    private static readonly IReadOnlyDictionary<string, (string MethodName, string AcceptedResult, string RejectedResult)> MessageBoxAnswers =
        new Dictionary<string, (string, string, string)>(StringComparer.Ordinal)
        {
            ["YesNo"] = ("ShowYesNoAsync", "Yes", "No"),
            ["OKCancel"] = ("ShowOkCancelAsync", "OK", "Cancel"),
        };

    /// <summary>
    /// The `X` of `X.ShowDialog(...)`, when X is a converted Form - either freshly constructed or
    /// a local already holding its View.
    /// </summary>
    private static bool TryResolveShowDialogTarget(
        InvocationExpressionSyntax call, IRewriteTarget target, out string viewExpression, out string? viewNamespace)
    {
        viewExpression = "";
        viewNamespace = null;

        if (call.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ShowDialog" } access
            || call.ArgumentList.Arguments.Count > 1)
        {
            return false;
        }

        switch (access.Expression)
        {
            case ObjectCreationExpressionSyntax { ArgumentList: null or { Arguments.Count: 0 } } creation
                when target.TryResolveFormView(RoslynTypeNameHelper.GetSimpleTypeName(creation.Type), out var view):
                viewExpression = $"new {view.ViewClassName}()";
                viewNamespace = view.ViewNamespace;
                return true;

            case IdentifierNameSyntax { Identifier.ValueText: var localName }
                when target.Locals.TryGet(localName, out var kind) && kind == LocalKind.FormView:
                viewExpression = localName;
                return true;

            default:
                return false;
        }
    }

    private static bool TryRewriteMemberAccess(MemberAccessExpressionSyntax memberAccess, IRewriteTarget target, out string text)
    {
        // `this.isBusy` on a carried-over Form field.
        if (memberAccess is { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var ownFieldName }
            && target.IsPromotedField(ownFieldName))
        {
            text = ownFieldName;
            return true;
        }

        // `this.Text` / `dialog.Title` - checked first, so a local holding a View shadows a
        // same-named control field exactly as it would in the original.
        if (TryRewriteWindowPropertyRead(memberAccess, target, out text))
        {
            return true;
        }

        if (TryResolveControlProperty(memberAccess, target, forWrite: false, out text))
        {
            return true;
        }

        // `clockTimer.Enabled` on a Timer this run emits as a DispatcherTimer field.
        if (target.TryResolveControlField(memberAccess.Expression, out var timerField)
            && target.IsDispatcherTimerField(timerField)
            && DispatcherTimerMemberCatalog.TryGetRead(memberAccess.Name.Identifier.ValueText, out var timerMember))
        {
            text = $"{timerField}.{timerMember}";
            return true;
        }

        // `string.Empty`, `Math.PI`, `DateTime.Now`, ... - plain .NET, unchanged in Avalonia.
        if (IsSafeStaticReceiver(memberAccess.Expression, out _))
        {
            text = memberAccess.ToString();
            return true;
        }

        // `nameTextBox.Text.Length` - a member *of* a control property. Safe because every type
        // in BindablePropertyCatalog is a plain BCL type (string/bool/int/...), so anything
        // hanging off one is ordinary .NET rather than a WinForms API. The receiver goes through
        // the same read path as anywhere else, so it is null-guarded too.
        if (TryResolveControlProperty(memberAccess.Expression, target, forWrite: false, out var propertyAccess))
        {
            text = $"{propertyAccess}.{memberAccess.Name}";
            return true;
        }

        // `openFileDialog1.FileName` inside an inlined picker branch - the chosen item is a
        // pattern variable now, not a property of a dialog object that no longer exists.
        if (memberAccess.Expression is IdentifierNameSyntax or MemberAccessExpressionSyntax
            && target.TryResolveControlField(memberAccess.Expression, out var dialogField)
            && target.DialogSelections.TryGetValue(
                $"{dialogField}.{memberAccess.Name.Identifier.ValueText}", out var selection))
        {
            text = selection;
            return true;
        }

        // `e.NewValue`, `e.X`, ... - the handler's own EventArgs parameter.
        if (target.TryResolveEventArgsMember(memberAccess.Expression, memberAccess.Name.Identifier.ValueText, out var argsMember))
        {
            text = argsMember;
            return true;
        }

        // `DragDropEffects.Copy` - an enum both frameworks spell the same way.
        if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: var enumTypeName }
            && PassThroughEnums.TryGetValue(enumTypeName, out var enumMembers)
            && enumMembers.Contains(memberAccess.Name.Identifier.ValueText))
        {
            text = $"{enumTypeName}.{memberAccess.Name.Identifier.ValueText}";
            return true;
        }

        // `backgroundWorker1.IsBusy` / `process1.StartInfo` - a member of a component this run
        // emits unchanged. Safe for the same reason members of a translated local are: the
        // object is the very same .NET type it was in WinForms, so everything hanging off it is
        // ordinary .NET. That is what makes a per-member catalog unnecessary here.
        if (TryRewriteComponentPath(memberAccess, target, out text))
        {
            return true;
        }

        // `text.Length` on a local. Same argument: a Value local can only hold what a
        // translatable expression produced, which is always a plain .NET value.
        if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: var localName }
            && target.Locals.TryGet(localName, out var localKind)
            && localKind == LocalKind.Value)
        {
            text = $"{localName}.{memberAccess.Name}";
            return true;
        }

        text = "";
        return false;
    }

    /// <summary>
    /// A receiver that names one of the <see cref="SafeStaticReceivers"/>. Both spellings count:
    /// `int.Parse` parses as a predefined-type keyword, `Int32.Parse` as an identifier.
    /// </summary>
    private static bool IsSafeStaticReceiver(ExpressionSyntax expression, out string name)
    {
        name = expression switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => "",
        };

        return name.Length > 0 && SafeStaticReceivers.Contains(name);
    }

    /// <summary>
    /// A call used as a value: either on a safe BCL type (`int.Parse(...)`) or on something this
    /// rewriter already understands (`this.textBox1.Text.Trim()`).
    /// </summary>
    private static bool TryRewriteCallExpression(InvocationExpressionSyntax invocation, IRewriteTarget target, out string text)
    {
        text = "";

        if (TryRewriteDragDataQuery(invocation, target, out text))
        {
            return true;
        }

        // `if (checkedListBox1.GetItemChecked(0))` - the tick as a value.
        if (TrySplitInvocation(invocation, out var checkedReceiver, out var checkedMethod)
            && TryRewriteCheckedListItem(checkedReceiver, checkedMethod, invocation, target, out text))
        {
            return true;
        }

        // `Format(x)` used as a value. A *synchronous* helper only: an awaited call inside a
        // larger expression would need precedence handling this rewriter deliberately avoids, so
        // an async helper is callable as a statement and nowhere else.
        if (TrySplitInvocation(invocation, out var helperReceiver, out var helperName)
            && (helperReceiver is null || helperReceiver is ThisExpressionSyntax)
            && target.TryResolveHelperCall(helperName, invocation.ArgumentList.Arguments.Count, out var helper)
            && !helper.IsAsync
            && TryRewriteArguments(invocation, target, out var helperArguments))
        {
            text = $"{helperName}({string.Join(", ", helperArguments)})";
            return true;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax { Name: var name } call)
        {
            return false;
        }

        string receiverText;
        if (IsSafeStaticReceiver(call.Expression, out var receiverName))
        {
            receiverText = receiverName;
        }
        else if (!TryRewriteExpression(call.Expression, target, out receiverText))
        {
            return false;
        }

        var arguments = new List<string>();
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            // A ref/out/named argument changes the call's meaning in ways this is not trying to
            // reason about.
            if (argument.NameColon is not null || !argument.RefKindKeyword.IsKind(SyntaxKind.None)
                || !TryRewriteExpression(argument.Expression, target, out var argumentText))
            {
                return false;
            }

            arguments.Add(argumentText);
        }

        text = $"{receiverText}.{name}({string.Join(", ", arguments)})";
        return true;
    }

    /// <summary>
    /// <c>errorProvider1.SetError(control, "message")</c> - the WinForms validation idiom, onto the
    /// bundled <c>ErrorProviderFallback</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The only translation whose result is a <em>static</em> call on a fallback type. Everywhere
    /// else a fallback is something the AXAML instantiates and the handler then talks to; here the
    /// WinForms component has no element at all, and its Avalonia counterpart is an attached
    /// property set from outside - so the instance call becomes a static one. That is also why it
    /// cannot live in <see cref="ControlMethodCatalog"/>, which names members of the *target*
    /// control.
    /// </para>
    /// <para>
    /// Like <c>MessageBox.Show</c>, this pulls a bundled template in from a <em>handler body</em>
    /// rather than from an element, which is what <c>RequiredFallbackKeys</c> exists for.
    /// </para>
    /// </remarks>
    private static bool TryRewriteSetError(
        InvocationExpressionSyntax invocation,
        ExpressionSyntax? receiver,
        string methodName,
        IRewriteTarget target,
        out RewrittenStatement rewritten)
    {
        rewritten = default;

        if (methodName != "SetError"
            || receiver is null
            || !target.TryResolveControlField(receiver, out var providerField)
            || !target.TryResolveFallbackTemplate(providerField, out var templateKey)
            || templateKey != "ErrorProviderFallback"
            || invocation.ArgumentList.Arguments is not [{ Expression: var controlArgument }, { Expression: var messageArgument }]
            // The first argument has to be a control the AXAML actually names, or there is no
            // field on the generated View to hand the fallback.
            || !target.TryResolveControlField(controlArgument, out var controlField)
            || !target.IsMappedElement(controlField)
            || !TryRewriteExpression(messageArgument, target, out var message))
        {
            return false;
        }

        rewritten = new RewrittenStatement(
            $"ErrorProviderFallback.SetError({controlField}, {message});",
            RequiredFallbackKeys: ["ErrorProviderFallback"]);
        return true;
    }

    /// <summary>
    /// <c>e.Data.GetDataPresent(DataFormats.FileDrop)</c> -> <c>e.DataTransfer.Contains(DataFormat.File)</c>,
    /// the one thing a translated body may ask a drag payload.
    /// </summary>
    /// <remarks>
    /// Matched as a whole shape rather than assembled from parts, because not one part of it
    /// survives on its own: Avalonia 12 renamed the property (<c>Data</c> -> <c>DataTransfer</c>),
    /// changed its type (<c>IDataObject</c> -> <c>IDataTransfer</c>, with different method names),
    /// and replaced the format constants (<c>DataFormats.FileDrop</c> -> <c>DataFormat.File</c>).
    /// Reading the payload itself (<c>GetData</c>) is left alone - Avalonia hands back storage
    /// items rather than a <c>string[]</c>, which is a change of shape, not of spelling.
    /// </remarks>
    private static bool TryRewriteDragDataQuery(
        InvocationExpressionSyntax invocation, IRewriteTarget target, out string text)
    {
        text = "";

        if (invocation.Expression is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "GetDataPresent",
                Expression: var dataReceiver,
            }
            || StripSuppression(dataReceiver) is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Data",
                Expression: var argsReceiver,
            }
            || !target.TryResolveEventArgsParameter(argsReceiver, "DragEventArgs", out var parameterName)
            || invocation.ArgumentList.Arguments is not [{ Expression: var formatArgument }]
            || formatArgument is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "DataFormats" },
                Name.Identifier.ValueText: var formatName,
            }
            || !DataFormatNames.TryGetValue(formatName, out var avaloniaFormat))
        {
            return false;
        }

        text = $"{parameterName}.DataTransfer.Contains(DataFormat.{avaloniaFormat})";
        return true;
    }

    /// <summary>
    /// <c>(string[])e.Data.GetData(DataFormats.FileDrop)</c> - reading a dropped file list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A change of shape rather than of spelling, and it took a while to decide it was worth
    /// making: WinForms hands back an array of paths, Avalonia an array of storage items. The
    /// *content* is the same set of files, and <c>IStorageItem.Path</c> is a <c>Uri</c> whose
    /// <c>LocalPath</c> is exactly the string WinForms would have given - so the conversion is
    /// exact, it just is not a rename.
    /// </para>
    /// <para>
    /// The null-forgiving operator is kept rather than dropped, unlike everywhere else in this
    /// rewriter. Both sides return null when the drop carried no files, and the original code -
    /// either through its own <c>!</c> or by being written where the cast produced a non-nullable
    /// array - treats the result as non-null. Emitting <c>string[]?</c> instead would make the
    /// very next line (<c>files.Length</c>) a nullable warning in a project that must build
    /// warning-free.
    /// </para>
    /// <para>
    /// Only <c>FileDrop</c>, and only into a <c>string[]</c>. Every other format is a different
    /// payload with a different shape.
    /// </para>
    /// </remarks>
    private static bool TryRewriteDragPayloadRead(
        CastExpressionSyntax cast, IRewriteTarget target, out string text)
    {
        text = "";

        if (cast.Type is not ArrayTypeSyntax { ElementType: PredefinedTypeSyntax { Keyword.ValueText: "string" } }
            || StripSuppression(cast.Expression) is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name.Identifier.ValueText: "GetData",
                    Expression: var dataReceiver,
                },
                ArgumentList.Arguments:
                [{
                    Expression: MemberAccessExpressionSyntax
                    {
                        Expression: IdentifierNameSyntax { Identifier.ValueText: "DataFormats" },
                        Name.Identifier.ValueText: "FileDrop",
                    },
                }],
            }
            || StripSuppression(dataReceiver) is not MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Data",
                Expression: var argsReceiver,
            }
            || !target.TryResolveEventArgsParameter(argsReceiver, "DragEventArgs", out var parameterName))
        {
            return false;
        }

        text = $"{parameterName}.DataTransfer.TryGetFiles()!.Select(w2aFile => w2aFile.Path.LocalPath).ToArray()";
        target.Requirements.RequiredUsings.Add("System.Linq");
        return true;
    }

    /// <summary>`isBusy` or `this.isBusy` - the Form's own field, written either way.</summary>
    private static string? FormFieldNameOf(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,
        _ => null,
    };

    /// <summary>
    /// A member path rooted at a non-visual component field - <c>process1.StartInfo.FileName</c>
    /// as readily as <c>worker1.IsBusy</c>. The path survives verbatim; only the `this.` and the
    /// field name at the root are resolved.
    /// </summary>
    private static bool TryRewriteComponentPath(ExpressionSyntax expression, IRewriteTarget target, out string text)
    {
        text = "";

        if (expression is not MemberAccessExpressionSyntax memberAccess)
        {
            // The root itself: `worker1` or `this.worker1`.
            return target.TryResolveControlField(expression, out text) && target.IsComponentField(text);
        }

        if (target.TryResolveControlField(memberAccess, out var rootField) && target.IsComponentField(rootField))
        {
            text = rootField;
            return true;
        }

        if (!TryRewriteComponentPath(memberAccess.Expression, target, out var receiver))
        {
            return false;
        }

        text = $"{receiver}.{memberAccess.Name}";
        return true;
    }

    /// <summary>`this.label1.Text` / `label1.Text` resolved to whatever it is on the target side.</summary>
    private static bool TryResolveControlProperty(
        ExpressionSyntax expression, IRewriteTarget target, bool forWrite, out string text)
    {
        text = "";

        if (expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var propertyName } access)
        {
            return false;
        }

        // `dialog.EnteredText` - a property of another converted View, on a local holding one.
        // Before the control branch, because such a local is not a control field at all.
        if (access.Expression is IdentifierNameSyntax { Identifier.ValueText: var localName }
            && target.Locals.TryGetBinding(localName, out var binding)
            && binding is { Kind: LocalKind.FormView, WinFormsTypeName: { } viewTypeName })
        {
            if (!target.TryResolveViewProperty(viewTypeName, propertyName, forWrite))
            {
                return false;
            }

            text = $"{localName}.{propertyName}";
            return true;
        }

        if (!target.TryResolveControlField(access.Expression, out var fieldName))
        {
            return false;
        }

        // `notifyIcon1.Visible` - the NotifyIcon has no element of its own, but the App this run
        // generates exposes the TrayIcon it became. Before the control branch, because the field
        // is a component rather than a control and the catalog knows nothing about it.
        if (target.TryResolveTrayIconAccessor(fieldName, propertyName, out text))
        {
            return true;
        }

        return target.TryResolveProperty(fieldName, propertyName, forWrite, out text);
    }

    /// <summary>
    /// Drops a trailing null-forgiving `!`. The operator asserts something about the *WinForms*
    /// expression's nullability, and the translated expression is a different one whose
    /// nullability this converter decides itself - it null-guards the reads that need it.
    /// </summary>
    private static ExpressionSyntax StripSuppression(ExpressionSyntax expression) =>
        expression is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppression
            ? StripSuppression(suppression.Operand)
            : expression;

    /// <summary>
    /// Translates every argument of a call, refusing the whole thing if one does not translate or
    /// carries a modifier this rewriter is not reasoning about.
    /// </summary>
    private static bool TryRewriteArguments(
        InvocationExpressionSyntax invocation, IRewriteTarget target, out List<string> arguments)
    {
        arguments = [];

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.NameColon is not null
                || !argument.RefKindKeyword.IsKind(SyntaxKind.None)
                || !TryRewriteExpression(argument.Expression, target, out var text))
            {
                return false;
            }

            arguments.Add(text);
        }

        return true;
    }

    /// <summary>Splits `a.B(...)` into receiver `a` and name `B`; `B(...)` yields a null receiver.</summary>
    private static bool TrySplitInvocation(
        InvocationExpressionSyntax invocation, out ExpressionSyntax? receiver, out string methodName)
    {
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } access:
                receiver = access.Expression;
                methodName = name;
                return true;
            case IdentifierNameSyntax identifier:
                receiver = null;
                methodName = identifier.Identifier.ValueText;
                return true;
            default:
                receiver = null;
                methodName = "";
                return false;
        }
    }

    private readonly record struct RewrittenStatement(
        string Text,
        IReadOnlyList<string>? RequiredUsings = null,
        IReadOnlyList<string>? RequiredFallbackKeys = null,
        bool RequiresAsync = false)
    {
        public IReadOnlyList<string> RequiredUsings { get; } = RequiredUsings ?? [];

        public IReadOnlyList<string> RequiredFallbackKeys { get; } = RequiredFallbackKeys ?? [];
    }

    /// <summary>What a translated local variable holds, which is what decides how it may be used.</summary>
    private enum LocalKind
    {
        /// <summary>
        /// A plain .NET value. Safe to take members of: a translatable initializer can only be
        /// built from literals, catalog properties and safe BCL statics, so its result is always
        /// a BCL value - the same argument that allows members of a control property.
        /// </summary>
        Value,

        /// <summary>A converted Form's View. Only the navigation calls are allowed on it.</summary>
        FormView,

        /// <summary>
        /// Another name for one of the form's controls - what `var button = (Button)sender;`
        /// declares in a handler wired to exactly one control. Everything a control field
        /// supports works through it, because it *is* that field.
        /// </summary>
        Control,

        /// <summary>
        /// A tree node an earlier `Nodes.Add` returned, emitted as a TreeViewItem. Only
        /// `Nodes.Add`/`Nodes.Clear` go through it - it is a node, not a control.
        /// </summary>
        TreeNode,
    }

    /// <summary>
    /// One local: the control it aliases when it is a <see cref="LocalKind.Control"/>, or the
    /// WinForms type it was constructed from when it is a <see cref="LocalKind.FormView"/> - which
    /// is what says whose public surface `dialog.EnteredText` is asking about.
    /// </summary>
    private readonly record struct LocalBinding(
        LocalKind Kind, string? ControlFieldName = null, string? WinFormsTypeName = null);

    /// <summary>
    /// The locals in scope while one body is translated. Block-scoped, so a declaration inside an
    /// `if` branch cannot leak past it - matching C#, and matching the fact that a branch is
    /// translated all-or-nothing.
    /// </summary>
    private sealed class LocalScope
    {
        private readonly List<Dictionary<string, LocalBinding>> _scopes = [new(StringComparer.Ordinal)];

        public void Push() => _scopes.Add(new Dictionary<string, LocalBinding>(StringComparer.Ordinal));

        public void Pop() => _scopes.RemoveAt(_scopes.Count - 1);

        public void Declare(
            string name, LocalKind kind, string? controlFieldName = null, string? winFormsTypeName = null) =>
            _scopes[^1][name] = new LocalBinding(kind, controlFieldName, winFormsTypeName);

        public bool TryGet(string name, out LocalKind kind)
        {
            var found = TryGetBinding(name, out var binding);
            kind = binding.Kind;
            return found;
        }

        public bool TryGetBinding(string name, out LocalBinding binding)
        {
            for (var i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].TryGetValue(name, out binding))
                {
                    return true;
                }
            }

            binding = default;
            return false;
        }
    }

    /// <summary>
    /// What the translation so far has obliged the generated method to do. Statements report this
    /// through <see cref="RewrittenStatement"/>; an *expression* has no such channel, so a
    /// translation like the dialog-result comparison - which introduces an `await` from inside a
    /// condition - records it here instead.
    /// </summary>
    /// <remarks>
    /// Mutable and scoped to a single Rewrite call. The top-level loop snapshots it before each
    /// statement and restores it if that statement turns out to be untranslatable, so a partially
    /// rewritten expression cannot leave the method marked `async` with nothing to await.
    /// </remarks>
    private sealed class RewriteRequirements
    {
        public bool RequiresAsync { get; set; }

        public HashSet<string> RequiredUsings { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Bundled templates an *expression* pulled in - the message-box comparison is the one
        /// that does, since it collapses a whole comparison into an awaited call.
        /// </summary>
        public HashSet<string> RequiredFallbackKeys { get; } = new(StringComparer.Ordinal);

        /// <summary>Dialog fields a body opened inline, so no separate method is generated for them.</summary>
        public HashSet<string> InlinedDialogFields { get; } = new(StringComparer.Ordinal);

        public (bool Async, string[] Usings, string[] Fallbacks, string[] Dialogs) Snapshot() =>
            (RequiresAsync, [.. RequiredUsings], [.. RequiredFallbackKeys], [.. InlinedDialogFields]);

        public void Restore((bool Async, string[] Usings, string[] Fallbacks, string[] Dialogs) snapshot)
        {
            RequiresAsync = snapshot.Async;
            RequiredUsings.Clear();
            RequiredUsings.UnionWith(snapshot.Usings);
            RequiredFallbackKeys.Clear();
            RequiredFallbackKeys.UnionWith(snapshot.Fallbacks);
            InlinedDialogFields.Clear();
            InlinedDialogFields.UnionWith(snapshot.Dialogs);
        }
    }

    /// <summary>Where the rewritten body is going to live, and what a control property means there.</summary>
    private interface IRewriteTarget
    {
        /// <summary>Locals declared so far. Mutable, and scoped to a single Rewrite call.</summary>
        LocalScope Locals { get; }

        /// <summary>What expression-level translations have obliged the method to do.</summary>
        RewriteRequirements Requirements { get; }

        /// <summary>
        /// While an inlined file-dialog branch is being translated, what that dialog's path
        /// property now reads as. Scoped to the branch, like the pattern variable it names.
        /// </summary>
        Dictionary<string, string> DialogSelections { get; }

        /// <summary>
        /// What a dialog was seeded with before it was shown, keyed by dialog field. WinForms
        /// spells that as an assignment to the component (<c>colorDialog1.Color = ...;</c>)
        /// *before* the ShowDialog; Avalonia's replacement takes it as an argument, so the
        /// statement is absorbed here and spent when the call is emitted.
        /// </summary>
        Dictionary<string, string> DialogSeeds { get; }

        /// <summary>False in a ViewModel, which has no Window to close and no dialog owner.</summary>
        bool AllowsWindowApis { get; }

        /// <summary>
        /// False where turning the handler <c>async</c> would change what it does, not just how it
        /// reads - see <see cref="ViewTarget.AllowsAsync"/>.
        /// </summary>
        bool AllowsAsync { get; }

        /// <summary>
        /// Whether a Window can be named from here at all. False in a ViewModel and in a converted
        /// UserControl; true both for a View that is the Window and for one that can reach it.
        /// </summary>
        bool ReachesWindow { get; }

        /// <summary>
        /// The Window as an expression - <c>this</c> where the View is one. Everything below
        /// writes <see cref="WindowMemberPrefix"/> rather than reading this directly, so the
        /// common case keeps emitting the bare call it always did.
        /// </summary>
        string WindowExpression { get; }

        /// <summary>What to put in front of a Window member: nothing, or `expr.`.</summary>
        string WindowMemberPrefix =>
            string.Equals(WindowExpression, "this", StringComparison.Ordinal) ? "" : $"{WindowExpression}.";

        /// <summary>
        /// How the file pickers are reached. <c>StorageProvider</c> hangs off the TopLevel, so the
        /// bare name only works where the View is one - a Window. Anywhere else it has to be
        /// walked up to, which is also true of a converted UserControl.
        /// </summary>
        string StorageProviderExpression { get; }

        /// <summary>Resolves a WinForms Form type name to the View this run generates for it.</summary>
        bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view);

        /// <summary>`e.X` and friends, rewritten against the Avalonia args type. False in a ViewModel.</summary>
        bool TryResolveEventArgsMember(ExpressionSyntax receiver, string memberName, out string text);

        /// <summary>
        /// The handler's own EventArgs parameter, when <paramref name="expression"/> names it and
        /// its Avalonia args type is the one asked for - the check a whole-shape translation needs
        /// before it can trust what the parameter's members mean.
        /// </summary>
        bool TryResolveEventArgsParameter(ExpressionSyntax expression, string argsTypeName, out string parameterName);

        bool TryResolveControlField(ExpressionSyntax expression, out string fieldName);

        /// <param name="forWrite">True for an assignment target, where the expression must stay assignable.</param>
        bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text);

        /// <summary>The WinForms type of a control this body may name, when it names one.</summary>
        bool TryResolveControlTypeName(string fieldName, out string winFormsTypeName);

        /// <summary>
        /// The Avalonia element a control was mapped to - which a per-instance mapper decides, so
        /// it cannot be read off the WinForms type alone. A ListView is the reason: it becomes a
        /// DataGrid or a ListBox depending on the instance.
        /// </summary>
        bool TryResolveMappedElementName(string fieldName, out string avaloniaElementName);

        /// <summary>
        /// A NotifyIcon this run emitted into App.axaml, reached through the accessor the
        /// generated App declares for it.
        /// </summary>
        bool TryResolveTrayIconAccessor(string fieldName, string winFormsPropertyName, out string text);

        /// <summary>
        /// The catalog entry behind a control-property assignment target, when there is one - what
        /// says whether the *value* needs rewriting as well as the name.
        /// </summary>
        bool TryResolveWrittenProperty(
            ExpressionSyntax left, out BindablePropertyCatalog.BindableProperty property);

        /// <summary>
        /// A property another converted View exposes for real - the whole vocabulary a body may
        /// use on a View that is not this one. Named after the *WinForms* type, since that is what
        /// the original body says.
        /// </summary>
        bool TryResolveViewProperty(string winFormsTypeName, string propertyName, bool forWrite);

        /// <summary>
        /// A control method with an exact Avalonia equivalent, given the call's already-translated
        /// arguments. The arity has to match: a different overload is a different method.
        /// </summary>
        bool TryResolveControlMethod(
            string fieldName, string methodName, IReadOnlyList<string> arguments, out string statement);

        /// <summary>The field's file-dialog kind, when it is one this converter can open inline.</summary>
        bool TryResolveFileDialog(string fieldName, out FileDialogKind kind);

        /// <summary>The bundled fallback template a field maps to, when it maps to one.</summary>
        bool TryResolveFallbackTemplate(string fieldName, out string templateKey);

        /// <summary>The field's WinForms type name, for the few rules keyed on the component itself.</summary>
        bool TryResolveComponentTypeName(string fieldName, out string winFormsTypeName);

        /// <summary>
        /// True when the field's *Avalonia* element really carries the given styling surface -
        /// the same question <c>AxamlEmitter</c> asks before emitting a style attribute, and for
        /// the same reason: a Panel has a Background but no Foreground, and an Image has neither.
        /// </summary>
        bool SupportsStyleProperty(string fieldName, AvaloniaStyleProperties property);

        /// <summary>
        /// True when the field really becomes a named element in the AXAML - so the generated View
        /// has a `Control`-typed field for it, which is what an API taking a control can be given.
        /// </summary>
        bool IsMappedElement(string fieldName);

        /// <summary>
        /// True when the field is a WinForms Timer this run emits as a real DispatcherTimer field,
        /// which is what makes it something a translated body may name at all.
        /// </summary>
        bool IsDispatcherTimerField(string fieldName);

        /// <summary>
        /// `(Button)sender` in a handler wired to exactly one control - which control that is.
        /// </summary>
        bool TryResolveSenderCast(TypeSyntax castType, string localName, out string fieldName, out string statement);

        /// <summary>
        /// True when the field is a non-visual component this run emits as a real field of the
        /// same, unchanged .NET type - so anything the body says about it is ordinary .NET.
        /// </summary>
        bool IsComponentField(string fieldName);

        /// <summary>
        /// A code-behind helper method this run emits as real, compiling code - and therefore one
        /// a translated body may call.
        /// </summary>
        bool TryResolveHelperCall(string methodName, int argumentCount, out HelperCallInfo helper);

        /// <summary>
        /// A private field of the original Form this run carries over as real code, and therefore
        /// one a translated body may read and write.
        /// </summary>
        bool IsPromotedField(string name);

        /// <summary>
        /// The ViewModel collections that replaced a <c>BindingSource</c>, by the BindingSource's
        /// own field name. False on a target that cannot name a ViewModel collection at all.
        /// </summary>
        bool TryResolveDataSourceCollections(
            string bindingSourceField, out IReadOnlyList<DataSourceBindingPlan> plans)
        {
            plans = [];
            return false;
        }

        /// <summary>
        /// The bundled print document a field is, or the one a print dialog was pointed at
        /// through its designer <c>Document</c> property.
        /// </summary>
        bool TryResolvePrintDocument(string fieldName, out string documentFieldName)
        {
            documentFieldName = "";
            return false;
        }

        /// <summary>
        /// The ViewModel collection a CheckedListBox's rows live in, each carrying a caption and
        /// a tick.
        /// </summary>
        bool TryResolveCheckedList(string controlField, out CheckedListPlan plan)
        {
            plan = null!;
            return false;
        }

        /// <summary>
        /// The ViewModel collection a Details-mode ListView's rows live in, and how many columns
        /// a row must have.
        /// </summary>
        /// <remarks>
        /// Both this and <see cref="TryResolveDataSourceCollections"/> are false on
        /// <c>ViewModelTarget</c>, and not as an oversight: a promoted <c>[RelayCommand]</c> only
        /// exists when every statement was proved to touch nothing but bindable properties, which
        /// neither of these shapes is. So a handler that fills a grid stays in code-behind, where
        /// the View can name its own ViewModel field.
        /// </remarks>
        bool TryResolveListViewRows(string controlField, out ListViewRowsPlan plan)
        {
            plan = null!;
            return false;
        }
    }

    private sealed class ViewTarget(
        FormModel formModel,
        ControlMappingRegistry controlMappings,
        ViewNavigationContext navigation,
        HandlerSignature signature,
        IReadOnlySet<string> dispatcherTimerFields,
        IReadOnlySet<string> componentFields,
        IReadOnlyDictionary<string, HelperCallInfo> promotedHelpers,
        IReadOnlySet<string> promotedFields,
        IReadOnlyDictionary<string, IReadOnlyList<ViewPropertyInfo>> viewProperties,
        IReadOnlySet<string> trayIconFields,
        IReadOnlyList<DataSourceBindingPlan> dataSourceBindings,
        IReadOnlyList<ListViewRowsPlan> listViewRows,
        IReadOnlyList<CheckedListPlan> checkedLists) : IRewriteTarget
    {
        public bool TryResolvePrintDocument(string fieldName, out string documentFieldName)
        {
            documentFieldName = "";

            if (!formModel.Controls.TryGetValue(fieldName, out var field))
            {
                return false;
            }

            if (field.ClrTypeName == "PrintDocument")
            {
                documentFieldName = fieldName;
                return true;
            }

            // `this.printPreviewDialog1.Document = this.printDocument1;` - the designer records
            // which document a dialog shows, so nothing has to be inferred from a handler.
            if (field.Properties.TryGetValue("Document", out var value)
                && value is PropertyValue.ControlReference(var documentField)
                && formModel.Controls.TryGetValue(documentField, out var document)
                && document.ClrTypeName == "PrintDocument")
            {
                documentFieldName = documentField;
                return true;
            }

            return false;
        }

        public bool TryResolveCheckedList(string controlField, out CheckedListPlan plan)
        {
            plan = checkedLists.FirstOrDefault(c =>
                string.Equals(c.ControlFieldName, controlField, StringComparison.Ordinal))!;

            return plan is not null;
        }

        public bool TryResolveDataSourceCollections(
            string bindingSourceField, out IReadOnlyList<DataSourceBindingPlan> plans)
        {
            plans =
            [
                .. dataSourceBindings.Where(b =>
                    string.Equals(b.SourceFieldName, bindingSourceField, StringComparison.Ordinal)),
            ];

            return plans.Count > 0;
        }

        public bool TryResolveListViewRows(string controlField, out ListViewRowsPlan plan)
        {
            plan = listViewRows.FirstOrDefault(r =>
                string.Equals(r.ControlFieldName, controlField, StringComparison.Ordinal))!;

            return plan is not null;
        }

        private readonly Dictionary<string, ControlModel> _senderAliases = new(StringComparer.Ordinal);

        public LocalScope Locals { get; } = new();

        public RewriteRequirements Requirements { get; } = new();

        public Dictionary<string, string> DialogSelections { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> DialogSeeds { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The generated method keeps the original parameter name, so a translated member reads
        /// naturally against the same `e` the developer wrote.
        /// </summary>
        public bool TryResolveEventArgsMember(ExpressionSyntax receiver, string memberName, out string text)
        {
            text = "";

            if (signature.EventArgsParameterName is not { } parameterName
                || receiver is not IdentifierNameSyntax { Identifier.ValueText: var receiverName }
                || receiverName != parameterName
                || !EventArgsMemberCatalog.TryGet(signature.EventArgsTypeName, memberName, out var member))
            {
                return false;
            }

            // A translation that needs the raising control is only possible when there is exactly
            // one - a shared handler has no single answer.
            if (member.NeedsSourceControl && signature.SourceControlFieldName is null)
            {
                return false;
            }

            text = string.Format(member.Format, parameterName, signature.SourceControlFieldName);
            return true;
        }

        public bool TryResolveEventArgsParameter(ExpressionSyntax expression, string argsTypeName, out string parameterName)
        {
            parameterName = "";

            if (signature.EventArgsParameterName is not { } name
                || expression is not IdentifierNameSyntax { Identifier.ValueText: var referenced }
                || referenced != name
                || signature.EventArgsTypeName != argsTypeName)
            {
                return false;
            }

            parameterName = name;
            return true;
        }

        public bool AllowsWindowApis => true;

        /// <summary>
        /// A cancellable event is read the moment the handler returns, and an `async void` handler
        /// returns at its first <c>await</c>. WinForms could get away with this because its dialogs
        /// block; Avalonia's do not, so `e.Cancel = await …` compiles, looks right, and never
        /// cancels anything - the window is already gone when the await resumes.
        /// </summary>
        /// <remarks>
        /// Refusing the whole statement is the honest answer rather than emitting the Avalonia
        /// pattern for it (cancel first, await, close again if confirmed): that restructures the
        /// handler into something the original never said, and this converter does not invent
        /// control flow. The prefix rule still applies, so everything before it comes across.
        /// </remarks>
        public bool AllowsAsync => signature.EventArgsTypeName != "WindowClosingEventArgs";

        public bool ReachesWindow => navigation.ReachesWindow;

        public string WindowExpression => navigation.ResolvedWindowExpression;

        public string StorageProviderExpression => navigation.HostIsWindow
            ? "StorageProvider"
            : "TopLevel.GetTopLevel(this)!.StorageProvider";

        public bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view) =>
            navigation.FormViews.TryGetValue(winFormsTypeName, out view!);

        public bool TryResolveControlField(ExpressionSyntax expression, out string fieldName)
        {
            // A local that aliases a control resolves to that control, so every path below this
            // one - properties, methods, the timer members - works through the alias unchanged.
            if (expression is IdentifierNameSyntax { Identifier.ValueText: var aliasName }
                && Locals.TryGetBinding(aliasName, out var binding)
                && binding is { Kind: LocalKind.Control, ControlFieldName: { } aliased })
            {
                fieldName = aliased;
                return true;
            }

            fieldName = expression switch
            {
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => "",
            };

            return fieldName.Length > 0 && TryGetControl(fieldName, out _);
        }

        /// <summary>
        /// The control still exists as an x:Name field, so the access survives - but only for a
        /// property with a proven Avalonia counterpart, on a control that really is that Avalonia
        /// element. A fallback control does not necessarily expose it.
        /// </summary>
        public bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text)
        {
            text = "";

            if (!TryGetControl(fieldName, out var control))
            {
                return false;
            }

            // A UserControl this project defines, hosted here as an element: its own public
            // surface is the vocabulary, not the catalog - the catalog only knows in-box types.
            // The name survives unchanged, because the generated View declares that same property.
            if (TryResolveViewProperty(control.ClrTypeName, winFormsPropertyName, forWrite))
            {
                text = $"{fieldName}.{winFormsPropertyName}";
                return true;
            }

            if (!BindablePropertyCatalog.TryGet(control.ClrTypeName, winFormsPropertyName, out var bindable)
                || !IsReachable(control, bindable.AvaloniaPropertyName))
            {
                return false;
            }

            // A three-state CheckBox reports Indeterminate as `Checked == true`, which no
            // coalescing of Avalonia's `bool?` reproduces - so it is refused rather than guessed.
            if (!forWrite
                && bindable.AvaloniaTypeName == "bool?"
                && control.Properties.TryGetValue("ThreeState", out var threeState)
                && threeState is PropertyValue.Literal { Value: true })
            {
                return false;
            }

            var access = $"{fieldName}.{bindable.AvaloniaPropertyName}";

            // A read has to come out as the type the WinForms expression had - Avalonia's member
            // is often nullable, or wider, where WinForms' was neither. The catalog says how.
            text = forWrite ? access : BindablePropertyCatalog.ReadExpression(access, bindable);
            return true;
        }

        /// <remarks>
        /// Resolved from the assignment target rather than passed in, so the write path asks the
        /// same question the read path does and the two cannot answer differently.
        /// </remarks>
        public bool TryResolveWrittenProperty(
            ExpressionSyntax left, out BindablePropertyCatalog.BindableProperty property)
        {
            property = default;

            return left is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var propertyName } access
                && TryResolveControlField(access.Expression, out var fieldName)
                && TryGetControl(fieldName, out var control)
                && BindablePropertyCatalog.TryGet(control.ClrTypeName, propertyName, out property);
        }

        public bool TryResolveFileDialog(string fieldName, out FileDialogKind kind)
        {
            kind = null!;

            return TryGetControl(fieldName, out var control)
                && FileDialogCatalog.TryGet(control.ClrTypeName, out kind);
        }

        /// <summary>
        /// Only the timers the plan actually emits count. A Timer with no Tick handler never
        /// becomes a field (the same evidence rule as everywhere else), so naming it here would
        /// produce code referring to something that does not exist.
        /// </summary>
        public bool IsDispatcherTimerField(string fieldName) => dispatcherTimerFields.Contains(fieldName);

        public bool IsComponentField(string fieldName) => componentFields.Contains(fieldName);

        public bool TryResolveControlTypeName(string fieldName, out string winFormsTypeName)
        {
            winFormsTypeName = TryGetControl(fieldName, out var control) ? control.ClrTypeName : "";
            return winFormsTypeName.Length > 0;
        }

        /// <remarks>
        /// The generated App is in the project's root namespace and the View in a child of it, so
        /// `App` resolves through the enclosing-namespace lookup with no using of its own - which
        /// is why this can be written without knowing what that namespace is called.
        /// </remarks>
        public bool TryResolveTrayIconAccessor(string fieldName, string winFormsPropertyName, out string text)
        {
            text = "";

            if (!trayIconFields.Contains(fieldName)
                || !TrayIconMemberCatalog.TryGet(winFormsPropertyName, out var avaloniaName))
            {
                return false;
            }

            text = $"App.{NamingConventions.Capitalize(fieldName)}.{avaloniaName}";
            return true;
        }

        public bool TryResolveComponentTypeName(string fieldName, out string winFormsTypeName)
        {
            winFormsTypeName = TryGetControl(fieldName, out var control) ? control.ClrTypeName : "";
            return winFormsTypeName.Length > 0;
        }

        public bool TryResolveFallbackTemplate(string fieldName, out string templateKey)
        {
            templateKey = "";

            if (!TryGetControl(fieldName, out var control))
            {
                return false;
            }

            var mapped = controlMappings.Map(control);
            templateKey = mapped is { Status: MappingStatus.Fallback, FallbackTemplateKey: { } key } ? key : "";
            return templateKey.Length > 0;
        }

        public bool IsMappedElement(string fieldName) =>
            TryGetControl(fieldName, out var control)
            && controlMappings.Map(control).Status is MappingStatus.Direct or MappingStatus.Fallback;

        public bool TryResolveMappedElementName(string fieldName, out string avaloniaElementName)
        {
            avaloniaElementName = TryGetControl(fieldName, out var control)
                && controlMappings.Map(control) is { Status: MappingStatus.Direct, AvaloniaElementName: { } name }
                ? name
                : "";

            return avaloniaElementName.Length > 0;
        }

        /// <summary>
        /// Whether this control's target can carry a style group - the same question, and now the
        /// same answer, the emitter asks before writing the attribute.
        /// </summary>
        public bool SupportsStyleProperty(string fieldName, AvaloniaStyleProperties property)
        {
            if (!TryGetControl(fieldName, out var control))
            {
                return false;
            }

            var mapped = controlMappings.Map(control);

            // A bundled template is not a Direct mapping, but it is *ours*: what it exposes is a
            // known fact, the same argument that lets a fallback carry a bindable property.
            if (mapped.Status == MappingStatus.Fallback)
            {
                return AvaloniaStylePropertySupport
                    .ForFallbackTemplate(mapped.FallbackTemplateKey)
                    .HasFlag(property);
            }

            return mapped.Status == MappingStatus.Direct
                && AvaloniaStylePropertySupport.Supports(mapped.AvaloniaElementName, property);
        }

        public bool TryResolveHelperCall(string methodName, int argumentCount, out HelperCallInfo helper) =>
            promotedHelpers.TryGetValue(methodName, out helper!) && helper.ParameterCount == argumentCount;

        public bool IsPromotedField(string name) => promotedFields.Contains(name);

        public bool TryResolveViewProperty(string winFormsTypeName, string propertyName, bool forWrite) =>
            viewProperties.TryGetValue(winFormsTypeName, out var properties)
            && properties.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.Ordinal))
                is { } property
            && (!forWrite || property.HasSetter);

        /// <summary>
        /// Every control this body may name: the View's own fields, plus any local a
        /// <c>(Button)sender</c> cast introduced for a handler wired to several of them.
        /// </summary>
        /// <remarks>
        /// A sender alias is a control for every purpose that matters here - which element it is,
        /// which of its members survive - it just has no field of its own, so the emitted text
        /// uses the local's name. Routing every lookup through here is what keeps the rest of the
        /// translation from having to know the difference.
        /// </remarks>
        private bool TryGetControl(string name, out ControlModel control) =>
            _senderAliases.TryGetValue(name, out control!) || formModel.Controls.TryGetValue(name, out control!);

        /// <summary>
        /// <c>var button = (Button)sender!;</c> resolved to the control the local stands for.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Wired to exactly one control, <c>sender</c> provably *is* that control, so the local
        /// becomes another name for its field and the declaration disappears entirely.
        /// </para>
        /// <para>
        /// Wired to several - one handler on N buttons, which is how WinForms shares a handler at
        /// all - there is no single field to alias, so the cast survives, against the *Avalonia*
        /// element type. It is admissible only when every wired control maps to the same element:
        /// then the cast is exactly as valid as the original's was, and everything the body goes
        /// on to say about the local is checked against that one type. Mixed types stay refused -
        /// telling them apart is the whole reason such a handler reads `sender`.
        /// </para>
        /// <para>
        /// Either way the cast has to name the control's own WinForms type. A base type
        /// (<c>(Control)sender</c>) is refused rather than widened: the body is checked against
        /// the actual control regardless, so accepting the wider cast would only let the
        /// translated code claim something the original did not.
        /// </para>
        /// </remarks>
        public bool TryResolveSenderCast(TypeSyntax castType, string localName, out string fieldName, out string statement)
        {
            fieldName = "";
            statement = "";

            var castTypeName = RoslynTypeNameHelper.GetSimpleTypeName(castType);
            var sources = signature.SourceControlFieldNames
                .Select(f => TryGetControl(f, out var c) ? c : null)
                .OfType<ControlModel>()
                .ToList();

            if (sources.Count != signature.SourceControlFieldNames.Count
                || sources.Count == 0
                || sources.Any(c => c.ClrTypeName != castTypeName))
            {
                return false;
            }

            if (sources is [{ } single])
            {
                fieldName = single.FieldName;
                return true;
            }

            var elements = sources.Select(controlMappings.Map).ToList();
            if (elements.Any(m => m.Status != MappingStatus.Direct)
                || elements.Select(m => m.AvaloniaElementName).Distinct(StringComparer.Ordinal).Count() != 1)
            {
                return false;
            }

            _senderAliases[localName] = sources[0];
            fieldName = localName;

            // `sender` is an `object?` on the Avalonia side, and the generated project enables
            // nullable - the null-forgiving operator is what keeps the cast warning-free.
            statement = $"var {localName} = ({elements[0].AvaloniaElementName})sender!;";
            return true;
        }

        public bool TryResolveControlMethod(
            string fieldName, string methodName, IReadOnlyList<string> arguments, out string statement)
        {
            statement = "";

            if (!TryGetControl(fieldName, out var control)
                || !ControlMethodCatalog.TryGet(control.ClrTypeName, methodName, out var method)
                || method.ArgumentCount != arguments.Count
                // The member the *translation* touches, not the WinForms method it came from:
                // AppendText reaches Text, which a fallback template may well expose even though
                // it has no AppendText of its own.
                || !IsReachable(control, method.AvaloniaMemberName))
            {
                return false;
            }

            // The resolved access, not the field: `logTextBox.Text` here, the generated
            // `LogTextBoxText` on a ViewModel. Built directly rather than through
            // TryResolveProperty, because a compound assignment needs an assignable left-hand
            // side and the read path null-guards.
            statement = string.Format(method.StatementFormat, [$"{fieldName}.{method.AvaloniaMemberName}", .. arguments]);
            return true;
        }

        /// <summary>
        /// A Direct-mapped element is the Avalonia control itself, so the catalog's answer is
        /// authoritative. A Fallback element is one of this repo's own templates, and only the
        /// members it demonstrably exposes are safe to touch.
        /// </summary>
        /// <summary>
        /// Whether a translated statement may touch this member of this control at all.
        /// </summary>
        /// <remarks>
        /// A target the AXAML emits without an <c>x:Name</c> - a DataGrid column, which is a
        /// description of a column rather than an element in the visual tree - has no field on the
        /// generated View, so naming it in code is a CS0103 there whatever the member is.
        /// </remarks>
        private bool IsReachable(ControlModel control, string avaloniaMemberName)
        {
            var mapped = controlMappings.Map(control);

            if (!mapped.SupportsName)
            {
                return false;
            }

            if (mapped.UnreachableBindableMembers.Contains(avaloniaMemberName, StringComparer.Ordinal))
            {
                return false;
            }

            return mapped.Status == MappingStatus.Direct
                || (mapped.Status == MappingStatus.Fallback
                    && FallbackControlMemberSupport.Exposes(mapped.FallbackTemplateKey, avaloniaMemberName));
        }
    }

    private sealed class ViewModelTarget(
        IReadOnlyList<BoundPropertyPlan> boundProperties,
        FormModel formModel,
        IReadOnlyDictionary<string, HelperCallInfo> promotedHelpers) : IRewriteTarget
    {
        public LocalScope Locals { get; } = new();

        public RewriteRequirements Requirements { get; } = new();

        public Dictionary<string, string> DialogSelections { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> DialogSeeds { get; } = new(StringComparer.Ordinal);

        public bool AllowsWindowApis => false;

        /// <summary>A promoted command cannot touch EventArgs at all, so nothing here is cancellable.</summary>
        public bool AllowsAsync => true;

        public bool ReachesWindow => false;

        public string WindowExpression => "this";

        /// <summary>Never reached - a ViewModel refuses the file dialogs outright.</summary>
        public string StorageProviderExpression => "StorageProvider";

        /// <summary>A ViewModel has no business constructing Views; navigation needs a service.</summary>
        public bool TryResolveFormView(string winFormsTypeName, out FormViewInfo view)
        {
            view = null!;
            return false;
        }

        /// <summary>
        /// A ViewModel has no Views to reach into - the whole point of a promoted command is that
        /// it names nothing visual.
        /// </summary>
        public bool TryResolveViewProperty(string winFormsTypeName, string propertyName, bool forWrite) => false;

        /// <summary>
        /// Only the entries whose Avalonia member is a property this plan actually bound.
        /// <c>AppendText</c> qualifies - it is a write to <c>Text</c> wearing a method's clothes -
        /// while <c>Focus()</c> has no ViewModel form at all and refuses.
        /// </summary>
        public bool TryResolveControlMethod(
            string fieldName, string methodName, IReadOnlyList<string> arguments, out string statement)
        {
            statement = "";

            if (!formModel.Controls.TryGetValue(fieldName, out var control)
                || !ControlMethodCatalog.TryGet(control.ClrTypeName, methodName, out var method)
                || method.ArgumentCount != arguments.Count)
            {
                return false;
            }

            var bound = boundProperties.FirstOrDefault(p =>
                p.ControlFieldName == fieldName && p.AvaloniaPropertyName == method.AvaloniaMemberName);

            if (bound is null)
            {
                return false;
            }

            statement = string.Format(method.StatementFormat, [bound.ViewModelPropertyName, .. arguments]);
            return true;
        }

        /// <summary>StorageProvider hangs off the TopLevel, which a ViewModel is not.</summary>
        public bool TryResolveFileDialog(string fieldName, out FileDialogKind kind)
        {
            kind = null!;
            return false;
        }

        /// <summary>The DispatcherTimer field lives on the View; a ViewModel cannot see it.</summary>
        public bool IsDispatcherTimerField(string fieldName) => false;

        /// <summary>Component fields live on the View too.</summary>
        public bool IsComponentField(string fieldName) => false;

        /// <summary>A ViewModel names no components at all.</summary>
        public bool TryResolveComponentTypeName(string fieldName, out string winFormsTypeName)
        {
            winFormsTypeName = "";
            return false;
        }

        /// <summary>A ViewModel has no elements, so nothing here maps to one.</summary>
        public bool TryResolveFallbackTemplate(string fieldName, out string templateKey)
        {
            templateKey = "";
            return false;
        }

        public bool IsMappedElement(string fieldName) => false;

        /// <summary>A promoted body names no elements, only ViewModel properties.</summary>
        public bool TryResolveMappedElementName(string fieldName, out string avaloniaElementName)
        {
            avaloniaElementName = "";
            return false;
        }

        /// <summary>Styling is an element concern, and a ViewModel has no elements.</summary>
        public bool SupportsStyleProperty(string fieldName, AvaloniaStyleProperties property) => false;

        /// <summary>
        /// A helper that moved to the ViewModel along with the command that calls it - which is
        /// only ever the case when the promotion analysis already proved the helper's whole body
        /// is expressible here.
        /// </summary>
        public bool TryResolveHelperCall(string methodName, int argumentCount, out HelperCallInfo helper) =>
            promotedHelpers.TryGetValue(methodName, out helper!) && helper.ParameterCount == argumentCount;

        /// <summary>The Form's own fields stay on the View.</summary>
        public bool IsPromotedField(string name) => false;

        /// <summary>A promoted handler has no sender - that is one of the promotion conditions.</summary>
        public bool TryResolveSenderCast(TypeSyntax castType, string localName, out string fieldName, out string statement)
        {
            fieldName = "";
            statement = "";
            return false;
        }

        /// <summary>A promoted handler cannot use EventArgs at all - that is one of the promotion conditions.</summary>
        public bool TryResolveEventArgsMember(ExpressionSyntax receiver, string memberName, out string text)
        {
            text = "";
            return false;
        }

        public bool TryResolveEventArgsParameter(ExpressionSyntax expression, string argsTypeName, out string parameterName)
        {
            parameterName = "";
            return false;
        }

        public bool TryResolveControlField(ExpressionSyntax expression, out string fieldName)
        {
            fieldName = expression switch
            {
                MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.ValueText: var name } => name,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => "",
            };

            var candidate = fieldName;
            return candidate.Length > 0 && boundProperties.Any(p => p.ControlFieldName == candidate);
        }

        /// <summary>
        /// There is no control here: the only thing a promoted body may name is an
        /// [ObservableProperty] the plan created for it, which is exactly the set of
        /// BoundPropertyPlans - so nothing else can resolve, by construction.
        /// </summary>
        /// <remarks>
        /// No null-guard needed on reads, unlike the View: the generated [ObservableProperty] for
        /// a string is non-nullable and initialized to string.Empty.
        /// </remarks>
        /// <summary>A promoted body names no controls, only ViewModel properties.</summary>
        public bool TryResolveControlTypeName(string fieldName, out string winFormsTypeName)
        {
            winFormsTypeName = "";
            return false;
        }

        /// <summary>A ViewModel has no App to reach into - that is what promotion means.</summary>
        public bool TryResolveTrayIconAccessor(string fieldName, string winFormsPropertyName, out string text)
        {
            text = "";
            return false;
        }

        /// <summary>
        /// Never: a property whose value shape differs is not two-way bindable, so no promoted
        /// command can have been planned against one.
        /// </summary>
        public bool TryResolveWrittenProperty(
            ExpressionSyntax left, out BindablePropertyCatalog.BindableProperty property)
        {
            property = default;
            return false;
        }

        public bool TryResolveProperty(string fieldName, string winFormsPropertyName, bool forWrite, out string text)
        {
            text = "";

            // BoundPropertyPlan is keyed by the *Avalonia* property, so the WinForms name has to
            // go through the catalog first - with the control's own type, which only the
            // FormModel still knows at this point.
            if (!formModel.Controls.TryGetValue(fieldName, out var control)
                || !BindablePropertyCatalog.TryGet(control.ClrTypeName, winFormsPropertyName, out var bindable))
            {
                return false;
            }

            var bound = boundProperties.FirstOrDefault(p =>
                p.ControlFieldName == fieldName && p.AvaloniaPropertyName == bindable.AvaloniaPropertyName);

            if (bound is null)
            {
                return false;
            }

            text = bound.ViewModelPropertyName;
            return true;
        }
    }
}
