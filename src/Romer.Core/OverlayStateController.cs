namespace Romer.Core;

public sealed class OverlayStateController
{
    private OverlayState _state;

    public OverlayStateController(string defaultTemplateId)
    {
        _state = new OverlayState(false, false, false, defaultTemplateId);
    }

    public OverlayState State => _state;

    public OverlayState ToggleVisibility()
        => _state = _state with { IsVisible = !_state.IsVisible };

    public OverlayState ToggleClickThrough()
        => _state = _state with { IsClickThrough = !_state.IsClickThrough };

    public OverlayState ToggleLock()
        => _state = _state with { IsLocked = !_state.IsLocked };

    public OverlayState SetTemplate(string templateId)
        => _state = _state with { ActiveTemplateId = templateId };

    public OverlayState SetVisible(bool visible)
        => _state = _state with { IsVisible = visible };
}
