using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace WinFormsToAvalonia.Core.Semantics;

/// <summary>
/// Decides whether a piece of the original source is <em>UI-neutral</em> - provably the same code
/// in the generated Avalonia project - and renders it there.
/// </summary>
/// <remarks>
/// <para>
/// The compilation it reads from references the .NET base class library and nothing else (see
/// <see cref="ReferenceAssemblies"/>), so every WinForms type binds to an error type. A node is
/// neutral when:
/// </para>
/// <list type="bullet">
///   <item>every name in it binds, unambiguously, and no expression has an error type;</item>
///   <item>nothing it names lives in <c>System.Windows.Forms</c> or <c>System.Drawing</c> - the
///     second matters because <c>Color</c>/<c>Point</c>/<c>Size</c> are part of the BCL and
///     <em>do</em> bind, while meaning something Avalonia spells differently;</item>
///   <item>a type declared in the project's own source is one the generated project really
///     carries (<see cref="SemanticProjectContext.CarriedFiles"/>), and a member of the Form itself
///     is one the generated View really declares (the caller's <c>ownMemberAllowed</c>);</item>
///   <item>nothing it touches is <c>[Obsolete]</c>, Windows-only or experimental - each of those is
///     a warning in a generated project that has to build warning-free.</item>
/// </list>
/// <para>
/// This is the same "only what is provably equivalent" bar the rest of the converter holds, with
/// the proof supplied by the compiler instead of a hand-written list - which is why it is allowed
/// to pass code through verbatim that the syntactic rules refused only because they could not see.
/// </para>
/// </remarks>
internal sealed class NeutralityChecker
{
    /// <summary>
    /// Namespaces whose types the generated file can name by their simple name: <c>System</c>, and
    /// the implicit global usings an SDK-style <c>net10.0</c> project has. Every other type a
    /// rendered node names is written namespace-qualified, which is what keeps it from binding to
    /// an Avalonia type of the same name (<c>Avalonia.Controls.Image</c>, <c>Calendar</c>, ...).
    /// </summary>
    internal static readonly IReadOnlySet<string> ImplicitNamespaces = new HashSet<string>(StringComparer.Ordinal)
    {
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    };

    private static readonly string[] UiNamespaces =
    [
        "System.Windows.Forms",
        "System.Drawing",
        // WPF and WinRT interop: the same "a UI type Avalonia spells differently" problem.
        "System.Windows",
        "Windows",
    ];

    private readonly SemanticModel _model;
    private readonly INamedTypeSymbol? _owner;
    private readonly Func<ISymbol, bool> _ownMemberAllowed;
    private readonly bool _refuseMethodParameters;
    private readonly Func<SyntaxTree, bool> _isTreeCarried;
    private readonly IReadOnlySet<string> _namespacesInScope;
    private readonly Dictionary<ITypeSymbol, bool> _uiTypes = new(SymbolEqualityComparer.Default);

    /// <param name="owner">The Form/UserControl class the code belongs to, or null for a free-standing file.</param>
    /// <param name="ownMemberAllowed">Whether a member of <paramref name="owner"/> exists on the generated View.</param>
    /// <param name="refuseMethodParameters">
    /// True for an event handler: its <c>sender</c> and <c>EventArgs</c> parameters are Avalonia
    /// objects in the generated method, so code that names them means something else there.
    /// </param>
    /// <param name="isTreeCarried">Whether a source file is copied into the generated project.</param>
    /// <param name="namespacesInScope">
    /// Namespaces whose extension methods the generated file sees - the implicit usings plus the
    /// namespaces enclosing the generated class.
    /// </param>
    public NeutralityChecker(
        SemanticModel model,
        INamedTypeSymbol? owner,
        Func<ISymbol, bool> ownMemberAllowed,
        bool refuseMethodParameters,
        Func<SyntaxTree, bool> isTreeCarried,
        IReadOnlySet<string> namespacesInScope)
    {
        _model = model;
        _owner = owner;
        _ownMemberAllowed = ownMemberAllowed;
        _refuseMethodParameters = refuseMethodParameters;
        _isTreeCarried = isTreeCarried;
        _namespacesInScope = namespacesInScope;
    }

