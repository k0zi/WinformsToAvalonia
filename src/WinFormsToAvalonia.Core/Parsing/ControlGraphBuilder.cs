using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Assembles a DesignerSyntaxWalker's flat facts (controls + Controls.Add/AddRange edges)
/// into an actual parent/child tree: <see cref="FormModel.RootControls"/> (added directly
/// to the Form/UserControl) and nested <see cref="ControlModel.Children"/> (added under
/// another control), vs. <see cref="FormModel.Components"/> - controls never targeted by
/// any Controls.Add, which is the structural signature of a non-visual designer component
/// (Timer, ImageList, ToolTip, ErrorProvider, BindingSource, ...).
/// </summary>
public sealed class ControlGraphBuilder
{
    public FormModel Build(DesignerWalkResult walkResult) => Build(walkResult, []);

    /// <param name="warnings">
    /// Collapsing a host away can drop things the designer set on it, and dropping them silently
    /// is the failure this converter exists to avoid - so the caller has to be given somewhere to
    /// put them.
    /// </param>
    public FormModel Build(DesignerWalkResult walkResult, List<string> warnings)
    {
        var formModel = walkResult.Form;
        var childFieldNames = new HashSet<string>(StringComparer.Ordinal);
        var edges = CollapseHostedControls(walkResult, warnings);

        foreach (var edge in edges)
        {
            if (!formModel.Controls.TryGetValue(edge.ChildFieldName, out var child))
            {
                continue;
            }

            childFieldNames.Add(edge.ChildFieldName);

            if (edge.ParentFieldName == ParentChildEdge.FormOwner)
            {
                formModel.RootControls.Add(child);
            }
            else if (TrySplitPanelSlot(edge.ParentFieldName, out var containerField, out var panelName)
                && formModel.Controls.TryGetValue(containerField, out var container))
            {
                var slot = panelName == "Panel1" ? container.Panel1Children : container.Panel2Children;
                slot.Add(child);
            }
            else if (formModel.Controls.TryGetValue(edge.ParentFieldName, out var parent))
            {
                parent.Children.Add(child);
            }
        }

        foreach (var control in formModel.Controls.Values)
        {
            if (!childFieldNames.Contains(control.FieldName))
            {
                formModel.Components.Add(control);
            }
        }

        return formModel;
    }

    /// <summary>
    /// Replaces every host in the edge list with the control it was built around, and removes the
    /// host itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>ToolStripControlHost</c> is plumbing: WinForms needs it because a <c>ToolStrip</c>
    /// only takes <c>ToolStripItem</c>s, and Avalonia does not, because the fallback a ToolStrip
    /// maps to is an ordinary panel. So the faithful conversion is to put the hosted control where
    /// the host was - which is a tree-assembly job, and belongs here beside the other synthetic-id
    /// decoding rather than in the walker: the host keeps taking property assignments until the
    /// last statement, so it cannot be collapsed while the walk is still running.
    /// </para>
    /// <para>
    /// Refuses rather than guesses in two cases, both of which would break the generated project:
    /// a hosted control that is also added somewhere else would be emitted twice, and two
    /// elements with one <c>x:Name</c> is an AVLN1001 the generated build fails on.
    /// </para>
    /// </remarks>
    private static List<ParentChildEdge> CollapseHostedControls(DesignerWalkResult walkResult, List<string> warnings)
    {
        var edges = walkResult.Edges.ToList();
        if (walkResult.HostedControlAliases.Count == 0)
        {
            return edges;
        }

        var formModel = walkResult.Form;
        var placed = walkResult.Edges.Select(e => e.ChildFieldName).ToHashSet(StringComparer.Ordinal);

        foreach (var (hostField, hostedField) in walkResult.HostedControlAliases.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            if (!formModel.Controls.TryGetValue(hostField, out var host))
            {
                continue;
            }

            if (!formModel.Controls.TryGetValue(hostedField, out var hosted))
            {
                warnings.Add(
                    $"'{hostField}' ({host.ClrTypeName}) hosts '{hostedField}', which the designer never " +
                    "creates - neither is emitted.");
                continue;
            }

            if (placed.Contains(hostedField))
            {
                warnings.Add(
                    $"'{hostField}' ({host.ClrTypeName}) hosts '{hostedField}', but '{hostedField}' is also " +
                    "added to a container of its own - it is left where it was added, and the host is not " +
                    "emitted, since one control cannot appear in two places.");
                continue;
            }

            CarryHostState(host, hosted, warnings);

            for (var i = 0; i < edges.Count; i++)
            {
                if (edges[i].ChildFieldName == hostField)
                {
                    edges[i] = edges[i] with { ChildFieldName = hostedField };
                }

                if (edges[i].ParentFieldName == hostField)
                {
                    edges[i] = edges[i] with { ParentFieldName = hostedField };
                }
            }

            placed.Add(hostedField);
            formModel.Controls.Remove(hostField);
        }

        return edges;
    }

    /// <summary>
    /// What the designer set on the host, now that the host is going away.
    /// </summary>
    /// <remarks>
    /// Only <c>Size</c> moves, and only into a gap: WinForms keeps a host's <c>Size</c> and its
    /// hosted control's in sync, so the two are provably the same number, and the control's own
    /// statement is the more specific one. <c>Name</c> is the host field's own identity and goes
    /// with it. Everything else is ToolStrip *item* semantics - <c>Alignment</c>,
    /// <c>Overflow</c>, <c>DisplayStyle</c> - which mean nothing on a plain child of a panel, and
    /// every event subscribed on the host would otherwise be dropped by the planner without a
    /// word, since it iterates the controls this method is about to delete one of.
    /// </remarks>
    private static void CarryHostState(ControlModel host, ControlModel hosted, List<string> warnings)
    {
        foreach (var (propertyName, value) in host.Properties)
        {
            if (propertyName == "Name")
            {
                continue;
            }

            if (propertyName == "Size" && !hosted.Properties.ContainsKey("Size"))
            {
                hosted.Properties["Size"] = value;
                continue;
            }

            if (propertyName == "Size")
            {
                continue;
            }

            warnings.Add(
                $"'{host.FieldName}' ({host.ClrTypeName}) sets '{propertyName}', which is a ToolStrip item " +
                $"setting with no counterpart on '{hosted.FieldName}' now that it is placed directly - not carried over.");
        }

        foreach (var subscription in host.Events)
        {
            warnings.Add(
                $"'{host.FieldName}' ({host.ClrTypeName}) subscribes '{subscription.EventName}'. The host is not " +
                $"emitted, and re-pointing it at '{hosted.FieldName}' would change which Avalonia event and " +
                "argument type the handler gets - subscribe it by hand.");
        }
    }

    /// <summary>
    /// Splits DesignerSyntaxWalker's synthetic "field.Panel1"/"field.Panel2" parent id (see
    /// its HandleInvocation) back into the owning SplitContainer's field name and the panel
    /// name. Real WinForms field names never contain '.', so this is unambiguous.
    /// </summary>
    private static bool TrySplitPanelSlot(string parentFieldName, out string containerField, out string panelName)
    {
        var dotIndex = parentFieldName.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex > 0)
        {
            containerField = parentFieldName[..dotIndex];
            panelName = parentFieldName[(dotIndex + 1)..];
            return true;
        }

        containerField = "";
        panelName = "";
        return false;
    }
}
