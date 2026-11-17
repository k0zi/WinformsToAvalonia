namespace WinFormsToAvalonia.Core.Model;

/// <summary>
/// A NuGet package the original project depends on - a <c>&lt;PackageReference&gt;</c>, or a
/// <c>packages.config</c> entry of a legacy project.
/// </summary>
/// <param name="Version">As written; may be a range or a floating version, which is never resolved.</param>
/// <param name="IsDevelopmentOnly">
/// <c>PrivateAssets="all"</c> (or a packages.config <c>developmentDependency</c>): an analyzer or a
/// build tool, nothing the code calls.
/// </param>
public sealed record NuGetPackageReference(string Id, string Version, bool IsDevelopmentOnly = false);
