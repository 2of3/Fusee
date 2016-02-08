using System;
using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI.GLView;
using Fusee.Engine;
using Fusee.Math;
namespace Examples.DepthVideo
{
     struct CurrentVideoFrames
    {
        public Image<Bgr, byte> CurrentColorFrame;
        public Image<Gray, byte> CurrentDepthFrame;
    }
    [FuseeApplication(Name = "DepthVideo", Description = "Integtrating a video with depth information.")]
    public class DepthVideo : RenderCanvas
    {
        #region S3D-Shader
        // GLSL
        private const string Vs = @"
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

        private const string Ps = @"
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
        #region custom depth shader
        private const string VsDepth = @"
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

        private const string PsDepth = @"
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
        private Mesh _meshPlane = new Mesh();
        private float3 _cubePos = float3.Zero;
        private Cube c =new Cube();
        // angle variables
        private static float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;
        private const float RotationSpeed = 1f;
        private const float Damping = 0.92f;

        // variables for depth shader
        private ShaderProgram _spDepth;
        private IShaderParam _textureColorParam;
        private IShaderParam _textureDepthParam;
        private IShaderParam _textureScaleParam;
        private ITexture _iTextureColor;
        private ITexture _iTextureDepth;

        // variables to dra depthtectuer of the Scene
        private ShaderProgram _spDrawDepth;

        // variables for color shader
        private ShaderProgram _spColor;
        private IShaderParam _colorParam;
        private IShaderParam _textureParam;

        // variables for stereoshader
        private ShaderProgram _shaderProgramS3D;
        private IShaderParam _colorParamS3D;
        private IShaderParam _textureParamS3DColor;
        private IShaderParam _textureParamS3DDepth;

        // variables for the videos
        private readonly List<Image<Bgr, byte>> _framesListColorVideo = new List<Image<Bgr, byte>>();
        private readonly List<Image<Gray, byte>> _framesListDepthVideo = new List<Image<Gray, byte>>();
        private IEnumerator<Image<Bgr, byte>> _framesListColorEnumerator;
        //private IEnumerator<Image<Gray, byte>> _framesListDepthEnumerator;

        private readonly List<Image<Bgr, byte>> _framesListLeft = new List<Image<Bgr, byte>>();
        private readonly List<Image<Bgr, byte>> _framesListRight = new List<Image<Bgr, byte>>();
        private readonly List<Image<Gray, byte>> _framesListDepth = new List<Image<Gray, byte>>();
        private IEnumerator<Image<Bgr, byte>> _framesListLeftEnumerator;
        private IEnumerator<Image<Bgr, byte>> _framesListRightEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthEnumerator;


        //variables for left and right images
        private ImageData _imgDataLeft;
        private ImageData _imgDataRight;
        private ImageData _imgDataDepth;
        //private ImageData _imgDataDepthLeft;
        //private ImageData _imgDataDepthRight;

        private ITexture _iTextureLeft;
        private ITexture _iTextureRight;
        private ITexture _iTexture;
        private ITexture iTextureDepthStatic;

        //Anaglyph S3D
        private Stereo3D _stereo3D;
        private float hit;


        private CurrentVideoFrames _currentVideoFrames;
        
        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(1f, 1f, 1f, 1);

            _cubePos.z = 50;
            hit = 0;
            //Set zNear and zFar (1, 10)
            // Wodth : Hwight :-> 1280 : 720
            Resize();
           
            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            _meshTeapot = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");
            CreatePlaneMesh();

            //init shader
            _spDepth = RC.CreateShader(VsDepth, PsDepth);
            _textureColorParam = _spDepth.GetShaderParam("textureColor");
            _textureDepthParam = _spDepth.GetShaderParam("textureDepth");
            _textureScaleParam = _spDepth.GetShaderParam("scale");

            _spDrawDepth = RC.CreateShader(VsDrawDepth, PsDrawDepth);

            _spColor = Shaders.GetColorShader(RC);
            _colorParam = _spColor.GetShaderParam("color");


