using WinFormsToAvalonia.Core.Model;

namespace WinFormsToAvalonia.Core.Parsing;

/// <summary>
/// Reads a Form's paired non-designer .cs file verbatim, for ViewCodeBehindEmitter to
/// embed as a preserved-for-migration comment block.
/// </summary>
/// <remarks>
/// Simplification vs. the original per-member-signature extraction design: control/Form
/// event handler *names* are already known precisely from DesignerSyntaxWalker's
/// EventHandlerBinding capture (Phase 4), so ViewModelEmitter doesn't need this class to
/// re-derive them by parsing member signatures - it only needs the raw text for the
/// human-readable comment block, which is simpler and more faithful to embed whole.
/// </remarks>
public sealed class CodeBehindExtractor
{
    public RawCodeBehind? Extract(string? primaryFilePath)
    {
        if (primaryFilePath is null || !File.Exists(primaryFilePath))
        {
            return null;
        }

        return new RawCodeBehind(primaryFilePath, File.ReadAllText(primaryFilePath));
    }
}