    /// <summary>
    /// The type a local really has where this compilation cannot know it: a local the rewriter
    /// declared from a WinForms call it translated (<c>var column = tableLayoutPanel1.GetColumn(label);</c>
    /// is an <c>int</c>, but binds to an error type here). <see cref="SpecialType.None"/> for any other.
    /// </summary>
    /// <remarks>
    /// Taken only as a whole argument of a call the model did resolve, and only when that type
    /// converts implicitly to the parameter's - see <see cref="IsVouchedArgument"/>.
    /// </remarks>
    public Func<ILocalSymbol, SpecialType>? VouchedLocalType { get; init; }

    /// <summary>The first reason a <see cref="Check"/> failed, for the report: the code, and why.</summary>
    public string? FirstProblem { get; private set; }

    /// <summary>Why the innermost check that failed did - set where the decision is made.</summary>
    private string? _why;

    /// <summary>Whether the node contains an <c>await</c> of its own (not one inside a lambda).</summary>
    public bool UsesAwait { get; private set; }

    public bool Check(SyntaxNode node)
    {
        FirstProblem = null;
        UsesAwait = false;

        // `this` on its own is the Form - the generated View is a different object. It is only
        // ever acceptable as the receiver of one of the Form's own members, judged with that member.
        if (node is ThisExpressionSyntax or BaseExpressionSyntax)
        {
            FirstProblem = $"'{node}' (the Form itself, which the generated View is not)";
            return false;
        }

        foreach (var descendant in node.DescendantNodesAndSelf(n => n is not UsingDirectiveSyntax))
        {
            _why = null;
            if (!CheckNode(descendant))
            {
                FirstProblem ??= $"'{Describe(descendant)}' ({_why ?? "uses WinForms, System.Drawing, or a type that did not come across"})";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// <see cref="Check"/>, then the node's text with every non-implicit type reference written
    /// namespace-qualified, and continuation lines dedented to the node's own indentation.
    /// </summary>
    public bool TryRender(SyntaxNode node, out string text)
    {
        text = "";

        if (!Check(node) || !TryCollectQualifications(node, out var replacements))
        {
            return false;
        }

        var original = node.ToString();
        var start = node.SpanStart;
        var builder = new System.Text.StringBuilder(original);

        foreach (var (span, replacement) in replacements.OrderByDescending(r => r.Span.Start))
        {
            builder.Remove(span.Start - start, span.Length).Insert(span.Start - start, replacement);
        }

        text = Dedent(builder.ToString(), IndentationOf(node));
        return true;
    }

    private bool CheckNode(SyntaxNode node)
    {
        switch (node)
        {
            // A jump whose target may be outside the node, and code this converter never needs.
            case GotoStatementSyntax or YieldStatementSyntax or UnsafeStatementSyntax or FixedStatementSyntax
                or StackAllocArrayCreationExpressionSyntax or MakeRefExpressionSyntax or RefTypeExpressionSyntax
                or RefValueExpressionSyntax:
                return false;

            case AwaitExpressionSyntax when !IsInsideFunction(node):
                UsesAwait = true;
                break;

            case ForEachStatementSyntax { AwaitKeyword.RawKind: not 0 } or UsingStatementSyntax { AwaitKeyword.RawKind: not 0 }
                or LocalDeclarationStatementSyntax { AwaitKeyword.RawKind: not 0 } when !IsInsideFunction(node):
                UsesAwait = true;
                break;
        }

        // Names that are labels rather than references - a named argument, an anonymous type's
        // member - bind to nothing an expression could reach.
        if (node is IdentifierNameSyntax { Parent: NameColonSyntax or NameEqualsSyntax })
        {
            return true;
        }

        // `string[]`'s empty rank and `List<>`'s empty argument are placeholders, not expressions:
        // they report an error type of their own while meaning nothing.
        if (IsNameofKeyword(node) || node is OmittedArraySizeExpressionSyntax or OmittedTypeArgumentSyntax)
        {
            return true;
        }

        switch (node)
        {
            case ParameterSyntax or VariableDeclaratorSyntax or SingleVariableDesignationSyntax
                or ForEachStatementSyntax or CatchDeclarationSyntax or LocalFunctionStatementSyntax:
                return _model.GetDeclaredSymbol(node) switch
                {
                    ILocalSymbol local => IsUsableType(local.Type),
                    IParameterSymbol parameter => IsUsableType(parameter.Type),
                    IMethodSymbol localFunction => IsUsableType(localFunction.ReturnType),
                    _ => node is not ForEachStatementSyntax,
                };

            case ExpressionSyntax expression:
                return CheckExpression(expression);

            default:
                return true;
        }
    }

    private bool CheckExpression(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax
            && VouchedLocalType is { } vouched
            && _model.GetSymbolInfo(expression).Symbol is ILocalSymbol { Type.TypeKind: TypeKind.Error } errorLocal
            && vouched(errorLocal) is var special and not SpecialType.None)
        {
            _why = "a value this compilation cannot type, used other than as a plain argument";
            return IsVouchedArgument(expression, special);
        }

        // `this.field` is judged by the member, not by `this` - whose type is the Form.
        var isThisReceiver = expression is ThisExpressionSyntax
            && expression.Parent is MemberAccessExpressionSyntax access
            && access.Expression == expression;

        if (!isThisReceiver)
        {
            var typeInfo = _model.GetTypeInfo(expression);
            if ((typeInfo.Type is { } type && !IsUsableType(type))
                || (typeInfo.ConvertedType is { } converted && !IsUsableType(converted)))
            {
                return false;
            }
        }

        if (expression is ThisExpressionSyntax or BaseExpressionSyntax)
        {
            return isThisReceiver;
        }

        var symbolInfo = _model.GetSymbolInfo(expression);
        if ((symbolInfo.Symbol ?? ResolveCallWithVouchedArguments(expression)) is { } symbol)
        {
            // `this.GetType()`, `this.ToString()`: an inherited member of `this` is asked of the
            // generated View, a different object of a different type. Only the Form's own members
            // mean the same thing there.
            if (_owner is not null
                && expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax }
                && !IsOwnMember(symbol))
            {
                return false;
            }

            return IsUsableSymbol(symbol);
        }

        // An ambiguity, an overload that did not resolve, an inaccessible member: the compiler
        // could not decide, so neither can this.
        if (!symbolInfo.CandidateSymbols.IsEmpty)
        {
            return false;
        }

        // A name that binds to nothing at all is a WinForms type or member - the reference this
        // compilation deliberately lacks.
        return expression is not SimpleNameSyntax;
    }

    /// <summary>
    /// The member a call would have bound to had its vouched-for arguments carried their real
    /// type: an error-typed argument fails overload resolution outright, which leaves the
    /// candidates and no symbol. Answered for the call itself, and for the method name it calls.
    /// </summary>
    /// <remarks>
    /// Exactly one candidate may take that many arguments - the real types could pick between two,
    /// and this cannot - and it may not be generic. Every other argument has to convert as it is.
    /// </remarks>
    private IMethodSymbol? ResolveCallWithVouchedArguments(ExpressionSyntax expression)
    {
        if (VouchedLocalType is not { } vouched)
        {
            return null;
        }

        var callee = expression is SimpleNameSyntax { Parent: MemberAccessExpressionSyntax access } name && access.Name == name
            ? access
            : expression;
        ExpressionSyntax? call = expression is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
            ? expression
            : callee.Parent is InvocationExpressionSyntax invocation && invocation.Expression == callee ? invocation : null;
        var list = call switch
        {
            InvocationExpressionSyntax invoked => invoked.ArgumentList,
            BaseObjectCreationExpressionSyntax created => created.ArgumentList,
            _ => null,
        };
        if (call is null
            || list is null
            || _model.GetSymbolInfo(call) is not { Symbol: null, CandidateReason: CandidateReason.OverloadResolutionFailure } failed)
        {
            return null;
        }

        var fitting = failed.CandidateSymbols
            .OfType<IMethodSymbol>()
            .Where(m => m.Parameters.Length == list.Arguments.Count)
            .ToList();
        if (fitting is not [{ IsGenericMethod: false } method])
        {
            return null;
        }

        for (var i = 0; i < list.Arguments.Count; i++)
        {
            var argument = list.Arguments[i];
            var parameter = method.Parameters[i];
            if (argument is not { NameColon: null, RefKindKeyword.RawKind: 0 } || parameter is { IsParams: true } or { RefKind: not RefKind.None })
            {
                return null;
            }

            var fits = _model.GetSymbolInfo(argument.Expression).Symbol is ILocalSymbol { Type.TypeKind: TypeKind.Error } local
                && vouched(local) is var special and not SpecialType.None
                    ? _model.Compilation.ClassifyConversion(_model.Compilation.GetSpecialType(special), parameter.Type) is { IsImplicit: true, IsUserDefined: false }
                    : _model.ClassifyConversion(argument.Expression, parameter.Type) is { IsImplicit: true, IsUserDefined: false };
            if (!fits)
            {
                return null;
            }
        }

        return method;
    }

    /// <summary>
    /// <c>Foo(column)</c>: the local is a whole, positional, by-value argument of a call (or
    /// <c>new</c>) the model resolved to exactly one member, and its real type converts implicitly
    /// to that parameter's. An error-typed argument fits any parameter, so a call it could have
    /// resolved differently has more than one candidate - and no symbol, which refuses.
    /// </summary>
    private bool IsVouchedArgument(ExpressionSyntax expression, SpecialType special)
    {
        if (expression.Parent is not ArgumentSyntax { NameColon: null, RefKindKeyword.RawKind: 0 } argument
            || argument.Parent is not ArgumentListSyntax { Parent: InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax } list
            || list.Parent is not ExpressionSyntax call
            || (_model.GetSymbolInfo(call).Symbol ?? ResolveCallWithVouchedArguments(call)) is not IMethodSymbol method)
        {
            return false;
        }

        var index = list.Arguments.IndexOf(argument);
        if (index < 0 || index >= method.Parameters.Length || method.Parameters[index] is { IsParams: true } or { RefKind: not RefKind.None })
        {
            return false;
        }

        var conversion = _model.Compilation.ClassifyConversion(_model.Compilation.GetSpecialType(special), method.Parameters[index].Type);
        return conversion is { IsImplicit: true, IsUserDefined: false };
    }

    private bool IsUsableSymbol(ISymbol symbol)
    {
        switch (symbol)
        {
            case IAliasSymbol alias:
                return IsUsableSymbol(alias.Target);

            case IDiscardSymbol or ILabelSymbol or IRangeVariableSymbol or ITypeParameterSymbol or IPreprocessingSymbol:
                return true;

            case ILocalSymbol local:
                return IsUsableType(local.Type);

            case IParameterSymbol parameter:
                if (_refuseMethodParameters
                    && parameter.ContainingSymbol is IMethodSymbol { MethodKind: not (MethodKind.AnonymousFunction or MethodKind.LocalFunction) })
                {
                    _why = "the handler's own parameters are Avalonia objects in the generated method";
                    return false;
                }

                return IsUsableType(parameter.Type);

            case INamespaceSymbol ns:
                return !IsUiNamespace(ns);

            case ITypeSymbol type:
                return IsUsableType(type);
        }

        if (IsOwnMember(symbol))
        {
            _why = "a member of the Form that the generated View does not carry";
            return _ownMemberAllowed(symbol);
        }

        if (symbol is IMethodSymbol { ReducedFrom: { } extension } reduced)
        {
            // An extension method is found through a using, so its namespace has to be one the
            // generated file imports - qualifying a call's *receiver* cannot help it.
            var extensionNamespace = extension.ContainingNamespace?.ToDisplayString() ?? "";
            if (!_namespacesInScope.Contains(extensionNamespace))
            {
                return false;
            }

            return IsUsableMember(extension) && reduced.TypeArguments.All(IsUsableType);
        }

        if (symbol is IMethodSymbol method && !method.TypeArguments.All(IsUsableType))
        {
            return false;
        }

        return IsUsableMember(symbol);
    }

    private bool IsUsableMember(ISymbol member)
    {
        if (HasBlockingAttribute(member))
        {
            _why = "an [Obsolete], Windows-only or experimental API, which would be a warning in the generated project";
            return false;
        }

        if (member.ContainingType is { } containing && !IsUsableType(containing))
        {
            return false;
        }

        return member switch
        {
            IMethodSymbol method => IsUsableType(method.ReturnType) && method.Parameters.All(p => IsUsableType(p.Type)),
            IPropertySymbol property => IsUsableType(property.Type),
            IFieldSymbol field => IsUsableType(field.Type),
            IEventSymbol @event => IsUsableType(@event.Type),
            _ => false,
        };
    }

    /// <summary>A member declared by the Form itself, or by one of its base Forms in this project.</summary>
    private bool IsOwnMember(ISymbol symbol)
    {
        if (_owner is null || symbol.ContainingType is not { } containing)
        {
            return false;
        }

        for (INamedTypeSymbol? current = _owner; current is not null && current.TypeKind != TypeKind.Error; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, containing.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Not a WinForms/System.Drawing type, bound, available in the generated project, not obsolete.</summary>
    internal bool IsUsableType(ITypeSymbol type)
    {
        if (_uiTypes.TryGetValue(type, out var isUi))
        {
            return !isUi;
        }

        // Seeded before recursing, so a type whose definition mentions itself terminates.
        _uiTypes[type] = false;
        isUi = !ComputeUsable(type);
        _uiTypes[type] = isUi;
        return !isUi;
    }

    private bool ComputeUsable(ITypeSymbol type)
    {
        switch (type)
        {
            case { TypeKind: TypeKind.Error or TypeKind.Dynamic or TypeKind.Pointer or TypeKind.FunctionPointer }:
                return false;

            case IArrayTypeSymbol array:
                return IsUsableType(array.ElementType);

            case ITypeParameterSymbol:
                return true;

            case INamedTypeSymbol named:
                if (IsUiNamespace(named.ContainingNamespace))
                {
                    _why = "a WinForms or System.Drawing type";
                    return false;
                }

                if (HasBlockingAttribute(named))
                {
                    _why = "an [Obsolete], Windows-only or experimental type, which would be a warning in the generated project";
                    return false;
                }

                if (!named.TypeArguments.All(IsUsableType))
                {
                    return false;
                }

                if (named.ContainingType is { } containing && !IsUsableType(containing))
                {
                    return false;
                }

                // A class of the project's own deriving from a WinForms type (`MyButton : Button`,
                // the Form itself) is a WinForms type too: its base is an error type here.
                if (named.BaseType is { } baseType && !IsUsableType(baseType))
                {
                    return false;
                }

                if (named.Interfaces.Any(i => !IsUsableType(i)))
                {
                    return false;
                }

                return IsAvailable(named.OriginalDefinition);

            default:
                return true;
        }
    }

    /// <summary>A type of the project's own source exists in the generated project only if its file was carried.</summary>
    private bool IsAvailable(INamedTypeSymbol definition)
    {
        if (definition.DeclaringSyntaxReferences.IsEmpty)
        {
            // Metadata: the base class library, which the generated project references too.
            return true;
        }

        return definition.DeclaringSyntaxReferences.All(r => _isTreeCarried(r.SyntaxTree));
    }

    private static bool IsUiNamespace(INamespaceSymbol? ns)
    {
        if (ns is null || ns.IsGlobalNamespace)
        {
            return false;
        }

        var name = ns.ToDisplayString();
        return UiNamespaces.Any(ui => name == ui || name.StartsWith(ui + ".", StringComparison.Ordinal));
    }

    /// <summary>
    /// <c>[Obsolete]</c> (a CS0618 warning), <c>[SupportedOSPlatform]</c> (CA1416) and
    /// <c>[Experimental]</c> (an error unless suppressed) - on the symbol, the types containing it,
    /// or its assembly.
    /// </summary>
    private static bool HasBlockingAttribute(ISymbol symbol)
    {
        for (var current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.GetAttributes().Any(IsBlocking))
            {
                return true;
            }
        }

        return symbol.ContainingAssembly?.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "SupportedOSPlatformAttribute") == true;

        static bool IsBlocking(AttributeData attribute) =>
            attribute.AttributeClass?.Name is "ObsoleteAttribute" or "SupportedOSPlatformAttribute"
                or "ExperimentalAttribute" or "RequiresPreviewFeaturesAttribute";
    }

    private static bool IsInsideFunction(SyntaxNode node) =>
        node.Ancestors().TakeWhile(a => a is not MemberDeclarationSyntax)
            .Any(a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax);

    private bool IsNameofKeyword(SyntaxNode node) =>
        node is IdentifierNameSyntax { Identifier.ValueText: "nameof", Parent: InvocationExpressionSyntax invocation }
        && invocation.Expression == node
        && _model.GetSymbolInfo(invocation).Symbol is null;

    /// <summary>
    /// The type and namespace references that must be written qualified in the generated file:
    /// every leftmost simple name that binds to a type outside the implicit namespaces, or to a
    /// namespace (which the original may have spelled relative to its own).
    /// </summary>
    private bool TryCollectQualifications(SyntaxNode node, out List<(TextSpan Span, string Text)> replacements)
    {
        replacements = [];

        foreach (var name in node.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
        {
            if (!IsLeftmost(name) || name.Parent is NameColonSyntax or NameEqualsSyntax || IsNameofKeyword(name)
                || name is IdentifierNameSyntax { IsVar: true } || IsInitializedMember(name))
            {
                continue;
            }

            if (_model.GetAliasInfo(name) is { } alias)
            {
                switch (alias.Target)
                {
                    case INamespaceSymbol aliasedNamespace:
                        replacements.Add((name.Span, aliasedNamespace.ToDisplayString()));
                        continue;
                    case INamedTypeSymbol aliasedType when QualifiedName(aliasedType) is { } aliasedName:
                        replacements.Add((name.Span, aliasedType.IsGenericType
                            ? aliasedType.ToDisplayString(QualifiedFormat)
                            : aliasedName));
                        continue;
                    default:
                        return false;
                }
            }

            switch (_model.GetSymbolInfo(name).Symbol)
            {
                case INamespaceSymbol ns:
                    var full = ns.ToDisplayString();
                    if (!string.Equals(full, name.Identifier.ValueText, StringComparison.Ordinal))
                    {
                        replacements.Add((name.Span, full));
                    }

                    break;

                case INamedTypeSymbol type:
                    if (type.ContainingType is null
                        && ImplicitNamespaces.Contains(type.ContainingNamespace?.ToDisplayString() ?? ""))
                    {
                        break;
                    }

                    if (QualifiedName(type) is not { } qualified)
                    {
                        return false;
                    }

                    replacements.Add((name.Identifier.Span, qualified));
                    break;

                // A member reached by its simple name: the Form's own (the View declares it under the
                // same name), a local, a parameter. Anything else got there through a `using static`
                // the generated file does not have.
                case IFieldSymbol or IPropertySymbol or IMethodSymbol or IEventSymbol when name.Parent is not MemberBindingExpressionSyntax:
                    var member = _model.GetSymbolInfo(name).Symbol!;
                    if (!IsOwnMember(member) && !IsDeclaredInside(member, node))
                    {
                        return false;
                    }

                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// <c>Total</c> in <c>new Snapshot { Total = total }</c>: a member of the object being created,
    /// named relative to it - never a simple name that needs qualifying, or that a <c>using static</c> brought in.
    /// </summary>
    private static bool IsInitializedMember(SimpleNameSyntax name) =>
        name.Parent is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax initializer } assignment
        && assignment.Left == name
        && initializer.Kind() is SyntaxKind.ObjectInitializerExpression or SyntaxKind.WithInitializerExpression;

    private static bool IsDeclaredInside(ISymbol symbol, SyntaxNode node) =>
        symbol.DeclaringSyntaxReferences.Any(r => r.SyntaxTree == node.SyntaxTree && node.Span.Contains(r.Span));

    private static bool IsLeftmost(SimpleNameSyntax name) => name.Parent switch
    {
        QualifiedNameSyntax qualified => qualified.Left == name,
        MemberAccessExpressionSyntax access => access.Expression == name,
        MemberBindingExpressionSyntax => false,
        AliasQualifiedNameSyntax => false,
        _ => true,
    };

    private static readonly SymbolDisplayFormat QualifiedFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// <c>Namespace.Outer.Name</c>, without type arguments - the caller keeps the ones written. Null
    /// when a containing type is generic, which would need its own arguments spelled out.
    /// </summary>
    private static string? QualifiedName(INamedTypeSymbol type)
    {
        var parts = new List<string> { type.Name };
        for (var containing = type.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            if (containing.IsGenericType)
            {
                return null;
            }

            parts.Insert(0, containing.Name);
        }

        if (type.ContainingNamespace is { IsGlobalNamespace: false } ns)
        {
            parts.Insert(0, ns.ToDisplayString());
        }

        return string.Join(".", parts);
    }

    /// <summary>The leading whitespace of the line the node starts on.</summary>
    private static int IndentationOf(SyntaxNode node)
    {
        var text = node.SyntaxTree.GetText();
        var line = text.Lines.GetLineFromPosition(node.SpanStart);
        var indentation = 0;
        for (var i = line.Start; i < line.End && text[i] is ' ' or '\t'; i++)
        {
            indentation++;
        }

        return indentation;
    }

    private static string Dedent(string text, int indentation)
    {
        if (indentation == 0 || !text.Contains('\n'))
        {
            return text;
        }

        var lines = text.Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            var strip = 0;
            while (strip < indentation && strip < lines[i].Length && lines[i][strip] is ' ' or '\t')
            {
                strip++;
            }

            lines[i] = lines[i][strip..];
        }

        return string.Join('\n', lines);
    }

    private static string Describe(SyntaxNode node)
    {
        var text = (node.ToString().Length > 0 ? node : node.Parent ?? node).ToString().ReplaceLineEndings(" ");
        return text.Length > 80 ? text[..77] + "..." : text;
    }
}
