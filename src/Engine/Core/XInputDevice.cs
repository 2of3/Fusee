using System;
using System.Collections.Generic;

namespace Fusee.Engine
{
    /// <summary>
    /// Represents one instance of an input device other than keyboard or mouse
    /// </summary>
    public class XInputDevice
    {
        public enum Axis
        {
            LTHorizontal,
            LTVertical,
            RTHorizontal,
            RTVertical,
            LeftZ,
            RightZ
        }

        private readonly IXInputDeviceImp _inputDeviceImp;

        /// <summary>
        /// Initializes a new instance of the <see cref="XInputDevice"/> class.
        /// </summary>
        /// <param name="inputDeviceImp">The input device imp.</param>
        public XInputDevice(IXInputDeviceImp inputDeviceImp)
        {
            _inputDeviceImp = inputDeviceImp;
        }

        public XInputDevice()
        {

        }

        /// <summary>
        /// Very important function. Updates the device status.
        /// Without calling this every intervall (e.g. frame) there will be no data.
        /// </summary>
        public void UpdateStatus()
        {
            _inputDeviceImp.UpdateState();
        }

        /// <summary>
        /// Gets the current value of one axis (i.e. joystick or trigger).
        /// </summary>
        /// <param name="axis">Specifies the desired axis, can be "horizontal", "vertical" or "z".</param>
        /// <returns>
        /// The current value (between -1.0 and +1.0) for the specified axis.
        /// </returns>
        public float GetAxis(Axis axis)
        {
            UpdateStatus();
            switch (axis)
            {
                case Axis.LTHorizontal:
                    return MapStickRange(_inputDeviceImp.GetThumbLXAxis());

                case Axis.LTVertical:
                    return MapStickRange(_inputDeviceImp.GetThumbLYAxis());

                case Axis.RTHorizontal:
                    return MapStickRange(_inputDeviceImp.GetThumbRXAxis());

                case Axis.RTVertical:
                    return MapStickRange(_inputDeviceImp.GetThumbRYAxis());

                case Axis.RightZ:
                    return _inputDeviceImp.GetZAxisLeft();

                case Axis.LeftZ:
                    return _inputDeviceImp.GetZAxisRight();

                default:
                    return 0.0f;
            }
        }

        private float MapStickRange(float value, int leftMin = -32768, int leftMax = 32767, int rightMin = -255, int rightMax = 255)
        {
            //Figure out how 'wide' each range is
            int leftSpan = leftMax - leftMin;
            int rightSpan = rightMax - rightMin;

            //Convert the left range into a -255 to 255 range (float)
            float valueScaled = (float)(value - leftMin) / (float)(leftSpan);

            //Convert the 0-1 range into a value in the right range.
            return rightMin + (valueScaled * rightSpan);
        }

        /// <summary>
        /// Gets the name of the instance 
        /// </summary>
        /// <returns>The product name of the queried input device.</returns>
        public string Name()
        {
            return _inputDeviceImp.GetName();
        }

        /// <summary>
        /// Gets the index of the currently pressed button on the input device.
        /// </summary>
        /// <returns>The index of the currently pressed button</returns>
        public List<int> GetPressedButtons()
        {
            return _inputDeviceImp.GetPressedButtons();
        }

        /// <summary>
        /// Checks if a specified button is pressed in the current frame on the input device.
        /// </summary>
        /// <param name="buttonIndex">The index of the button that is checked.</param>
        /// <returns>True if the button at the specified index is pressed in the current frame and false if not.</returns>
        public bool IsButtonDown(int buttonIndex)
        {
            return _inputDeviceImp.IsButtonDown(buttonIndex);
        }

        /// <summary>
        /// Counts the buttons on the input device.
        /// </summary>
        /// <returns>The amount of buttons on the device.</returns>
        public int GetButtonCount()
        {
            return _inputDeviceImp.GetButtonCount();
        }

        /// <summary>
        /// Gets the category of the input device.
        /// </summary>
        /// <returns>The name of the Device Category.</returns>
        public String GetCategory()
        {
            return _inputDeviceImp.GetCategory();
        }

        /// <summary>
        /// Returns the Device Name.
        /// </summary>
        /// <returns></returns>
        public String GetName()
        {
            return _inputDeviceImp.GetName();
        }

        /// <summary>
        /// Sets the vibration of the rumble motors.
        /// </summary>
        /// <param name="rumbleLeft"></param>
        /// <param name="rumbleRight"></param>
        public void SetRumble(int rumbleLeft, int rumbleRight)
        {
            _inputDeviceImp.SetRumble(rumbleLeft, rumbleRight);
        }

        public void SetDeadZone(float dzLeftThumb, float dzRightThumb)
        {
            _inputDeviceImp.SetDeadZone(dzLeftThumb, dzRightThumb);
        }


        public int BatteryLevel()
        {
            return _inputDeviceImp.BatteryLevel();
        }

    }
}
