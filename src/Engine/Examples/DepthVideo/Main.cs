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
                
                //float ndcDepth = (clip.z/clip.w);
                //float coordZ = (1-0)*0.5*ndcDepth+(gl_DepthRange.far-gl_DepthRange.near)*0.5; 
                //vec4 temp = (1,1,1,1);
                ////if(gl_FragCoord.z == coordZ)
                ////{
                ////    temp = vec4(0,1,0,1);
                ////}
                //gl_FragDepth =  coordZ;//gl_FragCoord.z;  
                gl_FragColor = dot(vColor, vec4(0, 0, 0, 1)) * colTex * dot(vNormal, vec3(0, 0, -1));
               
                
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

        #region custom depth shader

        private const string VsDepthCustom = @"
            #ifdef GL_ES
                precision mediump float;
            #endif

            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;

            varying vec3 vNormal;
            varying vec2 vUV;
            
            uniform mat4 FUSEE_MVP;
            uniform mat4 FUSEE_ITMV;      
            uniform mat4 FUSEE_P; 
            uniform mat4 FUSEE_MV;        
            
            varying vec4 pos;
            void main(){
    
                vUV = fuUV;
                pos = FUSEE_MV[3];
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
            }";

        private const string PsDepthCustom = @"
            #ifdef GL_ES
                precision mediump float;
            #endif

            uniform sampler2D textureColor, textureDepth;
            uniform float scale;
            varying vec3 vNormal;
            varying vec2 vUV;
            float zFar = 10;
            float zNear =1;
            varying vec4 pos;
            uniform mat4 FUSEE_P;         
            
            
            float linDepthToZ(float d)
            {
                return (2*zFar*zNear)/(-d*zFar+d*zNear+zFar+zNear);
            }
            float depthToZ(float din)
            {
                float d= (din-0.5)*2;
                return (2*zFar*zNear)/(-d*zFar+d*zNear+zFar+zNear);

             }

            void main(){                               
           
  
                
                //get gray depthvalue form depth texture
                float depthTexValue =1-texture2D(textureDepth, vUV).r;               
//                float z = linDepthToZ(depthTexValue);
//            
//                vec4 pTemp = pos;
//                pTemp.z += linDepthToZ(depthTexValue);
//                vec4 clipPos = FUSEE_P * vec4(pTemp.xyz, 1.0);
//                float ndcDepth = clipPos.z / clipPos.w;
//                gl_FragDepth = ((9 * ndcDepth) +1 + 10) / 2.0;
//    
                float z3 = depthToZ(gl_FragCoord.z);
                float DEPTH = (depthTexValue*2)-1;
                vec4 col;
                if(depthTexValue <=0.5)
                {
                    col = vec4(0,1,0,1);
                }
                else
                {
                    col = vec4(1,0,0,1);
                }
                gl_FragColor = texture2D(textureColor, vUV)*col;         
            
                //gl_FragDepth = gl_FragCoord.z + (depthTexValue/(log(10*24 -1)));
               // gl_FragDepth= gl_FragCoord.z + ((depthTexValue+0.5)/(log(10*24 -1)));    
                if(depthTexValue == 1)          
                {
                    gl_FragDepth = 1;
                }
                else
                {

                    gl_FragDepth = gl_FragCoord.z + (depthTexValue-(log(zFar*24 -1)*(pos.z/zFar)))*scale*0.01;
                }
            }";

        #endregion

        #region colordepth

        private const string VsDrawDepth = @"
            #ifdef GL_ES
                precision mediump float;
            #endif

            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;

            varying vec3 vNormal;
            varying vec2 vUV;
            
            uniform mat4 FUSEE_MVP;

            void main(){
    
               gl_Position = (FUSEE_MVP)* vec4(fuVertex, 1.0);
 
 
            }";

        private const string PsDrawDepth = @"
            #ifdef GL_ES
                precision mediump float;
            #endif
            
            void main(){
                
            float temp = gl_FragCoord.z;
            vec4 col;
            if(temp >0)
            {
                col = vec4(0,1,0,1);
            }
            if(temp >0.5)
            {
                col = vec4(0,0,1,1);
            }

            gl_FragColor = col;
            }";

        #endregion

        private Mesh _meshCube, _meshSphere, _meshTeapot;


        private float3 _move = float3.Zero;
        private float3 test = new float3(0,0,-25);
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



        private ScreenS3D _screenS3D, _screenS3D_1, _screenS3D_2;

        private StereoCameraRig _stereoCameraRig;
        //private Stereo3D _stereoCameraRig;

        private List<Object3D> _object3DList = new List<Object3D>();
        private List<ScreenS3D> _screenS3Ds = new List<ScreenS3D>(); 

        // is called on startup
        public override void Init()
        {

            Console.WriteLine("Init");
            RC.ClearColor = new float4(1f, 1f, 1f, 1);


            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            _meshTeapot = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");
            _meshSphere = MeshReader.LoadMesh(@"Assets/Sphere.obj.model");

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
            _stereoCameraRig = new StereoCameraRig(Stereo3DMode.Anaglyph, Width, Height, 0.63f);
            _stereoCameraRig.AttachToContext(RC);



            var videoConfigs =VideoConfigParser.ParseConfigs("Assets");
            //Set up screen object
            foreach (var config in videoConfigs)
            {
                _screenS3Ds.Add(new ScreenS3D(RC, _stereoCameraRig, new float3(0, 0, -25), config));
            }
            //_screenS3D = new ScreenS3D(RC, _stereoCameraRig, new float3(0,0,-25));
            //_screenS3D.SetVideo("Assets/left.mkv", "Assets/right.mkv", "Assets/depthLeft.mkv", "Assets/depthRight.mkv", 300);
            //_screenS3Ds.Add(_screenS3D);
            //_screenS3D_1 = new ScreenS3D(RC, _stereoCameraRig, new float3(-5, 0, -30));
            //_screenS3D_1.SetVideo("Assets/left.mkv", "Assets/right.mkv", "Assets/depthLeft.mkv", "Assets/depthRight.mkv", 300);
            //_screenS3D_2 = new ScreenS3D(RC, _stereoCameraRig, new float3(5, 0, -20));
            //_screenS3D_2.SetVideo("Assets/left.mkv", "Assets/right.mkv", "Assets/depthLeft.mkv", "Assets/depthRight.mkv", 300);

            //Create Objects3D
            var Cube3D_1 = new Object3D(RC, new float3(0, 0, -50), new float3((float)Math.PI / 4, (float)Math.PI / 4, 0), _meshCube, 0.01f, 0.01f);
            Cube3D_1.SimpleTextureMaterial(_shaderProgram3DColor, _s3dTextureParam, _s3dColorParam, _iTexture, new float4(1, 1, 1, 1));
            _object3DList.Add(Cube3D_1);
            var Cube3D_2 = new Object3D(RC, new float3(0, 0, -30), new float3(0, (float)Math.PI / 4, 0),  _meshCube, 0.01f, 0.01f);
            Cube3D_2.SimpleTextureMaterial(_shaderProgram3DColor, _s3dTextureParam, _s3dColorParam, _iTexture, new float4(1, 1, 1, 1));
            _object3DList.Add(Cube3D_2);
            var Cube3D_3 = new Object3D(RC, new float3(-5, 0, -9), new float3(0, (float)Math.PI / 4, 0),_meshCube, 0.01f, 0.01f);
            Cube3D_3.SimpleTextureMaterial(_shaderProgram3DColor, _s3dTextureParam, _s3dColorParam, _iTexture, new float4(1, 1, 1, 1));
            _object3DList.Add(Cube3D_3);

        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            //Update
            Update();

            //Esc -> close Application
            if (Input.Instance.IsKey(KeyCodes.Escape))
            {
                CloseGameWindow();
                System.Environment.Exit(-1);//Forcing VideoStreams stop!!
            }
            // move per keyboard
            if (Input.Instance.IsKey(KeyCodes.Left))
                _move.x += 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Right))
                _move.x -= 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Up))
                _move.z += 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Down))
                _move.z -= 0.1f;

            if (Input.Instance.IsKey(KeyCodes.D1))
                test.z += 0.1f;
            if (Input.Instance.IsKey(KeyCodes.D2))
                test.z -= 0.1f;

           

            // move per mouse
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _angleVelHorz = -_rotationSpeed*Input.Instance.GetAxis(InputAxis.MouseX);
                _angleVelVert = -_rotationSpeed*Input.Instance.GetAxis(InputAxis.MouseY);
            }
            else
            {
                var curDamp = (float) Math.Exp(-_damping*Time.Instance.DeltaTime);
                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;


            var mtxRot = float4x4.CreateRotationX(_angleVert)*float4x4.CreateRotationY(_angleHorz);

            if (Input.Instance.IsKey(KeyCodes.D4))
            {
                _move = new float3(0,0,-10);
            }
            if (Input.Instance.IsKey(KeyCodes.D5))
            {
                _move = new float3(0, 0, 0);
            }


            var move = float4x4.CreateTranslation(_move);



            


            //RC.SetShader(_colorShader);
            //RC.SetShaderParam(_color, new float4(0,1,0,1));
            //RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(0, 0, _cubePos.z)  * float4x4.CreateScale(0.01f);
            //RC.Render(_meshCube);



            RenderS3D(move*mtxRot);
            
            Present();
          
        }


        ITexture _iTex = null;
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

                
                if (_stereoCameraRig.CurrentEye == Stereo3DEye.Left)
                {
                    //RC.SetShader(_shaderProgram3DColor);
                    //RC.SetShaderParam(_s3dColorParam, new float4(new float3(0.5f, 0.5f, 0.5f), 1));
                    //RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                    //RC.ModelView = lookAt * mtx * float4x4.CreateTranslation(test) * float4x4.CreateRotationY((float)Math.PI)*float4x4.CreateScale(new float3(0.64f * 10, 0.48f * 10, 1f));
                    //RC.Render(_screenS3D.ScreenMesh);

                    RC.SetShader(_shaderProgram3DColor);
                    RC.SetShaderParam(_s3dColorParam, new float4(new float3(0.5f, 0.5f, 0.5f), 1));
                    RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                    RC.ModelView = lookAt * mtx * float4x4.CreateTranslation(new float3(5, 0,-25)) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(new float3(0.64f * 7, 0.48f * 7, 1f));
                    RC.Render(_screenS3Ds[0].ScreenMesh);
                }
                else
                {

                    //RC.SetShader(_shaderProgram3DColor);
                    //RC.SetShaderParam(_s3dColorParam, new float4(new float3(0.5f, 0.5f, 0.5f), 1));
                    //RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                    //RC.ModelView = lookAt * mtx * float4x4.CreateTranslation(test) * float4x4.CreateRotationY((float)Math.PI)* float4x4.CreateScale(new float3(0.64f * 10, 0.48f * 10, 1f));
                    //RC.Render(_screenS3D.ScreenMesh);

                    RC.SetShader(_shaderProgram3DColor);
                    RC.SetShaderParam(_s3dColorParam, new float4(new float3(0.5f, 0.5f, 0.5f), 1));
                    RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                    RC.ModelView = lookAt * mtx * float4x4.CreateTranslation(new float3(5,0,-25)) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(new float3(0.64f * 7, 0.48f * 7, 1f));
                    RC.Render(_screenS3Ds[0].ScreenMesh);

                    if (Input.Instance.IsKeyDown(KeyCodes.D3))
                    {
                        Console.WriteLine("test");
                        Console.WriteLine(test);
                        //Console.WriteLine("lookAt");
                        //Console.WriteLine(lookAt);
                        //Console.WriteLine("ModelView");
                        //Console.WriteLine(RC.ModelView);
                    }
                }
               
               // _screenS3D.Render3DScreen(lookAt, mtx);
                //_screenS3D_2.Render3DScreen(mtx, lookAt);
                //_screenS3D_1.Render3DScreen(mtx, lookAt);

                //RC.SetShader(_shaderProgram3DColor);
                //RC.SetShaderParam(_s3dColorParam, new float4(new float3(1, 1, 1), 0.2f));
                //RC.SetShaderParamTexture(_s3dTextureParam, _iTex);
                //RC.ModelView = lookAt * rot * float4x4.CreateTranslation(0, 0, 0) * float4x4.CreateRotationY((float)Math.PI / 4) * float4x4.CreateRotationX((float)Math.PI / 4) * float4x4.CreateScale(0.01f);
                //RC.Render(_meshCube);

                foreach (var screen in _screenS3Ds)
                {
                    screen.Render3DScreen(lookAt, mtx);
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

            //Console.WriteLine(RC.Projection);
          
        }


        private void Update()
        {
            foreach (var screen in _screenS3Ds)
            {
                screen.Update();
            }
           // _screenS3D.Update();
            //_screenS3D_1.Update();
            //_screenS3D_2.Update();
        }

        // is called when the window was resized
        public override void Resize()
        {
            Console.WriteLine("Resize1: " + Width + " " + Height);
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width/(float) Height;
            _stereoCameraRig.UpdateOnResize(Width,Height);
           // RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 50);
             _stereoCameraRig.SetFrustums(RC, MathHelper.PiOver4, aspectRatio, 10, 80, 25);
             //RC.Projection = _stereoCameraRig.CurrentProjection;
        }

        public static void Main()
        {
            var app = new DepthVideo();
            app.Run();
           
        }
    }
}
