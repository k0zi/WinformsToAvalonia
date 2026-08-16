using WinFormsToAvalonia.Core.Model;
using WinFormsToAvalonia.Core.Scaffolding;

namespace WinFormsToAvalonia.Core.Pipeline;

/// <summary>The full result of a conversion run: the staged output plus the report describing it.</summary>
public sealed record ConversionRunResult(VirtualFileSystem Vfs, ConversionReport Report);
