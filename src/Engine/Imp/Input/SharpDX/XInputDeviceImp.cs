using System;
using SharpDX.XInput;
using System.Collections.Generic;

namespace Fusee.Engine
{

    /// <summary>
    /// The SlimDX - specific implementation for the input devices.
    /// </summary>
    public class XInputDeviceImp : IXInputDeviceImp
    {
        #region Fields

        private List<Controller> _Devices = new List<Controller>(); // A list of all connected xinput devices.
        private Controller _Controller; // The currently active controller device.
        
        // Settings
        private float _deadZoneL = 0f;
        private float _deadZoneR = 0f;
        private int _lastpackageNumber;
        private bool _newUpdate = false;
        private State _lastState;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDeviceImp"/> class.
        /// </summary>
        /// <param name="instance">The DeviceInstance.</param>
        public XInputDeviceImp()
        {
            //_Devices = new List<Controller>();

            for(int idx = 0; idx < (int)UserIndex.Four; idx++)
            {
                Controller cTmp = new Controller((UserIndex)idx);
                if (cTmp.IsConnected)
                {
                    // We can add the controller to the devices list now.
                    _Devices.Add(cTmp);
                }
            }

            //_Controller = _Devices[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDeviceImp"/> class.
        /// This constructor is called with an instance of a controller.
        /// </summary>
        /// <param name="instance">The DeviceInstance.</param>
        public XInputDeviceImp(Controller contr)
        {
            //_Devices = new List<Controller>();

            if (contr.IsConnected)
            {
                // We can add the controller to the devices list now.
                _Devices.Add(contr);
                if(_Devices[0] != null)
                {
                    _Controller = _Devices[0];
                }
                
            }            
        }

        /// <summary>
        /// Reinitializes all the connected gamepads.
        /// </summary>
        public void ReinitializeGamepads()
        {
            for (int idx = 0; idx < (int)UserIndex.Four; idx++)
            {
                Controller cTmp = new Controller((UserIndex)idx);
                if (cTmp.IsConnected)
                {
                    // We can add the controller to the devices list now.
                    _Devices.Add(cTmp);
                }
            }

            _Controller = _Devices[0];
        }

        /// <summary>
        /// This updates the packagenumber whenever a new information poll is received.
        /// </summary>
        private void UpdatePacketNumber()
        {
            int pnr = _Controller.GetState().PacketNumber;
            if(pnr == _lastpackageNumber)
            {
                _newUpdate = false;
                return;
            }
            _lastpackageNumber = pnr;
            _newUpdate = true;            
        }

        /// <summary>
        /// This controlls the update polling for the gamepad.
        /// This should be called at least every frame (more often for less input lag). Probably in the OnRenderFrame() function.
        /// Caution: More calls are putting more stress on the gamepad connection and the cpu.
        /// </summary>
        public void UpdateState()
        {
            UpdatePacketNumber();
            if(_newUpdate)
            {
                _lastState = _Controller.GetState();
            }            
        }

        #region Buttons

        /// <summary>
        /// Counts the buttons on the input device.
        /// </summary>
        /// <returns>The amount of buttons on the device.</returns>
        public int GetButtonCount()
        {
            return 0; // TODO
        }

        /// <summary>
        /// Loop overt all buttons on the gamepad an see which one is pressed
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

        #endregion Buttons

        #region Axis

        /// <summary>
        /// Returns the value of the left thumb sticks X-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbLXAxis()
        {
            return _lastState.Gamepad.LeftThumbX;
        }

        /// <summary>
        /// Returns the value of the left thumb sticks Y-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbLYAxis()
        {
            return _lastState.Gamepad.LeftThumbY;
        }

        /// <summary>
        /// Returns the value of the right thumb sticks X-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbRXAxis()
        {
            return _lastState.Gamepad.RightThumbX;
        }

        /// <summary>
        /// Returns the value of the right thumb sticks Y-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbRYAxis()
        {
            return _lastState.Gamepad.RightThumbY;
        }

        /// <summary>
        /// Returns the value of the left trigger Z-axis.
        /// </summary>
        /// <returns></returns>
        public int GetZAxisLeft()
        {
            return _lastState.Gamepad.LeftTrigger;
        }

        /// <summary>
        /// Returns the value of the right trigger Z-axis.
        /// </summary>
        /// <returns></returns>
        public int GetZAxisRight()
        {
            return _lastState.Gamepad.RightTrigger;
        }

        #endregion Axis

        #region SetParameters

        /// <summary>
        /// Sets the current gamepap depending on the index given.
        /// </summary>
        /// <param name="index">The index of the desired controller from the active devices list.</param>
        /// <returns>Returns bool if successful</returns>
        public bool SetCurrentController(int index)
        {
            if (index <= _Devices.Count)
            {
                _Controller = _Devices[index];
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Sets the Deadzone to the gamepad.
        /// </summary>
        /// <param name="dL">The Deadzone for the left stick.</param>
        /// /// <param name="dL">The Deadzone for the right stick</param>
        public void SetDeadZone(float dzLeftThumb, float dzRightThumb)
        {
            _deadZoneL = dzLeftThumb;
            _deadZoneR = dzRightThumb;
        }

        /// <summary>
        /// Sets the rumble motors vibration.
        /// </summary>
        /// <param name="vibL">The rumble vibration for the left motor.</param>
        /// /// <param name="vibR">The rumble vibration for the right motor.</param>
        public void SetRumble(ushort rumbleLeft, ushort rumbleRight)
        {
            _Controller.SetVibration(
                new Vibration()
                {
                    LeftMotorSpeed = rumbleLeft,
                    RightMotorSpeed = rumbleRight
                }
                );
        }
        #endregion SetParameters

        #region RetrieveInformation

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <returns>The category of the device</returns>
        public String GetCategory()
        {
            return "XInput";
        }

        /// <summary>
        /// Gets a user-friendly product name of the device.
        /// </summary>
        /// <returns>The device name.</returns>
        public string GetName()
        {
            return "XInputGamepad";
        }

        /// <summary>
        /// Returns the battery level of the current controller.
        /// </summary>
        /// <returns>Integer for the battery level. 0 = empty, 1 = low, 2 = medium, 3 = full</returns>
        public int BatteryLevel()
        {

            return (int) _Controller.GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryLevel;

            //BatteryLevel level = _Controller.GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryLevel;
            //if(level == SharpDX.XInput.BatteryLevel.Empty)
            //{
            //    return 0;
            //}
            //else if (level == SharpDX.XInput.BatteryLevel.Low)
            //{
            //    return 1;
            //}
            //else if (level == SharpDX.XInput.BatteryLevel.Medium)
            //{
            //    return 2;
            //}
            //else if (level == SharpDX.XInput.BatteryLevel.Full)
            //{
            //    return 3;
            //}
            //return 0;
        }

        /// <summary>
        /// Gets the current state of the input device. The state is used to poll the device.
        /// </summary>
        /// <returns>The state of the input device.</returns>
        public State GetState(UserIndex userIndex)
        {
            return _Controller.GetState();
        }

        #endregion RetrieveInformation        
    }
}

