using System;
using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Fusee.Engine;
using Fusee.SceneManagement;
using Fusee.Math;

namespace Examples.TextDemo
{
    public class TextDemo : RenderCanvas
    {
        private IFont _fontCabin12;
        private IFont _fontCabin20;
        private IFont _fontCabin30;
        private IFont _fontCousine20;

        private Mesh _textMeshCabin12;
        private Mesh _textMeshCabin20;
        private Mesh _textMeshCabin30;
        private Mesh _textMeshCabin30N;
        private Mesh _textMeshCabin30K;
        private Mesh _textMeshCousine20;

        private Mesh _mesh;
        private IShaderParam _vColor;

        private static float _angleHorz;

        private GUIButton testButton;

        public override void Init()
        {
            RC.ClearColor = new float4(0.5f, 0.5f, 0.8f, 1);

            // load fonts
            _fontCousine20 = RC.LoadFont("Assets/Cousine.ttf", 20);
            _fontCabin12 = RC.LoadFont("Assets/Cabin.ttf", 15);
            _fontCabin20 = RC.LoadFont("Assets/Cabin.ttf", 20);
            _fontCabin30 = RC.LoadFont("Assets/Cabin.ttf", 30);

            // button
            testButton = new GUIButton(RC, "Exit", _fontCabin12, 10, 10, 100, 25)
            {
                ButtonColor = new float4(0.7f, 0.7f, 0.7f, 1),
                TextColor = new float4(0, 0, 0, 1),
                BorderWidth = 1,
                BorderColor = new float4(0, 0, 0, 1)
            };

            testButton.Refresh();

            testButton.OnGUIButtonDown += OnGUIButtonDown;
            testButton.OnGUIButtonUp += OnGUIButtonUp;
            testButton.OnGUIButtonEnter += OnGUIButtonEnter;
            testButton.OnGUIButtonLeave += OnGUIButtonLeave;

            // text
            _textMeshCabin20 = RC.GetTextMesh("The quick brown fox jumps over the lazy dog.", _fontCabin20, 8, 100, new float4(1, 1, 1, 1));
            _textMeshCabin30 = RC.GetTextMesh("The quick brown fox jumps over the lazy dog.", _fontCabin30, 8, 140, new float4(0, 0, 0, 0.5f));

            // dummy cube
            _mesh = new Cube();
        
            var sp = MoreShaders.GetShader("color", RC);
            RC.SetShader(sp);

            _vColor = sp.GetShaderParam("color");
            RC.SetShaderParam(_vColor, new float4(1, 1, 1, 1));
            
            _angleHorz = 0;
        }

        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            // dummy cube
            _angleHorz += 0.002f;

            var mtxRot = float4x4.CreateRotationY(_angleHorz) * float4x4.CreateRotationX(0);
            var mtxCam = float4x4.LookAt(0, 100, 200, 0, 0, 0, 0, 1, 0);

            RC.ModelView = float4x4.Scale(100, 100, 100) * mtxRot * float4x4.CreateTranslation(-60, 0, 0) * mtxCam;
            RC.SetShaderParam(_vColor, new float4(0.8f, 0.1f, 0.1f, 1));
            RC.Render(_mesh);

            // button
            RC.TextOut(testButton.GUIMesh, _fontCabin12);

            // text
            RC.TextOut(_textMeshCabin20, _fontCabin20);
            RC.TextOut(_textMeshCabin30, _fontCabin30);

            // text examples: dynamic text
            var col6 = new float4(0, 1, 1, 1);
            RC.TextOut("Framerate: " + Time.Instance.FramePerSecondSmooth + "fps", _fontCabin20, col6, 8, 210);
            RC.TextOut("Time: " + Math.Round(Time.Instance.TimeSinceStart, 1) + " seconds", _fontCabin20, col6, 8, 250);

            Present();
        }

        private void OnGUIButtonDown(object sender, MouseEventArgs mea)
        {
            Debug.WriteLine("ButtonDown");
        }

        private void OnGUIButtonUp(object sender, MouseEventArgs mea)
        {
            if (mea.Button == MouseButtons.Left)
                Environment.Exit(0);
        }

        private void OnGUIButtonEnter(object sender, MouseEventArgs mea)
        {
            testButton.TextColor = new float4(0.8f, 0.1f, 0.1f, 1);
            testButton.Refresh();
        }

        private void OnGUIButtonLeave(object sender, MouseEventArgs mea)
        {
            testButton.TextColor = new float4(0, 0, 0, 1);
            testButton.Refresh();
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
            var app = new TextDemo();
            app.Run();
        }
    }
}
