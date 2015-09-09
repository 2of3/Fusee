using System.Diagnostics;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.InputDevices
{
    [FuseeApplication(Name = "InputDevices",
        Description = "A sample application showing FUSEE's support for controller and other input devices.")]
    public class InputDevices : RenderCanvas
    {
        private Mesh _meshTea;

        private ShaderProgram _spColor;
        private IShaderParam _colorParam;

        public override void Init()
        {
            //Input.Instance.InitializeDevices();
            Input.Instance.InitializeXInputDevices();
            

            _meshTea = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");

            _spColor = MoreShaders.GetDiffuseColorShader(RC);
            _colorParam = _spColor.GetShaderParam("color");
        }

        public override void RenderAFrame()
        {
            #region PullInput
            float y = 0;
            float z = 0;
            float x = 0;
            if (Input.Instance.CountXIDevices() != 0)
            {
                System.Diagnostics.Debug.WriteLine(Input.Instance.GetXIDevice(0).GetAxis(XInputDevice.Axis.LTHorizontal));
                System.Diagnostics.Debug.WriteLine(Input.Instance.GetXIDevice(0).GetAxis(XInputDevice.Axis.LTVertical));

                y = 50 * Input.Instance.GetXIDevice(0).GetAxis(XInputDevice.Axis.LTVertical);

                x = 50 * Input.Instance.GetXIDevice(0).GetAxis(XInputDevice.Axis.LTHorizontal);

                z = 50 * Input.Instance.GetXIDevice(0).GetAxis(XInputDevice.Axis.LeftZ);
                z = 50 * Input.Instance.GetXIDevice(0).GetAxis(XInputDevice.Axis.RightZ);
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
