using WinFormsToAvalonia.Core.Scaffolding;
using Xunit;

namespace WinFormsToAvalonia.Core.Tests.Scaffolding;

public class FallbackControlResolverTests
{
    [Fact]
    public void CopyResolvedTemplates_KnownKeys_CopiesFilesWithRewrittenNamespace()
    {
        var vfs = new VirtualFileSystem();
        var resolver = new FallbackControlResolver();

        resolver.CopyResolvedTemplates(vfs, "DemoApp", new HashSet<string> { "PropertyGridFallback", "RichTextBoxFallback" });

        Assert.Contains("Controls/PropertyGridFallback.cs", vfs.RelativePaths);
        Assert.Contains("Controls/RichTextBoxFallback.cs", vfs.RelativePaths);

        vfs.TryGetText("Controls/PropertyGridFallback.cs", out var propertyGridSource);
        Assert.Contains("namespace DemoApp.Controls;", propertyGridSource);
        Assert.DoesNotContain("__TARGET_NAMESPACE__", propertyGridSource);
    }

    [Fact]
    public void CopyResolvedTemplates_UnknownKey_IsIgnoredRatherThanThrowing()
    {
        var vfs = new VirtualFileSystem();
        var resolver = new FallbackControlResolver();

        resolver.CopyResolvedTemplates(vfs, "DemoApp", new HashSet<string> { "SomeControlWithNoTemplate" });

        Assert.Empty(vfs.RelativePaths);
    }

    [Fact]
    public void CopyResolvedTemplates_EmptySet_CopiesNothing()
    {
        var vfs = new VirtualFileSystem();
        var resolver = new FallbackControlResolver();

        resolver.CopyResolvedTemplates(vfs, "DemoApp", new HashSet<string>());

        Assert.Empty(vfs.RelativePaths);
    }
}
