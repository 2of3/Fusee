﻿using System;
using SharpDX.XInput;
using System.Collections.Generic;
using System.Linq;

namespace Fusee.Engine
{
    #region Enums
    [Flags]
    public enum FuseeXInputButtons : short
    {        
        Y = short.MinValue,
        None = 0,
        DPadUp = 1,
        DPadDown = 2,
        DPadLeft = 4,
        DPadRight = 8,
        Start = 16,
        Back = 32,
        LeftThumb = 64,
        RightThumb = 128,
        LeftShoulder = 256,
        RightShoulder = 512,
        A = 4096,
        B = 8192,
        X = 16384
    }

    public enum GamepadBatteryLevel : byte
    {
        Empty = 0,
        Low = 1,
        Medium = 2,
        Full = 3
    }
    #endregion Enums

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
        private GamepadButtonFlags _buttonflags;
        private const int deadZoneMax = 32767;
        private const int deadZoneMin = -32768;
        private float deadZonePercentL = 5;
        private float deadZonePercentR = 5;
        private const int _rumbleMax = 65535;
        private const int _rumbleMin = 0;
        private bool _rumbleActive = false;

        #endregion Fields

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDeviceImp"/> class.
        /// </summary>
        /// <param name="instance">The DeviceInstance.</param>
        public XInputDeviceImp()
        {
            //_Devices = new List<Controller>();
            /*
            for(int idx = 0; idx < (int)UserIndex.Four; idx++)
            {
                Controller cTmp = new Controller((UserIndex)idx);
                if (cTmp.IsConnected)
                {
                    // We can add the controller to the devices list now.
                    _Devices.Add(cTmp);

                }
            }
            */
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
                _Controller = contr;
                UpdateState();                                    
            }            
        }

        /// <summary>
        /// Reinitializes all the connected gamepads.
        /// </summary>
        public void ReinitializeGamepads()
        {
            /*
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
            */
        }

        /// <summary>
        /// This updates the packagenumber whenever a new information poll is received.
        /// </summary>
        private void UpdatePacketNumber()
        {
            int pnr = _Controller.GetState().PacketNumber;
            if (pnr == _lastpackageNumber)
            {
                _newUpdate = false;
                return;
            }
            _lastpackageNumber = pnr;
            _newUpdate = true;
            return;            
        }

        /// <summary>
        /// This controlls the update polling for the gamepad.
        /// This should be called at least every frame (more often for less input lag). Probably in the OnRenderFrame() function.
        /// Caution: More calls are putting more stress on the gamepad connection and the cpu.
        /// </summary>
        public bool UpdateState()
        {
            if (_Controller == null)
                return false;

            if (_Controller.IsConnected)
            {
                UpdatePacketNumber();
                if (_newUpdate)
                {
                    _lastState = _Controller.GetState();
                    _buttonflags = _Controller.GetState().Gamepad.Buttons;
                    return true;
                }
                return false;
            }
            return false;
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
        public List<int> GetPressedButtons()
        {            
            List<int> result = new List<int>();            

            foreach(var item in Enum.GetValues(typeof(GamepadButtonFlags)).Cast<Enum>().Where(item => _buttonflags.HasFlag(item))){
                result.Add((int)(FuseeXInputButtons)item);
            }

            return result;
        }

        /// <summary>
        /// Checks if the button is down.
        /// </summary>
        /// <param name="buttonIndex">The button to check.</param>
        /// <returns>True if the button is pressed and false if not.</returns>
        public bool IsButtonDown(int button)
        {
            return _buttonflags.HasFlag((GamepadButtonFlags)button);
        }

        #endregion Buttons

        #region Axis

        /// <summary>
        /// Returns the value of the left thumb sticks X-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbLXAxis()
        {
            int val = _lastState.Gamepad.LeftThumbX;
            if (val > _deadZoneL || val < _deadZoneL * -1)
            {
                if(val < 0)
                    return val + (int)_deadZoneL;

                if(val > 0)
                    return val - (int)_deadZoneL;
            }
                
            return 0;
        }

        /// <summary>
        /// Returns the value of the left thumb sticks Y-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbLYAxis()
        {
            int val = _lastState.Gamepad.LeftThumbY;
            if (val > _deadZoneL || val < _deadZoneL * -1)
            {
                if (val < 0)
                    return val + (int)_deadZoneL;

                if (val > 0)
                    return val - (int)_deadZoneL;
            }

            return 0;
        }

        /// <summary>
        /// Returns the value of the right thumb sticks X-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbRXAxis()
        {
            int val = _lastState.Gamepad.RightThumbX;
            if (val > _deadZoneR || val < _deadZoneR * -1)
            {
                if (val < 0)
                    return val + (int)_deadZoneR;

                if (val > 0)
                    return val - (int)_deadZoneR;
            }

            return 0;
        }

        /// <summary>
        /// Returns the value of the right thumb sticks Y-axis.
        /// </summary>
        /// <returns></returns>
        public int GetThumbRYAxis()
        {
            int val = _lastState.Gamepad.RightThumbY;
            if (val > _deadZoneR || val < _deadZoneR * -1)
            {
                if (val < 0)
                    return val + (int)_deadZoneR;

                if (val > 0)
                    return val - (int)_deadZoneR;
            }

            return 0;
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
        public bool SetCurrentController(Controller contr)
        {
            if (contr != null)
            {
                _Controller = contr;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the Deadzone to the gamepad.
        /// Both in the range of 0 to positive max
        /// </summary>
        /// <param name="dL">The Deadzone for the left stick in %.</param>
        /// <param name="dL">The Deadzone for the right stick in %.</param>
        public void SetDeadZone(float dzLeftThumb, float dzRightThumb)
        {
            deadZonePercentL = dzLeftThumb;
            deadZonePercentR = dzRightThumb;

            int range = deadZoneMin * -1 + deadZoneMax;
            _deadZoneL = ((deadZonePercentL / 100) * range) / 2;
            _deadZoneR = ((deadZonePercentR / 100) * range) / 2;
        }

        /// <summary>
        /// Sets the rumble motors vibration.
        /// </summary>
        /// <param name="vibL">The rumble vibration for the left motor.</param>
        /// <param name="vibR">The rumble vibration for the right motor.</param>
        public void SetRumble(int rumbleLeft, int rumbleRight)
        {
            ushort valLeft = (ushort)((_rumbleMax / 100) * rumbleLeft);
            ushort valRight = (ushort)((_rumbleMax / 100) * rumbleRight);
            _Controller.SetVibration(
                new Vibration()
                {
                    LeftMotorSpeed = valLeft,
                    RightMotorSpeed = valRight
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
            return _Controller.GetCapabilities(DeviceQueryType.Gamepad).Type.ToString();
        }

        /// <summary>
        /// Gets a user-friendly product name of the device.
        /// </summary>
        /// <returns>The device name.</returns>
        public string GetName()
        {
            return _Controller.GetCapabilities(DeviceQueryType.Gamepad).Type.ToString() + "_" + _Controller.UserIndex.ToString();
        }

        /// <summary>
        /// Returns the battery level of the current controller.
        /// </summary>
        /// <returns>Integer for the battery level. 0 = empty, 1 = low, 2 = medium, 3 = full</returns>
        public int BatteryLevel()
        {
            return (int) _Controller.GetBatteryInformation(BatteryDeviceType.Gamepad).BatteryLevel;            
        }

        /// <summary>
        /// Gets the current state of the input device. The state is used to poll the device.
        /// </summary>
        /// <returns>The state of the input device.</returns>
        private State GetState(UserIndex userIndex)
        {
            return _Controller.GetState();
        }
        #endregion RetrieveInformation        
    }
}

