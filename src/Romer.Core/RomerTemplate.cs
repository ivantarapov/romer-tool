namespace Romer.Core;

public sealed record RomerTemplate(
    string Id,
    string DisplayName,
    string NominalScale,
    IReadOnlyList<RomerLine> GeometryDefinition);
