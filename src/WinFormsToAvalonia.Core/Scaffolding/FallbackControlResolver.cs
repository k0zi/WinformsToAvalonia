using WinFormsToAvalonia.FallbackControls;

namespace WinFormsToAvalonia.Core.Scaffolding;

/// <summary>
/// Copies the fallback control templates a conversion actually used - plus any other
/// template each one depends on (see <see cref="FallbackTemplateDefinition.DependsOnKeys"/>,
/// e.g. ToolStripContainerFallback composes ToolStripPanelFallback/
/// ToolStripContentPanelFallback internally even when no WinForms control was itself mapped
/// to those keys) - into the generated project's Controls/ folder, rewriting each template's
/// placeholder namespace to the target project's real namespace.
/// </summary>
public sealed class FallbackControlResolver
{
    private const string TemplateNamespaceToken = "__TARGET_NAMESPACE__";

    public void CopyResolvedTemplates(VirtualFileSystem vfs, string projectName, IReadOnlySet<string> usedFallbackKeys)
    {
        var copiedKeys = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>(usedFallbackKeys);

        while (pending.Count > 0)
        {
            var key = pending.Dequeue();
            if (!copiedKeys.Add(key))
            {
                continue;
            }

            if (!FallbackControlCatalog.All.TryGetValue(key, out var definition))
            {
                continue;
            }

            var source = FallbackControlCatalog.ReadTemplateSource(definition.ResourceLogicalName);
            var rewritten = source.Replace(TemplateNamespaceToken, $"{projectName}.Controls", StringComparison.Ordinal);
            vfs.AddText($"Controls/{definition.OutputFileName}", rewritten);

            foreach (var dependencyKey in definition.DependsOnKeys)
            {
                pending.Enqueue(dependencyKey);
            }
        }
    }
}
