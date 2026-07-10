using Converter.Generator.Mapping;
using Converter.Plugin.Abstractions;

namespace Converter.Tests.Generator;

public class MappingResolverTests
{
    private sealed class FakeControlMapper(int priority, Func<ControlNode, bool> canMap, string avaloniaControlType) : IControlMapper
    {
        public int Priority => priority;
        public bool CanMap(ControlNode winFormsControl) => canMap(winFormsControl);

        public Task<ControlMappingResult> MapAsync(ControlNode winFormsControl, MappingContext context) =>
            Task.FromResult(new ControlMappingResult { AvaloniaControlType = avaloniaControlType });
    }

    private static MappingContext Context() => new() { ProjectPath = "unused", OutputPath = "unused" };

    private static ControlNode NewControl(string controlType, string name) =>
        new() { ControlType = controlType, FullTypeName = controlType, Name = name };

    [Fact]
    public async Task ResolveForFormAsync_NoPlugins_ReturnsEmptyWithoutWalkingTree()
    {
        var root = NewControl("Form", "root");
        root.Children.Add(NewControl("Button", "button1"));

        var overrides = await MappingResolver.Empty.ResolveForFormAsync(root, Context());

        Assert.Same(PluginMappingOverrides.Empty, overrides);
        Assert.Empty(overrides.ControlMappings);
    }

    [Fact]
    public async Task ResolveForFormAsync_HigherPriorityMapperWins()
    {
        var lowPriority = new FakeControlMapper(0, c => c.ControlType == "Gauge", "Low");
        var highPriority = new FakeControlMapper(10, c => c.ControlType == "Gauge", "High");

        var resolver = new MappingResolver([lowPriority, highPriority], [], []);

        var root = NewControl("Form", "root");
        var gauge = NewControl("Gauge", "gauge1");
        root.Children.Add(gauge);

        var overrides = await resolver.ResolveForFormAsync(root, Context());

        Assert.True(overrides.ControlMappings.TryGetValue(gauge, out var result));
        Assert.Equal("High", result!.AvaloniaControlType);
    }

    [Fact]
    public async Task ResolveForFormAsync_UnclaimedControl_HasNoEntry_FallsBackToStaticRegistry()
    {
        var mapper = new FakeControlMapper(0, c => c.ControlType == "Gauge", "Mapped");
        var resolver = new MappingResolver([mapper], [], []);

        var root = NewControl("Form", "root");
        var button = NewControl("Button", "button1");
        root.Children.Add(button);

        var overrides = await resolver.ResolveForFormAsync(root, Context());

        Assert.False(overrides.ControlMappings.ContainsKey(button));
    }

    [Fact]
    public async Task ResolveForFormAsync_WalksWholeTree_IncludingRoot()
    {
        var mapper = new FakeControlMapper(0, c => true, "Everything");
        var resolver = new MappingResolver([mapper], [], []);

        var root = NewControl("Form", "root");
        var child = NewControl("Button", "button1");
        var grandchild = NewControl("Label", "label1");
        child.Children.Add(grandchild);
        root.Children.Add(child);

        var overrides = await resolver.ResolveForFormAsync(root, Context());

        Assert.Equal(3, overrides.ControlMappings.Count);
        Assert.True(overrides.ControlMappings.ContainsKey(root));
        Assert.True(overrides.ControlMappings.ContainsKey(child));
        Assert.True(overrides.ControlMappings.ContainsKey(grandchild));
    }

    [Fact]
    public async Task ResolveForFormAsync_ConcurrentCallsOverIndependentTrees_DoNotCrossContaminate()
    {
        var mapper = new FakeControlMapper(0, c => c.ControlType == "Gauge", "Mapped");
        var resolver = new MappingResolver([mapper], [], []);

        var trees = Enumerable.Range(0, 20).Select(i =>
        {
            var root = NewControl("Form", $"root{i}");
            var gauge = NewControl("Gauge", $"gauge{i}");
            root.Children.Add(gauge);
            return (Root: root, Gauge: gauge);
        }).ToList();

        var results = await Task.WhenAll(trees.Select(t => resolver.ResolveForFormAsync(t.Root, Context())));

        for (var i = 0; i < trees.Count; i++)
        {
            var overrides = results[i];
            Assert.Single(overrides.ControlMappings);
            Assert.True(overrides.ControlMappings.ContainsKey(trees[i].Gauge));
            Assert.False(overrides.ControlMappings.ContainsKey(trees[i].Root));
        }
    }
}
