using System.Diagnostics;
using Fusee.Engine;
using Fusee.Math;
using System.Collections.Generic;

namespace Examples.InputDevices
{
    [FuseeApplication(Name = "InputDevices",
        Description = "A sample application showing FUSEE's support for controller and other input devices.")]
    public class InputDevices : RenderCanvas
    {
        private Mesh _meshTea;

        private ShaderProgram _spColor;
        private IShaderParam _colorParam;

        private XInputDevice _gamepad;
        private bool _rumble = false;

        public override void Init()
        {
            // Initialize the xinput devices and save the device with id one to access it faster.
            Input.Instance.InitializeXInputDevices();
            _gamepad = Input.Instance.GetXIDevice(0);
            Debug.WriteLine("Device Name: " + _gamepad.GetName());
            // Setting the deadzone in % of one range. Deadzone is x% of the range 255.
            _gamepad.SetDeadZone(10, 10);

            _meshTea = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");

            _spColor = MoreShaders.GetDiffuseColorShader(RC);
            _colorParam = _spColor.GetShaderParam("color");
        }

        public override void RenderAFrame()
        {
            #region PullInput
            float x = 0;
            float y = 0;
            float z = 0;                       
            
            if (_gamepad != null)
            {
                // This is a very important call.
                // This asks the input device if new data has been generated.
                _gamepad.UpdateStatus();

                y = 2 * _gamepad.GetAxis(XInputDevice.Axis.LTVertical);
                x = 2 * _gamepad.GetAxis(XInputDevice.Axis.LTHorizontal);

                if(x != 0)
                    Debug.WriteLine("LT Horizontal: " + x);

                if (y != 0)
                    Debug.WriteLine("LT Vertical: " + y);                
                
                // Using the triggers of the gamepad to adjust the objects z axis.
                z = -_gamepad.GetAxis(XInputDevice.Axis.LeftZ);
                z += _gamepad.GetAxis(XInputDevice.Axis.RightZ);
                z = z * -1; // Now the z axis feels more natural.

                #region Buttons                
                // Asking the buttons individually if they have been pressed.
                // This is useful for if else stuff, etc.
                if(_gamepad.IsButtonDown((int)FuseeXInputButtons.DPadUp))
                {
                    Debug.WriteLine("Button pressed: " + FuseeXInputButtons.DPadUp);
                } else if (_gamepad.IsButtonDown((int)FuseeXInputButtons.DPadDown))
                {
                    Debug.WriteLine("Button pressed: " + FuseeXInputButtons.DPadDown);
                } else if (_gamepad.IsButtonDown((int)FuseeXInputButtons.DPadLeft))
                {
                    Debug.WriteLine("Button pressed: " + FuseeXInputButtons.DPadLeft);
                } else if (_gamepad.IsButtonDown((int)FuseeXInputButtons.DPadRight))
                {
                    Debug.WriteLine("Button pressed: " + FuseeXInputButtons.DPadRight);
                }                                              
                
                // Method to get all buttons at once.
                // Can be more costly than asking one button, but makes sense in specific situations like "combo" button presses etc.                
                string res = "";
                List<FuseeXInputButtons> buttons = new List<FuseeXInputButtons>();
                foreach(var btn in _gamepad.GetPressedButtons())
                {
                    if (btn == (int)FuseeXInputButtons.None)
                        continue;

                    buttons.Add((FuseeXInputButtons)btn);
                    res += " | " + (FuseeXInputButtons)btn;
                }
                if (!string.IsNullOrEmpty(res))
                    Debug.WriteLine("Buttons pressed: " + res);                   
                #endregion Buttons
            }
            else
            {
                if (Input.Instance.IsKeyDown(KeyCodes.Up))
                    y++;
                if (Input.Instance.IsKeyDown(KeyCodes.Down))
                    y--;
                if (Input.Instance.IsKeyDown(KeyCodes.Left))
                    x--;
                if (Input.Instance.IsKeyDown(KeyCodes.Right))
                    x++;
            }

            // This is how you can use the rumble functionality.
            // The parameters in SetRumble() represent a percentage of the maximum rumble capability.
            int zRumble = (int)MapRange(_gamepad.GetAxis(XInputDevice.Axis.RightZ));
            Debug.WriteLine("Rumble intesity: " + zRumble);
            if (zRumble != 0)
            {
                _gamepad.SetRumble(zRumble, zRumble);
                Debug.WriteLine("Enable rumble.");                    
                _rumble = true;
            } else
            {   
                if(_rumble)
                {
                    _gamepad.SetRumble(0, 0);
                    _rumble = false;
                }                                                
                Debug.WriteLine("Disable rumble.");
            }

            #endregion

            #region Render
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
                        
            var mtxCam = float4x4.LookAt(0, 200, 500, 0, 0, 0, 0, 1, 0);

            // first mesh
            RC.ModelView = float4x4.CreateTranslation(x, 50 + y, 200 + z) * mtxCam;

            RC.SetShader(_spColor);
            RC.SetShaderParam(_colorParam, new float4(0.5f, 0.8f, 0, 1));

            RC.Render(_meshTea);
            #endregion

            Present();
            Debug.WriteLine("Battery: " + ((GamepadBatteryLevel)_gamepad.BatteryLevel()));
            Debug.WriteLine("--");
        }

        public override void Resize()
        {
            // is called when the window is resized
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 10000);
        }

        public static void Main()
        {
            var app = new InputDevices();
            app.Run();
        }

        /// <summary>
        /// Can map a range of numbers from x to y to a range of u to v.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="leftMin"></param>
        /// <param name="leftMax"></param>
        /// <param name="rightMin"></param>
        /// <param name="rightMax"></param>
        /// <returns></returns>
        private float MapRange(float value, int leftMin = 0, int leftMax = 255, int rightMin = 0, int rightMax = 100)
        {
            //Figure out how 'wide' each range is
            int leftSpan = leftMax - leftMin;
            int rightSpan = rightMax - rightMin;

            //Convert the ranges
            float valueScaled = (float)(value - leftMin) / (float)(leftSpan);
            return rightMin + (valueScaled * rightSpan);
        }
    }
}