            //stereoshader
            _shaderProgramS3D = RC.CreateShader(Vs, Ps);
            _textureParamS3DColor = _shaderProgramS3D.GetShaderParam("vTexture");
            _colorParamS3D = _shaderProgramS3D.GetShaderParam("vColor");
            _textureParamS3DDepth = _shaderProgramS3D.GetShaderParam("textureDepth");



            //s3d render stuff
            _stereo3D = new Stereo3D(Stereo3DMode.Anaglyph, Width, Height);
            _stereo3D.AttachToContext(RC);


            //Load Videos
            //ImportVideos(_framesListColorVideo, "Assets/demoQuad.mkv", _framesListDepthVideo, "Assets/demoQuadDepth.mkv");
            //_framesListColorEnumerator = _framesListColorVideo.GetEnumerator();
            //_framesListDepthEnumerator = _framesListDepthVideo.GetEnumerator();
            Console.WriteLine(Width + " "+ Height);


            //Load S3d Videos
            ImportVideo(_framesListLeft, "Assets/VideoLeft.avi");
            ImportVideo(_framesListRight, "Assets/VideoRight.avi");
            ImportVideo(_framesListDepth, "Assets/demoQuadDepth.mkv");
            _framesListLeftEnumerator = _framesListLeft.GetEnumerator();
            _framesListRightEnumerator = _framesListRight.GetEnumerator();
            _framesListDepthEnumerator = _framesListDepth.GetEnumerator();

            _iTexture = RC.CreateTexture(RC.LoadImage("Assets/world_map.jpg"));
            //_iTextureLeft = RC.CreateTexture(RC.LoadImage("Assets/imL.png"));
            //_iTextureRight = RC.CreateTexture(RC.LoadImage("Assets/imR.png"));
            iTextureDepthStatic = RC.CreateTexture(RC.LoadImage("Assets/depthMap.png"));
        }

        private void CreatePlaneMesh()
        {
            var cube = new Cube();
            var vertecies = new []
            {
                new float3 {x = +0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = +0.5f},
                
            };

            var triangles = new ushort[] { // front face
                 0, 2, 1, 0, 3, 2

               };

            var normals = new []
            {
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(0, 0, 1)
                
               
            };
            var uVs = new [] 
            {
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0)
                
                
             };


