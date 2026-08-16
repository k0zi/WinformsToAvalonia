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
    public FormModel Build(DesignerWalkResult walkResult)
    {
        var formModel = walkResult.Form;
        var childFieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var edge in walkResult.Edges)
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
