using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{
   

    

    public class ScreenS3D : IDisposable
    {

        #region S3D-Shader + Depth
        // GLSL
        private const string VsS3dDepth = @"
            attribute vec4 fuColor;
            attribute vec3 fuVertex;
            attribute vec3 fuNormal;
            attribute vec2 fuUV;        

            varying vec3 vNormal;
            varying vec2 vUV;
            varying vec4 FuVertex;

            uniform mat4 FUSEE_MV;
            uniform mat4 FUSEE_P;
            uniform mat4 FUSEE_ITMV;
            uniform mat4 FUSEE_MVP;
            uniform mat4 FUSEE_IMV;

            void main()
            {               
               
                gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);                
                
                FuVertex = vec4(fuVertex, 1.0);              
                
                vNormal = mat3(FUSEE_ITMV[0].xyz, FUSEE_ITMV[1].xyz, FUSEE_ITMV[2].xyz) * fuNormal;
                vUV = fuUV;
            }";

        private const string PsS3dDepth = @"
            #ifdef GL_ES
                precision highp float;
            #endif
           
            //SahderParams
            uniform sampler2D vTexture;
            uniform sampler2D textureDepth;
            uniform float scale;
            uniform int invert;
            uniform mat4 FUSEE_MV;
            uniform mat4 FUSEE_P;
            uniform mat4 FUSEE_MVP;
            varying vec3 vNormal;
            varying vec2 vUV;
            varying vec4 FuVertex;
            float coordZ;
         
            void main()
            {
                //Read Texture Value (RGB)
                vec4 colTex = texture2D(vTexture, vUV);    
                //Read Texture Value (Grey/Depth)                  
                float depthTexValue = (texture(textureDepth, vUV));
                if(invert == 1)
                {
                    depthTexValue = 1- depthTexValue;
                }   
                //homogenous vertex coordinates               
                vec4 vertex = FuVertex;             
    
                if(depthTexValue >0.9)          
                {                   
                    ////ClipSpce
                    //vec4 clip = FUSEE_P*FUSEE_MV*FuVertex;
                    ////Noramlized Device Coordinates
                    //float ndcDepth = (clip.z/clip.w);  
                    ////Fragment Depth Value
                    //float coordZ = (gl_DepthRange.far-gl_DepthRange.near)*0.5*ndcDepth+(gl_DepthRange.far-gl_DepthRange.near)*0.5; 
                    //gl_FragDepth =  gl_FragCoord.z;  
                    discard;
                }
                else
                {          
                    //Add offest from 'textureDepth' with scaling value;               
                    vertex.z += ((depthTexValue*2)-1)*scale;
                    //trnasform to ClipSpace 
                    vec4 clip = FUSEE_P*FUSEE_MV*vertex;                     
                    //Noramlized Device Coordinates   
                    float ndcDepth = (clip.z/clip.w);                    
                    //Viewport transformation
                    coordZ  = (gl_DepthRange.diff)*0.5*ndcDepth+(gl_DepthRange.diff)*0.5; 
                    //Fragment Depth Value

                    gl_FragDepth =  coordZ;              
                }
                //write color 
                gl_FragColor = colTex /** dot(vNormal, vec3(0, 0, -1))*/;                            
            }";

        #endregion

        private bool disposed;
        private readonly RenderContext _rc;
        private ShaderProgram _stereo3DShaderProgram;
        private IShaderParam _colorShaderParam;
        private IShaderParam _colorTextureShaderParam;
        private IShaderParam _depthTextureShaderParam;
        private IShaderParam _depthShaderParamScale;
        private IShaderParam _invertDepthShaderParam;


        public Mesh ScreenMesh;
        public float3 Position { get; set; }
        private float3 ScaleFactor { get; set; }

        private struct VideoFrames
        {
            public ImageData ImgDataLeft;
            public ImageData ImgDataRight;
            public ImageData ImgDataDepthLeft;
            public ImageData ImgDataDepthRight;
        }

        private readonly List<Image<Rgb, byte>> _framesListLeft = new List<Image<Rgb, byte>>();
        private readonly List<Image<Rgb, byte>> _framesListRight = new List<Image<Rgb, byte>>();
        private readonly List<Image<Gray, byte>> _framesListDepthLeft = new List<Image<Gray, byte>>();
        private readonly List<Image<Gray, byte>> _framesListDepthRight = new List<Image<Gray, byte>>();


        private IEnumerator<Image<Rgb, byte>> _framesListLeftEnumerator;
        private IEnumerator<Image<Rgb, byte>> _framesListRightEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthLeftEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthRightEnumerator;


        private readonly List<ITexture> _iTexturesListLeft = new List<ITexture>();
        private readonly List<ITexture> _iTexturesListRight = new List<ITexture>();
        private readonly List<ITexture> _iTexturesListDepthLeft = new List<ITexture>();
        private readonly List<ITexture> _iTexturesListDepthRight = new List<ITexture>();

        private IEnumerator<ITexture> _iTexturesListLeftEnumerator;
        private IEnumerator<ITexture> _iTexturesListRightEnumerator;
        private IEnumerator<ITexture> _iTexturesListDepthLeftEnumerator;
        private IEnumerator<ITexture> _iTexturesListDepthRightEnumerator;

        private struct CurrentVideoTextrures
        {
            public ITexture TextureLeft;
            public ITexture TextureRight;
            public ITexture TextureDepthLeft;
            public ITexture TextureDepthRight;
        }

        private CurrentVideoTextrures CurrentVideoTextures { get; set; }
        private VideoConfig _config;


        public ScreenS3D(RenderContext rc, VideoConfig videoConfig)
        {
            _rc = rc;
            _config = videoConfig;
            SetVideo(_config.VideoDirectory + "/" + _config.LeftVideoRgb,
                _config.VideoDirectory +"/"+ _config.RightVideoRgb,
                _config.VideoDirectory + "/" + _config.LeftVideoDepth,
                _config.VideoDirectory + "/" + _config.RightVideoDepth, _config.FrameCount);
            InitializeShader();
            ScreenMesh = CreatePlaneMesh();
            ScaleFactor = new float3(_framesListLeft[0].Width * _config.ScalePlane, _framesListLeft[0].Height * _config.ScalePlane, 1f);
        }
        
        private void InitializeShader()
        {
            _stereo3DShaderProgram = _rc.CreateShader(VsS3dDepth, PsS3dDepth);
           // _colorShaderParam = _stereo3DShaderProgram.GetShaderParam("vColor");
            _colorTextureShaderParam = _stereo3DShaderProgram.GetShaderParam("vTexture");
            _depthTextureShaderParam = _stereo3DShaderProgram.GetShaderParam("textureDepth");
            _depthShaderParamScale = _stereo3DShaderProgram.GetShaderParam("scale");
            _invertDepthShaderParam = _stereo3DShaderProgram.GetShaderParam("invert");
        }

        /// <summary>
        /// Creates the Mesh where the Videos are maped on as a texture
        /// </summary>
        private Mesh CreatePlaneMesh()
        {
            var mesh = new Mesh();
            var vertecies = new[]
            {
                new float3 {x = +0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = +0.5f}
            };

            var triangles = new ushort[]
            {
                // front face
                1,2,0,2,3,0
            };

            var normals = new[]
            {
                new float3(0, 0, -1),
                new float3(0, 0, -1),
                new float3(0, 0, -1),
                new float3(0, 0, -1)
            };
            var uVs = new[]
            {

                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0)
            };

            mesh.Vertices = vertecies;
            mesh.Triangles = triangles;
            mesh.Normals = normals;
            mesh.UVs = uVs;
            return mesh;
        }


        /// <summary>
        /// Imort the videos to the ScreenS3D object
        /// </summary>
        /// <param name="pathLeftVideo">Path to left Video</param>
        /// <param name="pathRightVideo">Path to right Video</param>
        /// <param name="pathDepthVideLeft">Path to depth Video (left)</param>
        /// <param name="pathDepthVideRight">Path to depth Video (right)</param>
        /// <param name="videoLength">Length of the videos in frames. (All three videos must have the same amount of frames and recorded with the same frame rate)</param>
        public void SetVideo(string pathLeftVideo, string pathRightVideo, string pathDepthVideLeft, string pathDepthVideRight, int videoLength)
        {
            ImportVideo(_framesListLeft, pathLeftVideo, ref _framesListLeftEnumerator, videoLength);
            ImportVideo(_framesListRight, pathRightVideo, ref _framesListRightEnumerator, videoLength);
            ImportVideo(_framesListDepthLeft, pathDepthVideLeft, ref _framesListDepthLeftEnumerator, videoLength);
            ImportVideo(_framesListDepthRight, pathDepthVideRight, ref _framesListDepthRightEnumerator, videoLength);
            for (int i = 0; i < _framesListLeft.Count; i++)
            {
                var videoFrames = GetVideoFrames();
                CreateVideoTexture(videoFrames);
            }
            _iTexturesListLeftEnumerator = _iTexturesListLeft.GetEnumerator();
            _iTexturesListRightEnumerator = _iTexturesListRight.GetEnumerator();
            _iTexturesListDepthLeftEnumerator = _iTexturesListDepthLeft.GetEnumerator();
            _iTexturesListDepthRightEnumerator = _iTexturesListDepthRight.GetEnumerator();
        }

        /// <summary>
        ///  Imports video (Color) file and stores frames into a List
        /// </summary>
        /// <param name="frameList"></param>
        /// <param name="path"></param>
        /// <param name="frameListEnumerator"></param>
        /// <param name="videoLength"></param>
        private void ImportVideo(List<Image<Rgb, byte>> frameList, string path, ref IEnumerator<Image<Rgb, byte>> frameListEnumerator, int videoLength)
        {
            using (Capture capture = new Capture(path))
            {
                var tempFrame = capture.QueryFrame().ToImage<Rgb, byte>();
                var framecounter = 0;
                while (tempFrame != null)
                {
                    frameList.Add(tempFrame);
                    tempFrame = capture.QueryFrame().ToImage<Rgb, byte>();
                    framecounter++;
                    if (framecounter >= videoLength)
                    {
                        break;
                    }
                }
                frameListEnumerator = frameList.GetEnumerator();
            }
        }

        /// <summary>
        ///  Imports video (gray) file and stores frames into a List
        /// </summary>
        /// <param name="frameList"></param>
        /// <param name="path"></param>
        /// <param name="frameListEnumerator"></param>
        /// <param name="videoLength"></param>
        private void ImportVideo(List<Image<Gray, byte>> frameList, string path, ref IEnumerator<Image<Gray, byte>> frameListEnumerator, int videoLength)
        {
            using (Capture captureDepth = new Capture(path))
            {
                var tempFrame = captureDepth.QueryFrame().ToImage<Gray, byte>();
                var framecounter = 0;
                while (tempFrame != null)
                {
                    frameList.Add(tempFrame);
                    tempFrame = captureDepth.QueryFrame().ToImage<Gray, byte>();
                    framecounter++;
                    if (framecounter >= videoLength)
                    {
                        break;
                    }
                }
                frameListEnumerator = frameList.GetEnumerator();
            }
        }



        /// <summary>
        /// GetVideoFrames
        /// Reading all frames from the videos, storing them as ImageData as preperation to store them as ITexture
        /// </summary>
        /// <returns>VideoFrames</returns>
        private VideoFrames GetVideoFrames()
        {
            VideoFrames vf = new VideoFrames();
            //Iterating over the frames List - Left (Color)
            if (!_framesListLeftEnumerator.MoveNext())
            {
                _framesListLeftEnumerator.Reset();
                _framesListLeftEnumerator.MoveNext();
            }
            var imgDataLeft = new ImageData();
            //fits also right, and depth images -> read only once- applies to all
            imgDataLeft.Width = _framesListLeftEnumerator.Current.Width;
            imgDataLeft.Height = _framesListLeftEnumerator.Current.Height;
            imgDataLeft.PixelFormat = ImagePixelFormat.RGB;
            imgDataLeft.PixelData = _framesListLeftEnumerator.Current.Bytes;
            vf.ImgDataLeft = imgDataLeft;

            //Iterating over the frames List - Right (Color)
            if (!_framesListRightEnumerator.MoveNext())
            {
                _framesListRightEnumerator.Reset();
                _framesListRightEnumerator.MoveNext();
            }
            var imgDataRight = imgDataLeft;
            imgDataRight.PixelData = _framesListRightEnumerator.Current.Bytes;
            vf.ImgDataRight = imgDataRight;

            //Iterating over the frames List - Depth Left
            if (!_framesListDepthLeftEnumerator.MoveNext())
            {
                _framesListDepthLeftEnumerator.Reset();
                _framesListDepthLeftEnumerator.MoveNext();
            }
            var imgDataDepthLeft = imgDataLeft;
            imgDataDepthLeft.PixelFormat = ImagePixelFormat.Gray;
            imgDataDepthLeft.PixelData = _framesListDepthLeftEnumerator.Current.Bytes;
            vf.ImgDataDepthLeft = imgDataDepthLeft;


            //Iterating over the frames List - Depth Right
            if (!_framesListDepthRightEnumerator.MoveNext())
            {
                _framesListDepthRightEnumerator.Reset();
                _framesListDepthRightEnumerator.MoveNext();
            }
            var imgDataDepthRight= imgDataLeft;
            imgDataDepthRight.PixelFormat = ImagePixelFormat.Gray;
            imgDataDepthRight.PixelData = _framesListDepthRightEnumerator.Current.Bytes;
            vf.ImgDataDepthRight = imgDataDepthRight;

            return vf;
        }




        /// <summary>
        /// Stores all four Itextures in an object of type CurrentVideoTextrures
        /// </summary>
        /// <returns> ITexture of the current frame </returns>
        private CurrentVideoTextrures GetCurrentVideoTextures()
        { 
            CurrentVideoTextrures cvt;
            //Iterating over the frames List - Right (Color)
            if (!_iTexturesListLeftEnumerator.MoveNext())
            {
                _iTexturesListLeftEnumerator.Reset();
                _iTexturesListLeftEnumerator.MoveNext();
            }
            cvt.TextureLeft = _iTexturesListLeftEnumerator.Current;

            //Iterating over the frames List - Right (Color)
            if (!_iTexturesListRightEnumerator.MoveNext())
            {
                _iTexturesListRightEnumerator.Reset();
                _iTexturesListRightEnumerator.MoveNext();
            }
            cvt.TextureRight = _iTexturesListRightEnumerator.Current;

            //Iterating over the frames List - Depth Left
            if (!_iTexturesListDepthLeftEnumerator.MoveNext())
            {
                _iTexturesListDepthLeftEnumerator.Reset();
                _iTexturesListDepthLeftEnumerator.MoveNext();
            }
            cvt.TextureDepthLeft = _iTexturesListDepthLeftEnumerator.Current;

            //Iterating over the frames List - Depth Right
            if (!_iTexturesListDepthRightEnumerator.MoveNext())
            {
                _iTexturesListDepthRightEnumerator.Reset();
                _iTexturesListDepthRightEnumerator.MoveNext();
            }
            cvt.TextureDepthRight = _iTexturesListDepthRightEnumerator.Current;
            
            return cvt;
        }

      
        /// <summary>
        /// Creates Textures from ImageData and stores them in a list
        /// </summary>
        /// <param name="vf"></param>
        private void CreateVideoTexture(VideoFrames vf)
        {
            //iTexture left
            if (vf.ImgDataLeft.PixelData != null)
            {
                var left = _rc.CreateTexture(vf.ImgDataLeft);
                _iTexturesListLeft.Add(left);
            }

            //iTexture right
            if (vf.ImgDataRight.PixelData != null)
            {
                var right = _rc.CreateTexture(vf.ImgDataRight);
                _iTexturesListRight.Add(right);
            }

            //depth texture left
            if (vf.ImgDataDepthLeft.PixelData != null)
            {
                var depthleft = _rc.CreateTexture(vf.ImgDataDepthLeft);
                _iTexturesListDepthLeft.Add(depthleft);
            }

            //depth texture right
            if (vf.ImgDataDepthRight.PixelData != null)
            {
                var depthright = _rc.CreateTexture(vf.ImgDataDepthRight);
                _iTexturesListDepthRight.Add(depthright);
            }
        }


        public void Update()
        {
            //preloaded videos
            CurrentVideoTextures = GetCurrentVideoTextures();
           

           
        }

        public void SetPosition()
        {
            if (Input.Instance.IsKey(KeyCodes.A))
                _config.PositionX += 0.5f;
            if (Input.Instance.IsKey(KeyCodes.D))
                _config.PositionX += -0.5f;
            if (Input.Instance.IsKey(KeyCodes.W))
                _config.PositionZ += 0.5f;
            if (Input.Instance.IsKey(KeyCodes.S))
                _config.PositionZ += -0.5f;

 
            if (Input.Instance.IsKeyUp(KeyCodes.P))
            {
                VideoConfigParser.WriteConfigToDisk(_config);
            }
        }

        public void SetHit()
        {
            if (Input.Instance.IsKey(KeyCodes.Add))
                _config.Hit += 0.01f;
            if (Input.Instance.IsKey(KeyCodes.Subtract))
                _config.Hit -= 0.01f;
        
            if (Input.Instance.IsKeyUp(KeyCodes.H))
            {
               
                VideoConfigParser.WriteConfigToDisk(_config);
            }
        }

        public void SetDepthScale()
        {
            if (Input.Instance.IsKey(KeyCodes.N))
                _config.DepthScale += 0.5f;
            if (Input.Instance.IsKey(KeyCodes.M)&&_config.DepthScale>0)
                _config.DepthScale -= 0.5f;

            if (Input.Instance.IsKeyUp(KeyCodes.Enter))
            {
                Console.WriteLine("Saved Depth");
                VideoConfigParser.WriteConfigToDisk(_config);
            }
        }

        public void Render3DScreen(StereoCameraRig cameraRig, float4x4 mtx)
        {
            float hit = 0;
            ITexture textureColor = null;
            ITexture textureDepth = null; 
            switch (cameraRig.CurrentEye)
            {
                case Stereo3DEye.Left:
                    textureColor = CurrentVideoTextures.TextureLeft;
                    textureDepth = CurrentVideoTextures.TextureDepthLeft;
                    hit = (-_config.Hit/2);
                    break;
                case Stereo3DEye.Right:
                    textureColor =  CurrentVideoTextures.TextureRight;
                    textureDepth = CurrentVideoTextures.TextureDepthRight;
                    hit = (_config.Hit/2);
                    break;
            }

            if (textureColor != null && textureDepth != null)
            {
                _rc.SetShader(_stereo3DShaderProgram);
                //_rc.SetShaderParam(_colorShaderParam, new float4(new float3(1, 1, 1), 1));
                _rc.SetShaderParamTexture(_colorTextureShaderParam, textureColor);
                _rc.SetShaderParamTexture(_depthTextureShaderParam, textureDepth);
                _rc.SetShaderParam(_depthShaderParamScale, _config.DepthScale);
                _rc.SetShaderParam(_invertDepthShaderParam, 1);
                var mv =mtx *float4x4.CreateTranslation(_config.PositionX, _config.PositionY, _config.PositionZ) * float4x4.CreateRotationY((float)Math.PI)* float4x4.CreateRotationZ((float)Math.PI) * float4x4.CreateTranslation(hit, 0, 0) * float4x4.CreateScale(ScaleFactor);
               
                _rc.ModelView = mv;
                _rc.Render(ScreenMesh);
            }
        }

        ~ScreenS3D()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
                
            }

            if (disposing)
            {
               // _videoStreamL.Dispose();
               // _videoStreamR.Dispose();
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
