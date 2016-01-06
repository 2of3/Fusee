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
            uniform mat4 FUSEE_MV;
            uniform mat4 FUSEE_V;
            uniform mat4 FUSEE_M;

 
            varying vec4 pos;

            void main(){
    
                vUV = fuUV;
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
                pos = vec4(FUSEE_V[3].xyz,1);
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
            }";

        private const string PsDepth = @"
            #ifdef GL_ES
                precision mediump float;
            #endif

            uniform sampler2D  textureColor, textureDepth;
            varying vec3 vNormal;
            varying vec2 vUV;
            varying vec3 fuVertex;
            uniform mat4 FUSEE_MV;
            float zFar;
            float zNear;
            vec4 cameraSpacePosition;
            varying vec4 pos;

            float zPosToLinDepth(float z)
            {
                return ((gl_DepthRange.far*gl_DepthRange.near)/(gl_DepthRange.far-gl_DepthRange.near)) + (1/z)*((-2*gl_DepthRange.far*gl_DepthRange.near)/(gl_DepthRange.far-gl_DepthRange.near));
            }
            float depthToZ(float d)
            {
                return(2*gl_DepthRange.far*gl_DepthRange.near)/(-d*gl_DepthRange.far+d*gl_DepthRange.near+gl_DepthRange.far+gl_DepthRange.near);
            }
            float nonLinDepth(float linearDepth)
            {

                float nonLinearDepth = (gl_DepthRange.far + gl_DepthRange.near - 2.0 * gl_DepthRange.near * gl_DepthRange.far / linearDepth) / (gl_DepthRange.far - gl_DepthRange.near);
                nonLinearDepth = (nonLinearDepth + 1.0) / 2.0;
                return nonLinearDepth;
            }
            float linearDepth(float nonLinearDepth)
            {
                nonLinearDepth = 2.0 * nonLinearDepth - 1.0;
                float zLinear = 2.0 * gl_DepthRange.near * gl_DepthRange.far / (gl_DepthRange.far + gl_DepthRange.near - nonLinearDepth * (gl_DepthRange.far - gl_DepthRange.near));
                return zLinear;
               
            }

            void main(){
                zNear = gl_DepthRange.near;
                zFar = gl_DepthRange.far; 
        
            
                                 
            
  
                //get nonlinear depthvalue form depth texture
                float depthTexValue = 1-texture2D(textureDepth, vUV);


                float z_n = 2.0 * depthTexValue - 1.0;
                float z_e = 2.0 * zNear * zFar / (zFar + zNear - z_n * (zFar - zNear));
                
                //convert to linera depthvalue
                float linDepth = linearDepth(depthTexValue);
              
                //calculate pos refering to linear depth value
                float depthTexZ =depthToZ(depthTexValue);
                               
                

                //difference between actual pos of the plane and the one from the depthtexture
                float dif =abs(depthTexZ-pos.z);

                vec4 col;
                if(pos.z>0)
                    col= vec4(0,1,0,1) ;
                else 
                    col= vec4(1,0,0,1);
  

                //add offest to pos and calculate  depthvallue for gl_FragDepth
                float d = nonLinDepth(zPosToLinDepth(dif+pos.z));





                gl_FragColor =texture2D(textureColor, vUV)*col;
                gl_FragDepth =d;
                

           
            }";


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
            uniform mat4 FUSEE_ITMV;
            uniform mat4 FUSEE_V;
            uniform mat4 FUSEE_P;

            varying vec4 position;

            void main(){
    
               gl_Position = (FUSEE_MVP)* vec4(fuVertex, 1.0);
               position = gl_Position;
 
            }";

        private const string PsDrawDepth = @"
            #ifdef GL_ES
                precision mediump float;
            #endif
            float zNear, zFar;
            varying vec4 position;
            void main(){
                zNear = gl_DepthRange.near;
                zFar = gl_DepthRange.far;     
                float zLin = -((2*zNear*zFar)/(zFar-zNear))*(-1/position.z)+((zFar+zNear)/(zFar-zNear));
                float zLog = (2*zFar*zNear)/(zFar*(-zLin)+zFar+zLin*zNear+zNear);
            
                //Linear Depth Value
                float ndcDepth =  (2.0 * gl_FragCoord.z - zNear - zFar) / (zFar - zNear);

                
//                if(zLin == ndcDepth)
//                    gl_FragColor= vec4(0,1,0,1) ;
//                else
//                    gl_FragColor= vec4(1,0,0,1);
                //  float clipDepth = ndcDepth / gl_FragCoord.w;
                // gl_FragColor = vec4((clipDepth * 0.5) + 0.5); 
                gl_FragColor= vec4(gl_FragCoord.w,gl_FragCoord.w,gl_FragCoord.w,1) ;

           
            }";

        #endregion
        private Mesh _meshCube, _meshSphere, _meshTeapot;
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
        private ITexture _iTextureColor;
        private ITexture _iTextureDepth;

        // variables to dra depthtectuer of the Scene
        private ShaderProgram _spDrawDepth;

        // variables for color shader
        private ShaderProgram _spColor;
        private IShaderParam _colorParam;
        private IShaderParam _textureParam;

        // variables for the videos
        private readonly List<Image<Bgr, byte>> _framesListColorVideo = new List<Image<Bgr, byte>>();
        private readonly List<Image<Gray, byte>> _framesListDepthVideo = new List<Image<Gray, byte>>();
        private IEnumerator<Image<Bgr, byte>> _framesListColorEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthEnumerator;

        private CurrentVideoFrames _currentVideoFrames;
        
        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(0.1f, 0.1f, 0.5f, 1);

            //Set zNear and zFar (1, 10)
            // Wodth : Hwight :-> 1280 : 720
            Resize();
           
            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            _meshTeapot = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");
            _meshSphere = MeshReader.LoadMesh(@"Assets/Sphere.obj.model");
            CreatePlaneMesh();

            //init shader
            _spDepth = RC.CreateShader(VsDepth, PsDepth);
            _textureColorParam = _spDepth.GetShaderParam("textureColor");
            _textureDepthParam = _spDepth.GetShaderParam("textureDepth");

            _spDrawDepth = RC.CreateShader(VsDrawDepth, PsDrawDepth);

            _spColor = Shaders.GetColorShader(RC);
            _colorParam = _spColor.GetShaderParam("color");

            //Load Videos
            ImportVideos(_framesListColorVideo, "Assets/demoFarSmall.mkv", _framesListDepthVideo, "Assets/demoFarDepthSmall.mkv");
            _framesListColorEnumerator = _framesListColorVideo.GetEnumerator();
            _framesListDepthEnumerator = _framesListDepthVideo.GetEnumerator();
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
                _cubePos.x+=0.2f;

            if (Input.Instance.IsKey(KeyCodes.Right))
                _cubePos.x -= 0.2f;

            if (Input.Instance.IsKey(KeyCodes.Up))
                _cubePos.z+= 0.1f;

            if (Input.Instance.IsKey(KeyCodes.Down))
                _cubePos.z -= 0.1f;

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


            _currentVideoFrames = GetCurrentVideoForames();
            CrateTextures(_currentVideoFrames);

     
            var mtxCam = float4x4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);

            var q = new Quaternion(float3.UnitY, 180);
            
            RC.SetShader(_spDepth);
            RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(0, 0, -5) * float4x4.CreateFromAxisAngle(float3.UnitY, (float)Math.PI);
            RC.SetShaderParamTexture(_textureColorParam, _iTextureColor);
            RC.SetShaderParamTexture(_textureDepthParam, _iTextureDepth);
            RC.Render(_meshPlane);


            RC.SetShader(_spDrawDepth);
            RC.ModelView = mtxCam*mtxRot*float4x4.CreateTranslation(-_cubePos.x, 0, _cubePos.z)* float4x4.Scale(0.01f);
            //RC.SetShaderParam(_colorParam,new float4(1,0,0,1));
            RC.Render(_meshCube);

            //RC.SetShader(_spDepth);
            //RC.ModelView = mtxCam * float4x4.Scale(0.01f) * float4x4.CreateTranslation(0, 1, -2);
            //// RC.SetShaderParamTexture(_textureColorParam, _iTextureColor);
            //RC.SetShaderParamTexture(_textureDepthParam, _iTextureDepth);
            //RC.Render(_meshCube);

            Present();
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

        // looping videos and returning  current fram of color and depth video
        private CurrentVideoFrames GetCurrentVideoForames()
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

            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 10);
        }

        public static void Main()
        {
            var app = new DepthVideo();
            app.Run();
        }
    }
}
