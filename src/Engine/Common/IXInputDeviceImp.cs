
namespace Fusee.Engine
{
    public interface IXInputDeviceImp
    {
        /// <summary>
        /// This one updates the device state and pulls data from the device.
        /// </summary>
        /// <returns></returns>
        bool UpdateState();

        /// <summary>
        /// Implement this to get the left thumbstick's X-Axis.
        /// </summary>
        /// <returns>The left thumbstick's X-Axis value.</returns>
        int GetThumbLXAxis();

        /// <summary>
        /// Implement this to get the left thumbstick's Y-Axis.
        /// </summary>
        /// <returns>The left thumbstick's Y-Axis value.</returns>
        int GetThumbLYAxis();

        /// <summary>
        /// Implement this to get the right thumbstick's X-Axis.
        /// </summary>
        /// <returns>The right thumbstick's X-Axis value.</returns>
        int GetThumbRXAxis();

        /// <summary>
        /// Implement this to get the right thumbstick's Y-Axis.
        /// </summary>
        /// <returns>The right thumbstick's Y-Axis value.</returns>
        int GetThumbRYAxis();

        /// <summary>
        /// Implement this to get the Left Z-Axis.
        /// </summary>
        /// <returns>The Z-Axis value.</returns>
        int GetZAxisLeft();

        /// <summary>
        /// Implement this to get the Right Z-Axis.
        /// </summary>
        /// <returns>The Z-Axis value.</returns>
        int GetZAxisRight();

        /// <summary>
        /// Implement this to get the Device Name.
        /// </summary>
        /// <returns>The Device Name.</returns>
        string GetName();

        /// <summary>
        /// Implement this to get the pressed button.
        /// </summary>
        /// <returns>The Index of the pressed button.</returns>
        int GetPressedButton();

        /// <summary>
        /// Implement this to check if button is down.
        /// </summary>
        /// <returns>True, if button is down</returns>
        bool IsButtonDown(int button);

        /// <summary>
        /// Implement this to check if button has been pressed.
        /// </summary>
        /// <returns>True, if button has been pressed.</returns>
        bool IsButtonPressed(int button);

        /// <summary>
        /// Implement this to get the amount of buttons.
        /// </summary>
        /// <returns>The amount of buttons.</returns>
        int GetButtonCount();

        /// <summary>
        /// Implement this to get the level of the controller battery.
        /// </summary>
        /// <returns>The level of the battery. 0 Should be empty, higher should represent XInput levels.</returns>
        int BatteryLevel();

        /// <summary>
        /// Implement this to set the deadzone.
        /// </summary>
        /// <returns>The amount of the deadzone</returns>
        void SetDeadZone(float dzLeftThumb, float dzRightThumb);

        /// <summary>
        /// Implement this to set the rumble motor.
        /// </summary>
        /// <returns>The amount of rumble</returns>
        void SetRumble(ushort rumbleLeft, ushort rumbleRight);

        /// <summary>
        /// Implement this to get the device category name.
        /// </summary>
        /// <returns>The name of the device categroy.</returns>
        string GetCategory();

    }
}
