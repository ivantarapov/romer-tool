using Romer.Core;

namespace Romer.Core.Tests;

public sealed class OverlayStateControllerTests
{
    [Fact]
    public void Toggles_UpdateExpectedFields()
    {
        var controller = new OverlayStateController("UTM_24K");

        Assert.False(controller.State.IsVisible);
        Assert.False(controller.State.IsClickThrough);
        Assert.False(controller.State.IsLocked);

        controller.ToggleVisibility();
        controller.ToggleClickThrough();
        controller.ToggleLock();
        controller.SetTemplate("UTM_50K");

        Assert.True(controller.State.IsVisible);
        Assert.True(controller.State.IsClickThrough);
        Assert.True(controller.State.IsLocked);
        Assert.Equal("UTM_50K", controller.State.ActiveTemplateId);
    }
}
