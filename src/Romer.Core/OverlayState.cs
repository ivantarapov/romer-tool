namespace Romer.Core;

public sealed record OverlayState(
    bool IsVisible,
    bool IsClickThrough,
    bool IsLocked,
    string ActiveTemplateId);
