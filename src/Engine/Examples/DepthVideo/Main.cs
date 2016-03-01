using System;
using System.Collections.Generic;
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
            varying vec4 pos;

            void main()
            {
                mat4 FUSEE_MVP = FUSEE_P * FUSEE_MV;
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
                pos = FUSEE_MV[3];
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
            varying vec4 pos;
            float zNear = 1;
            float zFar = 100;
            vec4 transparency = 1;
            void main()
            {
                vec4 colTex = vColor * texture2D(vTexture, vUV);
                // colh gl_FragColor = dot(vColor, vec4(0, 0, 0, 1)) * colTex * dot(vNormal, vec3(0, 0, 1));
                
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


            void main()
            {
                mat4 FUSEE_MVP = FUSEE_P * FUSEE_MV;
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
               
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

   

            void main()
            {
                vec4 colTex = vColor * texture2D(vTexture, vUV);       
                
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

        private Mesh _meshCube, _meshTeapot;

        private float3 _cubePos = float3.Zero;

        // angle variables
        private static float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;
        private const float RotationSpeed = 1f;
        private const float Damping = 0.92f;

        // variables for depth shader
        private ShaderProgram _spDepth;
        private IShaderParam _textureColorParam;
        private IShaderParam _textureDepthParam;
        private IShaderParam _textureScaleParam;
  

        // variables to dra depthtectuer of the Scene
        private ShaderProgram _spDrawDepth;


        // variables for stereoshader + depth
        private ShaderProgram _shaderProgramS3DDepth;

        // variables for stereoshader-> color only
        private ShaderProgram _shaderProgram3DColor;
        private IShaderParam _s3dColorParam;
        private IShaderParam _s3dTextureParam;

        //variables depth shader
        private ShaderProgram _shaderProgramDepth;

        // 2D color shader
        private ShaderProgram _colorShader;
        private IShaderParam _color;

        private ITexture _iTexture;


        //Anaglyph S3D
        private Stereo3D _stereo3D;
        private float hit;

        private ScreenS3D _screenS3D;



        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(1f, 1f, 1f, 1);

            _cubePos.z = 50;
            hit = 0;
            //Set zNear and zFar (1, 10)
            // Width : Hight :-> 1280 : 720
            Resize();

            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            _meshTeapot = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");

            //stereoshader -> color only
            _shaderProgram3DColor = RC.CreateShader(VsS3D, PsS3D);
            _s3dColorParam = _shaderProgram3DColor.GetShaderParam("vColor");
            _s3dTextureParam = _shaderProgram3DColor.GetShaderParam("vTexture");

            //stereoshader -> color+depth combo
            _shaderProgramS3DDepth = RC.CreateShader(VsS3dDepth, PsS3dDepth);

            //depthshader -> depth only
            _shaderProgramDepth = RC.CreateShader(VsDepth, PsDepth);

            //normal 2D color Shader
            _colorShader = Shaders.GetColorShader(RC);
            _color = _colorShader.GetShaderParam("color");

            //s3d render stuff
            _stereo3D = new Stereo3D(Stereo3DMode.Anaglyph, Width, Height);
            _stereo3D.AttachToContext(RC);

            //Set up screen object
            _screenS3D = new ScreenS3D(RC, _stereo3D, _shaderProgramS3DDepth, new float3(0,0,10));
            _screenS3D.SetVideo("Assets/left.mkv", "Assets/right.mkv", "Assets/depthCenter.mkv", 100);


            Console.WriteLine(Width + " " + Height);         

            _iTexture = RC.CreateTexture(RC.LoadImage("Assets/world_map.jpg"));

        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            //Update
            Update();

            //Esc -> close Application
            if (Input.Instance.IsKey(KeyCodes.Escape))
                CloseGameWindow();
            // move per keyboard
            if (Input.Instance.IsKey(KeyCodes.Left))
                _cubePos.x += 0.5f;

            if (Input.Instance.IsKey(KeyCodes.Right))
                _cubePos.x -= 0.5f;

            if (Input.Instance.IsKey(KeyCodes.Up))
                _cubePos.z -= 0.5f;

            if (Input.Instance.IsKey(KeyCodes.Down))
                _cubePos.z += 0.5f;



            if (Input.Instance.IsKey(KeyCodes.W))
                _screenS3D.Position += new float3(0,0,0.02f);

            if (Input.Instance.IsKey(KeyCodes.S))
                _screenS3D.Position += new float3(0, 0, -0.02f);

            // move per mouse
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _angleVelHorz = -RotationSpeed*Input.Instance.GetAxis(InputAxis.MouseX);
                _angleVelVert = -RotationSpeed*Input.Instance.GetAxis(InputAxis.MouseY);
            }
            else
            {
                var curDamp = (float) Math.Exp(-Damping*Time.Instance.DeltaTime);

                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;


            var mtxRot = float4x4.CreateRotationX(_angleVert)*float4x4.CreateRotationY(_angleHorz);
            var mtxCam = float4x4.LookAt(0, 0, 0, 0, 0, 100, 0, 1, 0);



            RC.SetShader(_colorShader);
            RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(-_cubePos.x, 2, _cubePos.z)  * float4x4.CreateScale(0.05f);
            RC.Render(_meshCube);


            //if (Input.Instance.IsKey(KeyCodes.P))
            //    Console.WriteLine("Billboard: " + bbpos.Column3 + "  Plane: "+ planepos.Column3);


            RenderS3D(mtxRot);
            
            Present();
            
        }

        private void RenderS3D(float4x4 rot)
        {
           
            // 3d mode
            var eyeF = new float3(0, 0, 0);
            var targetF = new float3(0, 0, 100);
            var upF = new float3(0, 1, 0);

            _stereo3D.Prepare(Stereo3DEye.Left);

            for (var x = 0; x < 2; x++)
            {
                var lookAt = _stereo3D.LookAt3D(_stereo3D.CurrentEye, eyeF, targetF, upF);
                
               // _screenS3D.RenderScreen(rot, lookAt);
                var b = 0.02f;
                if (_stereo3D.CurrentEye == Stereo3DEye.Left)
                {
                    _screenS3D.RenderLeft(rot,lookAt);
                }
                else
                {
                    _screenS3D.RenderRight(rot, lookAt);
                }


                
                RC.SetShader(_shaderProgram3DColor);
                RC.SetShaderParam(_s3dColorParam, new float4(new float3(1, 1, 1), b));
                RC.SetShaderParamTexture(_s3dTextureParam, _iTexture);
                RC.ModelView = lookAt * rot * float4x4.CreateTranslation(_cubePos.x - 5, 0, _cubePos.z) *
                               float4x4.CreateRotationY((float)Math.PI / 3) * float4x4.CreateScale(0.01f);
                RC.Render(_meshCube);


                _stereo3D.Save();

                if (x == 0)
                    _stereo3D.Prepare(Stereo3DEye.Right);
            }

            _stereo3D.Display();
            if (Input.Instance.IsKey(KeyCodes.P))
                Console.WriteLine("contollpos z : " + _cubePos.z);
        }


        private void Update()
        {
            _screenS3D.Update();
        }

        // is called when the window was resized
        public override void Resize()
        {
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width/(float) Height;

            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 100);
        }

        public static void Main()
        {
            var app = new DepthVideo();
            app.Run();
        }
    }
}
