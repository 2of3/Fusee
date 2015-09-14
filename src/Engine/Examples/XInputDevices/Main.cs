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

        public override void Init()
        {
            // Initialize the xinput devices and save the device with id one to access it faster.
            Input.Instance.InitializeXInputDevices();
            _gamepad = Input.Instance.GetXIDevice(0);

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

                y = _gamepad.GetAxis(XInputDevice.Axis.LTVertical);
                x = _gamepad.GetAxis(XInputDevice.Axis.LTHorizontal);

                System.Diagnostics.Debug.WriteLine("LT Vertical: " + y);
                System.Diagnostics.Debug.WriteLine("LT Horizontal: " + x);
                
                // Using the triggers of the gamepad to adjust the objects z axis.
                z = -_gamepad.GetAxis(XInputDevice.Axis.LeftZ);
                z += _gamepad.GetAxis(XInputDevice.Axis.RightZ);
                z = z * -1; // Now the z axis feels more natural.

                #region Buttons                
                // Asking the buttons individually if they have been pressed.
                // This is useful for if else stuff, etc.
                if(_gamepad.IsButtonDown((int)FuseeXInputButtons.A))
                {
                    System.Diagnostics.Debug.WriteLine("Button pressed: " + FuseeXInputButtons.A);
                } else if (_gamepad.IsButtonDown((int)FuseeXInputButtons.B))
                {
                    System.Diagnostics.Debug.WriteLine("Button pressed: " + FuseeXInputButtons.B);
                } else if (_gamepad.IsButtonDown((int)FuseeXInputButtons.Start))
                {
                    System.Diagnostics.Debug.WriteLine("Button pressed: " + FuseeXInputButtons.Start);
                } else if (_gamepad.IsButtonDown((int)FuseeXInputButtons.Back))
                {
                    System.Diagnostics.Debug.WriteLine("Button pressed: " + FuseeXInputButtons.Back);
                }                                              
                
                // Method to get all buttons at once.
                // Can be more costly than asking one button, but makes sense in specific situations like "combo" button presses etc.                
                string res = "";
                List<FuseeXInputButtons> buttons = new List<FuseeXInputButtons>();
                foreach(var btn in _gamepad.GetPressedButtons())
                {
                    buttons.Add((FuseeXInputButtons)btn);
                    res += " | " + (FuseeXInputButtons)btn;
                }
                System.Diagnostics.Debug.WriteLine("Buttons pressed: " + res);

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

    }
}
