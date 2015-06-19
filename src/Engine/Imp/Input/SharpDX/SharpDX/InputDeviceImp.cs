using System;
using SharpDX.XInput;

namespace Fusee.Engine
{

    /// <summary>
    /// The SlimDX - specific implementation for the input devices.
    /// </summary>
    public class InputDeviceImp : IInputDeviceImp
    {
        private readonly Controller _controller;
        
        // Settings
        private float _deadZone = 0.1f;
        private float _vibration = 0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDeviceImp"/> class.
        /// </summary>
        /// <param name="instance">The DeviceInstance.</param>
        public InputDeviceImp(Controller device)
        {
            _controller = device;
            _deadZone = 0.1f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDeviceImp"/> class.
        /// </summary>
        public InputDeviceImp()
        {

        }

        /// <summary>
        /// Gets the current state of the input device. The state is used to poll the device.
        /// </summary>
        /// <returns>The state of the input device.</returns>
        public State GetState()
        {
            return _controller.GetState();
        }

        /// <summary>
        /// Loop though all buttons on the gamepad an see which one is pressed
        /// </summary>
        /// <returns>The pressed button</returns>
        public int GetPressedButton()
        {
            return 0;
        }

        /// <summary>
        /// Checks if the button is down.
        /// </summary>
        /// <param name="buttonIndex">The button to check.</param>
        /// <returns>True if the button is pressed and false if not.</returns>
        public bool IsButtonDown(int buttonIndex)
        {
            return false;
        }

        /// <summary>
        /// Checks if a specified button is held down for more than one frame.
        /// </summary>
        /// <param name="buttonIndex">The index of the button that is checked.</param>
        /// <returns>true if the button at the specified index is held down for more than one frame and false if not.</returns>
        public bool IsButtonPressed(int buttonIndex)
        {
            return false;
        }

        /// <summary>
        /// Counts the buttons on the input device.
        /// </summary>
        /// <returns>The amount of buttons on the device.</returns>
        public int GetButtonCount()
        {
            return 0;
        }

        /// <summary>
        /// Gets the value of the z-axis. On most gamepads the z-axis is either the right joystick or the triggers at the back.
        /// For wobbly joysticks a dead zone is required to avoid value changes if the joystick is not moved.
        /// </summary>
        /// <returns>The current value of the z-axis.</returns>
        public float GetZAxis()
        {
            return 0;
        }

        /// <summary>
        /// Gets the value of the y-axis. On most gamepads the y-axis is the horizontal axis of the left joystick.
        /// For wobbly joysticks a dead zone is required to avoid value changes if the joystick is not moved.
        /// </summary>
        /// <returns>The current value of the y-axis.</returns>
        public float GetYAxis()
        {
            return 0;
        }

        /// <summary>
        /// Gets the value of the x-axis. On most gamepads the x-axis the horizontal axis of the left joystick.
        /// For wobbly joysticks a dead zone is required to avoid value changes if the joystick is not moved.
        /// </summary>
        /// <returns>The current value of the x-axis.</returns>
        public float GetXAxis()
        {

            return 0;
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <returns>The category of the device</returns>
        public String GetCategory()
        {
            return _controller.GetCapabilities(DeviceQueryType.Any).ToString();
        }

        /// <summary>
        /// Gets a user-friendly product name of the device.
        /// </summary>
        /// <returns>The device name.</returns>
        public string GetName()
        {
            return _controller.GetCapabilities(DeviceQueryType.Any).ToString();
        }

        /// <summary>
        /// Sets the Deadzone
        /// </summary>
        /// <param name="zone">The Deadzone.</param>
        public void SetDeadZone(float zone)
        {
            _deadZone = zone;
        }

        /// <summary>
        /// Sets the rumble motors vibration.
        /// </summary>
        /// <param name="zone">The rumble vibration.</param>
        public void SetVibration(float vib)
        {
            _vibration = vib;
        }


    }
}

