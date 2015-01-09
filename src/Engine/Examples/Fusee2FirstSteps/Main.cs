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
        /*private SceneObjectContainer FindByName(string name, SceneContainer sc)
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
        private SceneObjectContainer _wuggy;*/

        private ScenePicker _sp;
        SceneContainer _scene;
        private SceneRenderer _sr;

        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(0.8f, 0.8f, 0.9f, 1);

            SceneContainer scene;
            var ser = new Serializer();
            using (var file = File.OpenRead(@"Assets/Wuggy.fus"))
            {
                _scene = ser.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
                _sr = new SceneRenderer(_scene, "Assets");
            }

            _sr = new SceneRenderer(_scene, "Assets");
            //_sp = new ScenePicker(RC);

            /*_wheelR = FindByName("WheelBigR", _scene);
            _wheelL = FindByName("WheelBigL", _scene);
            _wheelSR = FindByName("WheelSmallR", _scene);
            _wheelSL = FindByName("WheelSmallL.", _scene);
            _wuggy = FindByName("Wuggy", _scene);*/

            _angleR = 0.2f;
            _angleL = _angleR;
            _angleSL = 0.4f;
            _angleSL = _angleSR;


        }

        private float _angleR;
        private float _angleL;
        private float _angleSR;
        private float _angleSL;
        private float _globalAngle;
        private float _modelAngle;
        private float3 _move;

        // is called once a frame
        public override void RenderAFrame()
        {

            float mouseX = 0;
            //float mouseX1 = 0;
            float xValue = 0;
            float zValue = 0;

            /*float3 rotR = _wheelR.Transform.Rotation;
            float3 rotL = _wheelL.Transform.Rotation;
            float3 rotSR = _wheelSR.Transform.Rotation;
            float3 rotSL = _wheelSL.Transform.Rotation;
            float3 mov = _wuggy.Transform.Translation;
            float3 rotWuggy = _wuggy.Transform.Rotation;*/

            //rotate Cam by mouse click
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                mouseX = Input.Instance.GetAxis(InputAxis.MouseX);              
                
            }            

            /*//rotate Wuggy by mouse click
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                mouseX1 = Input.Instance.GetAxis(InputAxis.MouseX);

            } */          

            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            var mtxCam = float4x4.LookAt(0, 150, 700, 0, 150, 900, 0, 1, 0);

            RC.Model = float4x4.CreateTranslation(0, 0, 1500) * float4x4.CreateRotationY(_globalAngle);
            RC.View = mtxCam;
                        
            //take x,y,z value of float3 to create float
            /*rotR.x = _angleR;
            rotL.x = _angleL;
            rotSR.x = _angleSR;
            rotSL.x = _angleSL;
            rotWuggy.y = _modelAngle;

            _wheelR.Transform.Rotation = rotR;
            _wheelL.Transform.Rotation = rotL;
            _wheelSR.Transform.Rotation = rotSR;
            _wheelSL.Transform.Rotation = rotSL;*/

            if (Input.Instance.IsButton(MouseButtons.Left))
            {
               // _wuggy.Transform.Rotation = rotWuggy;                
            }


            //control Wuggy by WSAD
            if (Input.Instance.IsKey(KeyCodes.A))
            {
                xValue = 2f;
               //_wuggy.Transform.Rotation = rotWuggy;               

            }

            if (Input.Instance.IsKey(KeyCodes.D))
            {
                xValue = -2f;
                //_wuggy.Transform.Rotation = rotWuggy;

            }

            if (Input.Instance.IsKey(KeyCodes.W))
            {
                zValue = -5f;
                _angleR = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSR = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;
                _angleL = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSL = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;

            }

            if (Input.Instance.IsKey(KeyCodes.S))
            {
                zValue = 5f;
                _angleR = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSR = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;
                _angleL = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSL = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;

            }
   
            //update & speed of rotations
            _globalAngle = _globalAngle + mouseX * -50 * (float)Time.Instance.DeltaTime; //cam
            _modelAngle = _modelAngle + xValue * (float)Time.Instance.DeltaTime;        //Wuggy
            

            //change direction of wheel spin when wuggy is turned
            if (Input.Instance.IsKey(KeyCodes.A))
            {
                _angleR = _angleR + (xValue + zValue)* -1 * (float)Time.Instance.DeltaTime;
                _angleSR = _angleSR + (xValue + zValue) * -2 * (float)Time.Instance.DeltaTime;
                _angleL = _angleL + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSL = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;
            }

            if (Input.Instance.IsKey(KeyCodes.D))
            {
                _angleR = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSR = _angleSR + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;
                _angleL = _angleL + (xValue + zValue) * -1 * (float)Time.Instance.DeltaTime;
                _angleSL = _angleSL + (xValue + zValue) * -2 * (float)Time.Instance.DeltaTime;
            }

                     
            //just move if S or W is pressed
            float angleInDeg = _modelAngle * 180 / MathHelper.Pi;

           if (Input.Instance.IsKey(KeyCodes.S) || Input.Instance.IsKey(KeyCodes.W))
            {
                _move.z = (float)Math.Cos(_modelAngle)* zValue * 30 * (float)Time.Instance.DeltaTime;
                _move.x = (float)Math.Sin(_modelAngle)* zValue * 30 * (float)Time.Instance.DeltaTime;

                //_wuggy.Transform.Translation += _move;
            }

            Console.WriteLine("move" + _move);
            Console.WriteLine("Winkel " + _modelAngle);
            Console.WriteLine("Winkel1 " + angleInDeg);

            Point pickPos = new Point();
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                pickPos = Input.Instance.GetMousePos();
                //_sp.Pick(_scene, pickPos);
            }
                       
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
