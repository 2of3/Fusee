using System.IO;
using Fusee.Engine;
using Fusee.Engine.SimpleScene;
using Fusee.Math;
using Fusee.Serialization;

namespace Examples.Fusee2FirstSteps
{
    public class Fusee2FirstSteps : RenderCanvas
    {
        private SceneRenderer _sr;

        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(0.8f, 0.8f, 0.9f, 1);

            SceneContainer scene;
            var ser = new Serializer();
            using (var file = File.OpenRead(@"Assets/Wuggy.fus"))
            {
                scene = ser.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
                _sr = new SceneRenderer(scene, "Assets");
            }
        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
            RC.ModelView = float4x4.CreateTranslation(0, 0, 500);
            _sr.Render(RC);
            Present();
        }

        // is called when the window was resized
        public override void Resize()
        {
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 10000);
        }

        public static void Main()
        {
            var app = new Fusee2FirstSteps();
            app.Run();
        }
    }
}
