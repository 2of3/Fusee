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
        private float3 _move = new float3(0, 0, 0);

        

        // is called once a frame
        public override void RenderAFrame()
        {

            float mouseX = 0;
            //float mouseX1 = 0;
            float xValue = 0;
            float zValue = 0;

            float3 rotR = _wheelR.Transform.Rotation;
            float3 rotL = _wheelL.Transform.Rotation;
            float3 rotSR = _wheelSR.Transform.Rotation;
            float3 rotSL = _wheelSL.Transform.Rotation;
            float3 mov = _wuggy.Transform.Translation;
            float3 rotWuggy = _wuggy.Transform.Rotation;

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
            rotR.x = _angleR;
            rotL.x = _angleL;
            rotSR.x = _angleSR;
            rotSL.x = _angleSL;
            rotWuggy.y = _modelAngle;

            mov.x = _move.x;
            mov.z = _move.z;

            _wheelR.Transform.Rotation = rotR;
            _wheelL.Transform.Rotation = rotL;
            _wheelSR.Transform.Rotation = rotSR;
            _wheelSL.Transform.Rotation = rotSL;

            _wuggy.Transform.Translation = mov;           

            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _wuggy.Transform.Rotation = rotWuggy;                
            }


            //control Wuggy by WSAD
            if (Input.Instance.IsKey(KeyCodes.A))
            {
                xValue = 2f;
               _wuggy.Transform.Rotation = rotWuggy;               

            }

            if (Input.Instance.IsKey(KeyCodes.D))
            {
                xValue = -2f;
                _wuggy.Transform.Rotation = rotWuggy;

            }

            if (Input.Instance.IsKey(KeyCodes.W))
            {
                zValue = -5f;
                //xValue = -5f;
                _angleR = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSR = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;
                _angleL = _angleR + (xValue + zValue) * 1 * (float)Time.Instance.DeltaTime;
                _angleSL = _angleSL + (xValue + zValue) * 2 * (float)Time.Instance.DeltaTime;

            }

            if (Input.Instance.IsKey(KeyCodes.S))
            {
                zValue = 5f;
               // xValue = 5f;
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
            float _xz = _move.z/(float)Math.Cos(_modelAngle);           

            if (Input.Instance.IsKey(KeyCodes.S) || Input.Instance.IsKey(KeyCodes.W))
            {
                Console.WriteLine("_modelAngle " + _modelAngle*180/MathHelper.Pi);
                //_move = _move *60*(float)Time.Instance.DeltaTime;  //task: control Wuggy by mouse(rotation) + W (move forward) --> try to move Wuggy on xz layer (involve _modelAngle?)  
                _move.z = _move.z +zValue * -30 * (float)Time.Instance.DeltaTime;
                if (_modelAngle == 0 || _modelAngle == 180*Math.PI/180)
                {
                    _move.x = 0;      
                    
                }                
                else
                {
                    _move.x = _xz + zValue * (float)Time.Instance.DeltaTime;
                }

                if (_modelAngle >= 3.141)
                {
                    _move.x = -_xz + zValue * (float)Time.Instance.DeltaTime;
                }
                   
                
                
                Console.WriteLine("move" + _move);
                
            }

            //Console.WriteLine("float 3 _move: " + _move);
            
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
