using System.Windows;
using Romer.Core;

namespace Romer.UI.Services;

public sealed class TemplateRegistry : ITemplateProvider
{
    private readonly List<RomerTemplate> _templates;

    public TemplateRegistry()
    {
        _templates =
        [
            BuildTemplate("UTM_24K", "UTM/MGRS 1:24k", "1:24,000"),
            BuildTemplate("UTM_50K", "UTM/MGRS 1:50k", "1:50,000")
        ];
    }

    public IReadOnlyList<RomerTemplate> GetTemplates() => _templates;

    public RomerTemplate GetById(string id)
    {
        var found = _templates.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (found is null)
        {
            throw new InvalidOperationException($"Unknown template '{id}'.");
        }

        return found;
    }

    private static RomerTemplate BuildTemplate(string id, string displayName, string scale)
    {
        const double size = 240;
        const int divisions = 10;
        var lines = new List<RomerLine>
        {
            new(new Point(0, 0), new Point(size, 0), 1.8),
            new(new Point(size, 0), new Point(size, size), 1.8),
            new(new Point(size, size), new Point(0, size), 1.8),
            new(new Point(0, size), new Point(0, 0), 1.8)
        };

        var step = size / divisions;
        for (var i = 1; i < divisions; i++)
        {
            var p = step * i;
            lines.Add(new RomerLine(new Point(p, 0), new Point(p, size), i % 5 == 0 ? 1.2 : 0.8));
            lines.Add(new RomerLine(new Point(0, p), new Point(size, p), i % 5 == 0 ? 1.2 : 0.8));
        }

        return new RomerTemplate(id, displayName, scale, lines);
    }
}
