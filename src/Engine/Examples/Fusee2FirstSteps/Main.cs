using System;
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
            foreach (SceneObjectContainer soc in children)
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

        private SceneObjectContainer _wheelR;
        private SceneObjectContainer _wheelL;
        private SceneObjectContainer _wheelSL;
        private SceneObjectContainer _wheelSR;
        private SceneObjectContainer _wuggy;

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
            _wheelR = FindByName("WheelBigR", scene);
            _wheelL = FindByName("WheelBigL", scene);
            _wheelSR = FindByName("WheelSmallR", scene);
            _wheelSL = FindByName("WheelSmallL.", scene);

            _wuggy = FindByName("Wuggy", scene);
            _angle = 0.02f;


        }


        private float _angle;
        private float _modelAngle;
        private float3 _move = new float3(0, 0, 900);

        // is called once a frame
        public override void RenderAFrame()
        {

            float mouseX = 0;
            float xValue = 0;
            float zValue = 0;

            float3 rot = _wheelR.Transform.Rotation;
            float3 mov = _wuggy.Transform.Translation;

            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                mouseX = Input.Instance.GetAxis(InputAxis.MouseX);
                
            }

            if (Input.Instance.IsKey(KeyCodes.Left))
            {
                xValue = 5f;
                _wuggy.Transform.Rotation = new float3(0, 1.570f, 0);
            }

            if (Input.Instance.IsKey(KeyCodes.Right))
            {
                xValue = -5f;
                _wuggy.Transform.Rotation = new float3(0, -1.570f, 0);

            }

            if (Input.Instance.IsKey(KeyCodes.Up))
            {
                zValue = -5f;
                _wuggy.Transform.Rotation = new float3(0, 3.141f, 0);

            }

            if (Input.Instance.IsKey(KeyCodes.Down))
            {
                zValue = 5f;
                _wuggy.Transform.Rotation = new float3(0, 0, 0);

            }

            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
            RC.ModelView = float4x4.CreateTranslation(0, 0, 500) * float4x4.CreateRotationY(_modelAngle) * float4x4.LookAt(0, 150, 700, 0, 150, 900, 0, 1, 0);
                        
            rot.x = _angle;

            mov.x = _move.x;
            mov.z = _move.z;

            _wheelR.Transform.Rotation = rot;
            _wheelL.Transform.Rotation = rot;
            _wheelSR.Transform.Rotation = rot;
            _wheelSL.Transform.Rotation = rot;


            _wuggy.Transform.Translation = mov;

            _modelAngle = _modelAngle + mouseX * -10 * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKey(KeyCodes.Left) || Input.Instance.IsKey(KeyCodes.Up))
            {
                _angle = (_angle + xValue + zValue *-10 * (float)Time.Instance.DeltaTime);
            }
            else
            {
                _angle = _angle + xValue + zValue * -10 * (float)Time.Instance.DeltaTime;
            }

            _move.x = _move.x + xValue * -30 * (float)Time.Instance.DeltaTime;
            _move.z = _move.z + zValue * -30 * (float)Time.Instance.DeltaTime;

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