            _meshPlane.Vertices = vertecies;
            _meshPlane.Triangles = triangles;
            _meshPlane.Normals = normals;
            _meshPlane.UVs = uVs;
        }


        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
            if (Input.Instance.IsKey(KeyCodes.Escape))
                CloseGameWindow();
            // move per keyboard
            if (Input.Instance.IsKey(KeyCodes.Left))
                _cubePos.x+=1f;

            if (Input.Instance.IsKey(KeyCodes.Right))
                _cubePos.x -= 1f;

            if (Input.Instance.IsKey(KeyCodes.Up))
                _cubePos.z-= 1f;

            if (Input.Instance.IsKey(KeyCodes.Down))
                _cubePos.z += 1f;

            //Hit
            if (Input.Instance.IsKey(KeyCodes.Add))
                 hit -= 0.001f;
            if (Input.Instance.IsKey(KeyCodes.Subtract))
                hit += 0.001f;


            // move per mouse
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _angleVelHorz = -RotationSpeed * Input.Instance.GetAxis(InputAxis.MouseX);
                _angleVelVert = -RotationSpeed * Input.Instance.GetAxis(InputAxis.MouseY);
            }
            else
            {
                var curDamp = (float)Math.Exp(-Damping * Time.Instance.DeltaTime);

                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;



            var mtxRot = float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);


            //_currentVideoFrames = GetCurrentVideoFrames();
            //CrateTextures(_currentVideoFrames);

            //Iterating over the frames List - Left
            if (!_framesListLeftEnumerator.MoveNext())
            {
                _framesListLeftEnumerator.Reset();
                _framesListLeftEnumerator.MoveNext();
            }
            var frameL = _framesListLeftEnumerator.Current;
            //Iterating over the frames List - Right
            if (!_framesListRightEnumerator.MoveNext())
            {
                _framesListRightEnumerator.Reset();
                _framesListRightEnumerator.MoveNext();
            }
            var frameR = _framesListRightEnumerator.Current;
            //Iterating over the frames List - Depth
            if (!_framesListDepthEnumerator.MoveNext())
            {
                _framesListDepthEnumerator.Reset();
                _framesListDepthEnumerator.MoveNext();
            }
            var frameD = _framesListDepthEnumerator.Current;


            //creat equivalent Fusee imagedata object for render stuff
            _imgDataRight = new ImageData();
            _imgDataRight.Width = frameR.Width;
            _imgDataRight.Height = frameR.Height;
            _imgDataRight.PixelFormat = ImagePixelFormat.RGB;
            _imgDataRight.PixelData = frameR.Bytes;
            _imgDataLeft = _imgDataRight;
            _imgDataLeft.PixelData = frameL.Bytes;
            _imgDataDepth = _imgDataRight;
            _imgDataRight.PixelFormat = ImagePixelFormat.Gray;
            _imgDataDepth.PixelData = frameD.Bytes;

            //Create textures
            //left texture
            if (_imgDataLeft.PixelData != null)
            {
                if (_iTextureLeft == null)
                    _iTextureLeft = RC.CreateTexture(_imgDataLeft);

                RC.UpdateTextureRegion(_iTextureLeft, _imgDataLeft, 0, 0, _imgDataLeft.Width, _imgDataLeft.Height);
            }
            //right texture
            if (_imgDataRight.PixelData != null)
            {
                if (_iTextureRight == null)
                    _iTextureRight = RC.CreateTexture(_imgDataRight);
                RC.UpdateTextureRegion(_iTextureRight, _imgDataRight, 0, 0, _imgDataRight.Width, _imgDataRight.Height);
            }
            //depth texture
            if (_imgDataDepth.PixelData != null)
            {
                if (_iTextureDepth == null)
                    _iTextureDepth = RC.CreateTexture(_imgDataDepth);
                RC.UpdateTextureRegion(_iTextureDepth, _imgDataDepth, 0, 0, _imgDataDepth.Width, _imgDataDepth.Height);
            }


            var mtxCam = float4x4.LookAt(0, 0, 0, 0, 0, 10, 0, 1, 0);

            // var q = new Quaternion(float3.UnitY, 180);

            //RC.SetShader(_spDepth);
            //var bbpos = RC.ModelView = mtxCam * mtxRot  * float4x4.CreateTranslation(0, 0, 4) * float4x4.CreateRotationY((float)Math.PI)* float4x4.CreateScale(2, 2, 1);
            //RC.SetShaderParamTexture(_textureColorParam, _iTextureColor);
            //RC.SetShaderParamTexture(_textureDepthParam, _iTextureDepth);
            //RC.SetShaderParam(_textureScaleParam,7f);
            //RC.Render(_meshPlane);

            //RC.SetShader(_spDrawDepth);
            //var planepos = RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(-_cubePos.x, 0, _cubePos.z) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(2, 2, 1);
            //RC.Render(_meshPlane);


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

                var renderOnly = (_stereo3D.CurrentEye == Stereo3DEye.Left);
                float b = 0.02f;
                if (_stereo3D.CurrentEye == Stereo3DEye.Left)
                {
                    
                        RC.SetShader(_shaderProgramS3D);
                        //left
                        RC.SetShaderParam(_colorParamS3D, new float4(new float3(1,1,1), 1.0f));
                        RC.SetShaderParamTexture(_textureParamS3DColor, _iTextureLeft);
                        RC.SetShaderParamTexture(_textureParamS3DDepth, iTextureDepthStatic);
                        RC.ModelView = lookAt * rot* float4x4.CreateTranslation(_cubePos.x+hit, 0, 30) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(0.64f * 10, 0.48f * 10, 1f);
                        RC.Render(_meshPlane);


                        RC.SetShader(_shaderProgramS3D);
 
                        RC.SetShaderParam(_colorParamS3D, new float4(new float3(1, 1, 1), b));
                        RC.SetShaderParamTexture(_textureParamS3DColor, _iTexture);
                        RC.SetShaderParamTexture(_textureParamS3DDepth, iTextureDepthStatic);
                        RC.ModelView = lookAt * rot * float4x4.CreateTranslation(_cubePos.x-5, 0, _cubePos.z) * float4x4.CreateRotationY((float)Math.PI/3) * float4x4.CreateScale(0.01f);
                        RC.Render(_meshCube);


                        RC.SetShader(_spColor);
                        RC.SetShaderParam(_colorParam, new float4(new float3(0, 0, 1), 1));                       
                        RC.ModelView = lookAt * rot * float4x4.CreateTranslation(0, 0, _cubePos.z) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(0.64f * 10, 0.48f * 10, 1f);
                        RC.Render(_meshPlane);
                }
                else
                {
                   
                   
                        RC.SetShader(_shaderProgramS3D);
                        //right
                        RC.SetShaderParam(_colorParamS3D, new float4(new float3(1,1,1), 1.0f));
                        RC.SetShaderParamTexture(_textureParamS3DColor, _iTextureRight);
                        RC.SetShaderParamTexture(_textureParamS3DDepth, iTextureDepthStatic);
                        RC.ModelView = lookAt  * rot * float4x4.CreateTranslation(_cubePos.x-hit, 0,30) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(0.64f * 10, 0.48f * 10, 1f);
                        RC.Render(_meshPlane);

 
                        RC.SetShader(_shaderProgramS3D);
                 
                        RC.SetShaderParam(_colorParamS3D, new float4(new float3(1, 1, 1), b));
                        RC.SetShaderParamTexture(_textureParamS3DColor, _iTexture);
                        RC.SetShaderParamTexture(_textureParamS3DDepth, iTextureDepthStatic);
                        RC.ModelView = lookAt * rot * float4x4.CreateTranslation(_cubePos.x-5, 0, _cubePos.z) * float4x4.CreateRotationY((float)Math.PI/3)*float4x4.CreateScale(0.01f);
                        RC.Render(_meshCube);

                        RC.SetShader(_spColor);
                        RC.SetShaderParam(_colorParam, new float4(new float3(0, 0, 1), 1));
                        RC.ModelView = lookAt * rot * float4x4.CreateTranslation(0, 0, _cubePos.z) * float4x4.CreateRotationY((float)Math.PI) * float4x4.CreateScale(0.64f * 10, 0.48f * 10, 1f);
                        RC.Render(_meshPlane);
                }


                _stereo3D.Save();

                if (x == 0)
                    _stereo3D.Prepare(Stereo3DEye.Right);
            }

            _stereo3D.Display();
            if (Input.Instance.IsKey(KeyCodes.P)) 
                Console.WriteLine("contollpos z : "+ _cubePos.z);
        }


        //Loads videos (color and depth)
        private void ImportVideos(List<Image<Bgr, byte>> frameListColor, string pathColorVideo, List<Image<Gray, byte>> frameListDepth, string pathDepthVideo)
        {
            Capture captureColor = new Capture(pathColorVideo);
            Capture captureDepth = new Capture(pathDepthVideo);
            var tempFrameColor = captureColor.QueryFrame().ToImage<Bgr, byte>();
            var tempFrameDepth = captureDepth.QueryFrame().ToImage<Gray, byte>();

            var framecounter = 0;
            while (tempFrameColor != null)
            {
                frameListColor.Add(tempFrameColor);
                tempFrameColor = captureColor.QueryFrame().ToImage<Bgr, byte>();
                framecounter++;
                if (framecounter >= 150)
                {
                    break;
                }
            }

            framecounter = 0;
            while (tempFrameDepth != null)
            {
                frameListDepth.Add(tempFrameDepth);
                tempFrameDepth = captureDepth.QueryFrame().ToImage<Gray, byte>();
                var tempnorm = tempFrameDepth;
                framecounter++;
                if (framecounter >= 150)
                {
                    break;
                }
            }
        }


        /// <summary>
        /// Imports video file and stores frames into a List
        /// </summary>
        /// <param name="frameList"></param>
        /// <param name="path"></param>
        private void ImportVideo(List<Image<Bgr, byte>> frameList, string path)
        {
            Capture capture = new Capture(path);

            var tempFrame = capture.QueryFrame().ToImage<Bgr, byte>();

            var framecounter = 0;
            while (tempFrame != null)
            {
                frameList.Add(tempFrame);
                tempFrame = capture.QueryFrame().ToImage<Bgr, byte>();
                framecounter++;
                if (framecounter >= 150)
                {
                    break;
                }
            }
            capture.Dispose();
            //_framesListEnumerator = frameList.GetEnumerator();
        }
        private void ImportVideo(List<Image<Gray, byte>> frameList, string path)
        {
            Capture captureDepth = new Capture(path);

            var tempFrame = captureDepth.QueryFrame().ToImage<Gray, byte>();
            var framecounter = 0;
            while (frameList != null)
            {
                frameList.Add(tempFrame);
                tempFrame = captureDepth.QueryFrame().ToImage<Gray, byte>();

                framecounter++;
                if (framecounter >= 150)
                {
                    break;
                }
            }
            captureDepth.Dispose();

        }

        // looping videos and returning  current fram of color and depth video
        private CurrentVideoFrames GetCurrentVideoFrames()
        {
            CurrentVideoFrames cvf;
            //Iterating over the frames List of the Color Video
            if (!_framesListColorEnumerator.MoveNext())
            {
                _framesListColorEnumerator.Reset();
                _framesListColorEnumerator.MoveNext();
            }

            cvf.CurrentColorFrame = _framesListColorEnumerator.Current;

            //Iterating over the frames List of the Depth Video
            if (!_framesListDepthEnumerator.MoveNext())
            {
                _framesListDepthEnumerator.Reset();
                _framesListDepthEnumerator.MoveNext();
            }
            cvf.CurrentDepthFrame = _framesListDepthEnumerator.Current;
            return cvf;
        }


        //resate ITexture from Video frames
        private void CrateTextures(CurrentVideoFrames cvf)
        {
            var imgDataColor = new ImageData();
            imgDataColor.Width = cvf.CurrentColorFrame.Width;
            imgDataColor.Height = cvf.CurrentColorFrame.Height;
            imgDataColor.PixelFormat = ImagePixelFormat.RGB;
            imgDataColor.PixelData = cvf.CurrentColorFrame.Bytes;

            var imgDataDepth = new ImageData();
            imgDataDepth.Width = cvf.CurrentDepthFrame.Width;
            imgDataDepth.Height = cvf.CurrentDepthFrame.Height;
            imgDataDepth.PixelFormat = ImagePixelFormat.Gray;
            imgDataDepth.PixelData = cvf.CurrentDepthFrame.Bytes;

            //color texture
            if (imgDataColor.PixelData != null)
            {
                if (_iTextureColor == null)
                    _iTextureColor = RC.CreateTexture(imgDataColor);

                RC.UpdateTextureRegion(_iTextureColor, imgDataColor, 0, 0, imgDataColor.Width, imgDataColor.Height);
            }
            //depth texture
            if (imgDataDepth.PixelData != null)
            {
                if (_iTextureDepth == null)
                    _iTextureDepth = RC.CreateTexture(imgDataDepth);

                RC.UpdateTextureRegion(_iTextureDepth, imgDataDepth, 0, 0, imgDataDepth.Width, imgDataDepth.Height);
            }
        }



        // is called when the window was resized
        public override void Resize()
        {
            
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;

            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 100);
        }

        public static void Main()
        {
            var app = new DepthVideo();
            app.Run();
        }
    }
}
