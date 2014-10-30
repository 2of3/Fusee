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
            _angle = 0.2f;
            _angleS = 0.4f;


        }

        private float _angle;
        private float _angleS;
        private float _globalAngle;
        private float _modelAngle;
        private float3 _move = new float3(0, 0, 900);

        // is called once a frame
        public override void RenderAFrame()
        {

            float mouseX = 0;
            float mouseX1 = 0;
            float xValue = 0;
            float zValue = 0;

            float3 rot = _wheelR.Transform.Rotation;
            float3 rotS = _wheelSR.Transform.Rotation;
            float3 mov = _wuggy.Transform.Translation;
            float3 rotWuggy = _wuggy.Transform.Rotation;

            //rotate Cam by mouse click
            if (Input.Instance.IsButton(MouseButtons.Right))
            {
                mouseX = Input.Instance.GetAxis(InputAxis.MouseX);              
                
            }

            //rotate Wuggy by mouse click
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                mouseX1 = Input.Instance.GetAxis(InputAxis.MouseX);

            }            

            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            var mtxCam = float4x4.LookAt(0, 150, 700, 0, 150, 900, 0, 1, 0);

            RC.Model = float4x4.CreateTranslation(0, 0, 500) * float4x4.CreateRotationY(_globalAngle);
            RC.View = mtxCam;
                        
            //take x,y,z value of float3 to create float
            rot.x = _angle;
            rotS.x = _angleS;
            rotWuggy.y = _modelAngle;
           

            mov.x = _move.x;
            mov.z = _move.z;

            _wheelR.Transform.Rotation = rot;
            _wheelL.Transform.Rotation = rot;
            _wheelSR.Transform.Rotation = rotS;
            _wheelSL.Transform.Rotation = rotS;

            _wuggy.Transform.Translation = mov;


            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _wuggy.Transform.Rotation = rotWuggy;                
            }


            //control Wuggy by WSAD
            if (Input.Instance.IsKey(KeyCodes.A))
            {
                xValue = 5f;
                _wuggy.Transform.Rotation = new float3(0, 1.570f, 0);               

            }

            if (Input.Instance.IsKey(KeyCodes.D))
            {
                xValue = -5f;
                _wuggy.Transform.Rotation = new float3(0, -1.570f, 0);

            }

            if (Input.Instance.IsKey(KeyCodes.W))
            {
                zValue = -5f;
                _wuggy.Transform.Rotation = new float3(0, _modelAngle, 0);

            }

            if (Input.Instance.IsKey(KeyCodes.S))
            {
                zValue = 5f;
                _wuggy.Transform.Rotation = new float3(0, _modelAngle, 0);

            }
   
            //update & speed of rotations
            _globalAngle = _globalAngle + mouseX * -50 * (float)Time.Instance.DeltaTime;
            _modelAngle = _modelAngle + mouseX1 * -50 * (float)Time.Instance.DeltaTime;
            

            if (Input.Instance.IsKey(KeyCodes.Left) || Input.Instance.IsKey(KeyCodes.Down))
            {
                _angle = _angle + (xValue + zValue)* -1 * (float)Time.Instance.DeltaTime;
                _angleS = _angleS + (xValue + zValue) * -2 * (float)Time.Instance.DeltaTime;
            }
            else
            {
                _angle = _angle + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleS = _angleS + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;
            }

            _move.x = _move.x + xValue* -30 * (float)Time.Instance.DeltaTime; //task: control Wuggy by mouse(rotation) + W (move forward) --> try to move Wuggy on xz layer (involve _modelAngle?) 
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
