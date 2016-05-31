using System;
using System.Collections.Generic;
using System.Net.Mime;
using Emgu.CV;
using Emgu.CV.Structure;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{


    [FuseeApplication(Name = "DepthVideo", Description = "Integtrating a video with depth information.")]
    public class DepthVideo : RenderCanvas
    {
        #region S3D-Shader + Depth

        // GLSL
        private const string VsS3dDepth = @"
            attribute vec4 fuColor;
            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;
        
            varying vec4 vColor;
            varying vec3 vNormal;
            varying vec2 vUV;
        
            uniform mat4 FUSEE_MV;
            uniform mat4 FUSEE_P;
            uniform mat4 FUSEE_ITMV;
            uniform mat4 FUSEE_MVP;

            void main()
            {
               
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);

                //pos = FUSEE_MV[3];
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
                vUV = fuUV;
            }";

        private const string PsS3dDepth = @"
            #ifdef GL_ES
                precision highp float;
            #endif
        
            uniform sampler2D vTexture;
            uniform sampler2D textureDepth;
            uniform vec4 vColor;
            varying vec3 vNormal;
            varying vec2 vUV;

            void main()
            {
                vec4 colTex = vColor * texture2D(vTexture, vUV);
               
                
                float depthTexValue = 1-texture(textureDepth, vUV);
                if(depthTexValue == 1)          
                {
                    gl_FragDepth = 1;
                }
                else
                {

                    gl_FragDepth = gl_FragCoord.z + (depthTexValue-0.5)*0.1;  
                }
                
                gl_FragColor = dot(vColor, vec4(0, 0, 0, 1)) * colTex * dot(vNormal, vec3(0, 0, -1));
               
                
            }";

        #endregion

        #region S3D-Shader

        // GLSL
        private const string VsS3D = @"
            attribute vec4 fuColor;
            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;
        
            varying vec4 vColor;
            varying vec3 vNormal;
            varying vec2 vUV;
        
            uniform mat4 FUSEE_MV;
            uniform mat4 FUSEE_P;
            uniform mat4 FUSEE_ITMV;
            uniform mat4 FUSEE_MVP;
            varying vec4 clip;

            void main()
            {
  
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
                clip = FUSEE_P *FUSEE_MV * vec4(fuVertex, 1.0);
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
                vUV = fuUV;
            }";

        private const string PsS3D = @"
            #ifdef GL_ES
                precision highp float;
            #endif
        
            uniform sampler2D vTexture;
            uniform vec4 vColor;
            varying vec3 vNormal;
            varying vec2 vUV;
            varying vec4 clip;
   

            void main()
            {
                vec4 colTex = vColor * texture2D(vTexture, vUV);      

                gl_FragColor = dot(vColor, vec4(0, 0, 0, 1)) * colTex * dot(vNormal, vec3(0, 0, -1)* 0.5);
               
                
            }";

        #endregion

        #region Depth Shader

        // GLSL
        private const string VsDepth = @"
            attribute vec4 fuColor;
            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;
        
            varying vec4 vColor;
            varying vec3 vNormal;
            varying vec2 vUV;
        
            uniform mat4 FUSEE_MV;
            uniform mat4 FUSEE_P;
            uniform mat4 FUSEE_ITMV;
            varying vec4 pos;

            void main()
            {
                mat4 FUSEE_MVP = FUSEE_P * FUSEE_MV;
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
                vUV = fuUV;
            }";

        private const string PsDepth = @"
            #ifdef GL_ES
                precision highp float;
            #endif
        

            uniform sampler2D textureDepth;
          
            varying vec3 vNormal;
            varying vec2 vUV;
            varying vec4 pos;
            float zNear = 1;
            float zFar = 100;
            vec4 transparency = 1;
            void main()
            {                            
                float depthTexValue = 1-texture(textureDepth, vUV);
                if(depthTexValue == 1)          
                {
                    gl_FragDepth = 1;
                }
                else
                {

                    gl_FragDepth = gl_FragCoord.z + (depthTexValue-0.5)*0.1;  
                }  
                gl_FragColor = depthTexValue;                        
                
            }";

        #endregion



        



        private Mesh _meshCube;//, _meshSphere, _meshTeapot;


        private float3 _move = float3.Zero;
  //      private float3 test = new float3(0,0,-25);
        // angle variables
        private static float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;
        private const float _rotationSpeed = 1f;
        private const float _damping = 0.92f;

        // variables for stereoshader-> color only
        private ShaderProgram _shaderProgram3DColor;
        private IShaderParam _s3dColorParam;
        private IShaderParam _s3dTextureParam;
        private IShaderParam _s3dDepthTextureParam;

        // 2D color shader
        private ShaderProgram _colorShader;
        private IShaderParam _color;

        private ITexture _iTexture;



     //   private ScreenS3D _screenS3D, _screenS3D_1, _screenS3D_2;

        private StereoCameraRig _stereoCameraRig;
        //private Stereo3D _stereoCameraRig;

        private List<Object3D> _object3DList = new List<Object3D>();
        private List<ScreenS3D> _screenS3Ds = new List<ScreenS3D>();
        private int _selectedScreen;

        // is called on startup
        public override void Init()
        {
           
            Console.WriteLine("Init");
            RC.ClearColor = new float4(1f, 1f,1f, 1);


            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");

            //stereoshader -> color only
            _shaderProgram3DColor = RC.CreateShader(VsS3D, PsS3D);
            _s3dColorParam = _shaderProgram3DColor.GetShaderParam("vColor");
            _s3dTextureParam = _shaderProgram3DColor.GetShaderParam("vTexture");


            //normal 2D color Shader
            _colorShader = Shaders.GetColorShader(RC);
            _color = _colorShader.GetShaderParam("color");

            //Load Texrure
            _iTexture = RC.CreateTexture(RC.LoadImage("Assets/world_map.jpg"));

            //StereoCameraRig
            _stereoCameraRig = new StereoCameraRig(Stereo3DMode.Anaglyph, Width, Height, 0.2f);
            _stereoCameraRig.AttachToContext(RC);



            var videoConfigs = VideoConfigParser.ParseConfigs();
            //Set up screen object
            foreach (var config in videoConfigs)
            {
                _screenS3Ds.Add(new ScreenS3D(RC, config));
            }

            //Create Objects3D
            var Cube3D_1 = new Object3D(RC, new float3(0, 0, -50), new float3((float)Math.PI / 4, (float)Math.PI / 4, 0), _meshCube, new float3(0.01f, 0.01f, 0.01f), 0.01f);
            Cube3D_1.SimpleTextureMaterial(_shaderProgram3DColor, _s3dTextureParam, _s3dColorParam, _iTexture, new float4(1, 1, 1, 1));
            _object3DList.Add(Cube3D_1);
            var Cube3D_2 = new Object3D(RC, new float3(1, 0, -11), new float3(0, (float)Math.PI / 4, (float)Math.PI / 4), _meshCube, new float3(0.01f, 0.01f, 0.01f), 0.01f);
            Cube3D_2.SimpleTextureMaterial(_shaderProgram3DColor, _s3dTextureParam, _s3dColorParam, _iTexture, new float4(1, 1, 1, 1));
            _object3DList.Add(Cube3D_2);
            var Cube3D_3 = new Object3D(RC, new float3(-5, 0, -9), new float3(0, (float)Math.PI / 4, 0), _meshCube, new float3(0.01f, 0.01f, 0.01f), 0.01f);
            Cube3D_3.SimpleTextureMaterial(_shaderProgram3DColor, _s3dTextureParam, _s3dColorParam, _iTexture, new float4(1, 1, 1, 1));
            _object3DList.Add(Cube3D_3);

        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            Update();

            var mtxRot = float4x4.CreateRotationX(_angleVert)*float4x4.CreateRotationY(_angleHorz);
            var mtxMov = float4x4.CreateTranslation(_move);
            RenderS3D(mtxMov*mtxRot);
            
            Present();
        }



        private void RenderS3D(float4x4 mtx)
        {
           
            // 3d mode
            var eyeF = new float3(0, 0, 0);
            var targetF = new float3(0, 0,-50);
            var upF = new float3(0, 1, 0);

            _stereoCameraRig.Prepare(Stereo3DEye.Left);
            for (var x = 0; x < 2; x++)
            {
                var lookAt = _stereoCameraRig.LookAt3D(_stereoCameraRig.CurrentEye, eyeF, targetF, upF);

                
                //if (_stereoCameraRig.CurrentEye == Stereo3DEye.Left)
                //{
                //    RC.SetShader(_shaderProgram3DColor);
                //    RC.SetShaderParam(_s3dColorParam, new float4(new float3(0.5f, 0.5f, 0.5f), 1));
                //    RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                //    RC.ModelView = lookAt * mtx * float4x4.CreateTranslation(new float3(5, 0,-25)) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(new float3(0.64f * 7, 0.48f * 7, 1f));
                //    RC.Render(_screenS3Ds[0].ScreenMesh);
                //}
                //else
                //{
                //    RC.SetShader(_shaderProgram3DColor);
                //    RC.SetShaderParam(_s3dColorParam, new float4(new float3(0.5f, 0.5f, 0.5f), 1));
                //    RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                //    RC.ModelView = lookAt * mtx * float4x4.CreateTranslation(new float3(5, 0, -25)) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(new float3(0.64f * 7, 0.48f * 7, 1f));
                //    RC.Render(_screenS3Ds[0].ScreenMesh);
               // }
               
                foreach (var screen in _screenS3Ds)
                {
                    screen.Render3DScreen(_stereoCameraRig, lookAt*mtx);
                }

                foreach (var obj3d in _object3DList)
                {
                    obj3d.Render(lookAt * mtx);
                }


                _stereoCameraRig.Save();

                if (x == 0)
                {
                    _stereoCameraRig.Prepare(Stereo3DEye.Right);
                }
            }
            
            _stereoCameraRig.Display();
          
        }


        private void Update()
        {
            foreach (var screen in _screenS3Ds)
            {
                screen.Update();
            }

            ReadUserInput();

        }

        private void ReadUserInput()
        {

            // move per mouse
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _angleVelHorz = -_rotationSpeed * Input.Instance.GetAxis(InputAxis.MouseX);
                _angleVelVert = -_rotationSpeed * Input.Instance.GetAxis(InputAxis.MouseY);
            }
            else
            {
                var curDamp = (float)Math.Exp(-_damping * Time.Instance.DeltaTime);
                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            //Esc -> close Application
            if (Input.Instance.IsKey(KeyCodes.Escape))
            {
                CloseGameWindow();
            }
            // move cam with arrows
            if (Input.Instance.IsKey(KeyCodes.Left))
                _move.x += 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Right))
                _move.x -= 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Up))
                _move.z += 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Down))
                _move.z -= 0.1f;


            if (Input.Instance.IsKeyUp(KeyCodes.Tab))
            {
                if (_screenS3Ds.Count-1 > _selectedScreen)
                {
                    _selectedScreen ++;
                }
                else
                {
                    _selectedScreen=0;
                }
            }
            _screenS3Ds[_selectedScreen].SetHit();
            _screenS3Ds[_selectedScreen].SetDepthScale();
            _screenS3Ds[_selectedScreen].SetPosition();
           
            
        }

        // is called when the window was resized
        public override void Resize()
        {
            Console.WriteLine("Resize1: " + Width + " " + Height);
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width/(float) Height;
            _stereoCameraRig.UpdateOnResize(Width,Height);
             _stereoCameraRig.SetFrustums(RC, MathHelper.PiOver4, aspectRatio, 5, 150, 25);
        }

        public static void Main()
        {
            var app = new DepthVideo();
            app.Run();
           
        }
    }
}
