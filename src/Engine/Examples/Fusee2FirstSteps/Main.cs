using System.IO;
using Fusee.Engine;
using Fusee.Engine.SimpleScene;
using Fusee.Math;
using Fusee.Serialization;
using System.Collections;

namespace Examples.Fusee2FirstSteps
{
    public class Fusee2FirstSteps : RenderCanvas
    {
        private SceneRenderer _sr;

        private SceneObjectContainer FindByName(string name, SceneContainer sc) 
        {
            return FindByName(name, sc.Children);
        }

        private SceneObjectContainer FindByName(string name, IEnumerable children)
        {
            if (children == null)
            {
                return null;
            }
            foreach(SceneObjectContainer soc in children)
            {
                if (soc.Name == name)
                {
                    return soc;
                }
                SceneObjectContainer ret = FindByName(name, soc.Children);
                if (ret != null)
                {
                    return ret;
                }
            }
            return null;
        }

        private SceneObjectContainer _wheel;

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
            _wheel = FindByName("WheelBigR", scene);
            _angle = 0.02f;
        }
        private float _angle;
        // is called once a frame
        public override void RenderAFrame()
        {
            float mouseX = 0;
            if (Input.Instance.IsButton(MouseButtons.Left)) 
            {
                mouseX = Input.Instance.GetAxis(InputAxis.MouseX);    
            }
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
            RC.ModelView = float4x4.CreateTranslation(0, -200, 500)*float4x4.CreateRotationY(_angle);
            float3 rot = _wheel.Transform.Rotation;
            rot.x = _angle;
            _wheel.Transform.Rotation = rot;
            _angle = _angle + mouseX * -10 * (float)Time.Instance.DeltaTime;
            _sr.Render(RC);
            Present();
        }

        // is called when the window was resized
        public override void Resize()
        {
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 10, 10000);
        }

        public static void Main()
        {
            var app = new Fusee2FirstSteps();
            app.Run();
        }
    }
}
