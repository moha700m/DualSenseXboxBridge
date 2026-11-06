using DualSenseXboxBridge.Core.DualSense;
using DualSenseXboxBridge.Core.Mapping;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class MappingTests
{
    private static DualSenseInputState State(
        DualSenseButtons buttons = DualSenseButtons.None,
        DualSenseDpadDirection dpad = DualSenseDpadDirection.Neutral,
        byte lx = 128, byte ly = 128, byte rx = 128, byte ry = 128,
        byte lt = 0, byte rt = 0) =>
        new(buttons, dpad, lx, ly, rx, ry, lt, rt, 0, 5, false);

    [Fact]
    public void NeutralInput_MapsToNeutralPad()
    {
        var pad = DualSenseToXboxMapper.Map(State(), new MappingOptions());

        Assert.True(pad.IsNeutral);
    }

    [Theory]
    [InlineData(DualSenseButtons.Cross, Xbox360Buttons.A)]
    [InlineData(DualSenseButtons.Circle, Xbox360Buttons.B)]
    [InlineData(DualSenseButtons.Square, Xbox360Buttons.X)]
    [InlineData(DualSenseButtons.Triangle, Xbox360Buttons.Y)]
    [InlineData(DualSenseButtons.L1, Xbox360Buttons.LeftShoulder)]
    [InlineData(DualSenseButtons.R1, Xbox360Buttons.RightShoulder)]
    [InlineData(DualSenseButtons.Create, Xbox360Buttons.Back)]
    [InlineData(DualSenseButtons.Options, Xbox360Buttons.Start)]
    [InlineData(DualSenseButtons.L3, Xbox360Buttons.LeftThumb)]
    [InlineData(DualSenseButtons.R3, Xbox360Buttons.RightThumb)]
    [InlineData(DualSenseButtons.Ps, Xbox360Buttons.Guide)]
    public void DefaultButtonMap_MatchesXboxLayout(DualSenseButtons source, Xbox360Buttons expected)
    {
        var pad = DualSenseToXboxMapper.Map(State(buttons: source));
        Assert.Equal(expected, pad.Buttons);
    }

    [Theory]
    [InlineData(DualSenseDpadDirection.Up, Xbox360Buttons.DpadUp)]
    [InlineData(DualSenseDpadDirection.Down, Xbox360Buttons.DpadDown)]
    [InlineData(DualSenseDpadDirection.Left, Xbox360Buttons.DpadLeft)]
    [InlineData(DualSenseDpadDirection.Right, Xbox360Buttons.DpadRight)]
    [InlineData(DualSenseDpadDirection.UpLeft, Xbox360Buttons.DpadUp | Xbox360Buttons.DpadLeft)]
    [InlineData(DualSenseDpadDirection.DownRight, Xbox360Buttons.DpadDown | Xbox360Buttons.DpadRight)]
    [InlineData(DualSenseDpadDirection.Neutral, Xbox360Buttons.None)]
    public void Dpad_MapsToXboxButtons(DualSenseDpadDirection direction, Xbox360Buttons expected)
    {
        var pad = DualSenseToXboxMapper.Map(State(dpad: direction));
        Assert.Equal(expected, pad.Buttons & (Xbox360Buttons.DpadUp | Xbox360Buttons.DpadDown | Xbox360Buttons.DpadLeft | Xbox360Buttons.DpadRight));
    }

    [Fact]
    public void StickY_IsInvertedForXInput()
    {
        // 0 means "up" on a DualSense, but +32767 means "up" on XInput.
        var pad = DualSenseToXboxMapper.Map(State(ly: 0), new MappingOptions { StickDeadzone = 0 });

        Assert.True(pad.LeftStickY > 32000);
    }

    [Fact]
    public void StickX_FullRight_ReachesMaximum()
    {
        var pad = DualSenseToXboxMapper.Map(State(lx: 255), new MappingOptions { StickDeadzone = 0 });

        Assert.Equal(32767, pad.LeftStickX);
    }

    [Fact]
    public void Deadzone_SuppressesSmallMovement()
    {
        var pad = DualSenseToXboxMapper.Map(State(lx: 140), new MappingOptions { StickDeadzone = 0.2 });

        Assert.Equal(0, pad.LeftStickX);
        Assert.Equal(0, pad.LeftStickY);
    }

    [Fact]
    public void Deadzone_RescalesRemainingTravel()
    {
        var pad = DualSenseToXboxMapper.Map(State(lx: 255), new MappingOptions { StickDeadzone = 0.5 });

        Assert.Equal(32767, pad.LeftStickX);
    }

    [Fact]
    public void TriggerDeadzone_ZeroesLightPress()
    {
        var pad = DualSenseToXboxMapper.Map(State(lt: 3), new MappingOptions { TriggerDeadzone = 0.1 });

        Assert.Equal(0, pad.LeftTrigger);
    }

    [Fact]
    public void TriggerFullPress_Reaches255()
    {
        var pad = DualSenseToXboxMapper.Map(State(rt: 255), new MappingOptions { TriggerDeadzone = 0.1 });

        Assert.Equal(255, pad.RightTrigger);
    }

    [Fact]
    public void ButtonOverride_ReplacesDefault()
    {
        var options = new MappingOptions();
        options.ButtonOverrides["Cross"] = "X";

        var pad = DualSenseToXboxMapper.Map(State(buttons: DualSenseButtons.Cross), options);

        Assert.Equal(Xbox360Buttons.X, pad.Buttons);
    }

    [Fact]
    public void ButtonOverride_CanDisableButton()
    {
        var options = new MappingOptions();
        options.ButtonOverrides["Ps"] = "None";

        var pad = DualSenseToXboxMapper.Map(State(buttons: DualSenseButtons.Ps), options);

        Assert.Equal(Xbox360Buttons.None, pad.Buttons);
    }

    [Fact]
    public void InvalidButtonOverride_IsIgnored()
    {
        var options = new MappingOptions();
        options.ButtonOverrides["Cross"] = "NotAButton";

        var pad = DualSenseToXboxMapper.Map(State(buttons: DualSenseButtons.Cross), options);

        Assert.Equal(Xbox360Buttons.A, pad.Buttons);
    }

    [Fact]
    public void SwapSticks_SwapsAxes()
    {
        var pad = DualSenseToXboxMapper.Map(
            State(lx: 255, rx: 0),
            new MappingOptions { StickDeadzone = 0, SwapSticks = true });

        Assert.True(pad.LeftStickX < -32000);
        Assert.Equal(32767, pad.RightStickX);
    }

    [Fact]
    public void Calibration_ShiftsCentre()
    {
        var calibration = AxisCalibration.WithCenter(160);
        var axis = calibration.ToAxis(160, invert: false);

        Assert.Equal(0, axis);
    }

    [Fact]
    public void NormalizeOptions_ClampsOutOfRangeValues()
    {
        var options = new MappingOptions
        {
            StickDeadzone = 4,
            TriggerDeadzone = -2,
            StickSensitivity = 99
        };

        options.Normalize();

        Assert.Equal(0.9, options.StickDeadzone);
        Assert.Equal(0.0, options.TriggerDeadzone);
        Assert.Equal(3.0, options.StickSensitivity);
    }
}
