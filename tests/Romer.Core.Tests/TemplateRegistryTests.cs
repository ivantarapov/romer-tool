using Romer.UI.Services;

namespace Romer.Core.Tests;

public sealed class TemplateRegistryTests
{
    [Fact]
    public void ProvidesBothExpectedTemplates()
    {
        var registry = new TemplateRegistry();
        var templates = registry.GetTemplates();

        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, t => t.Id == "UTM_24K");
        Assert.Contains(templates, t => t.Id == "UTM_50K");
        Assert.All(templates, t => Assert.NotEmpty(t.GeometryDefinition));
    }
}
