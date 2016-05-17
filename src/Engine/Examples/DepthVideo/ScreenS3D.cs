using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{
    internal struct VideoFrames
    {
        public Image<Bgr, byte> CurrentLeftFrame;
        public Image<Bgr, byte> CurrentRightFrame;
        public Image<Gray, byte> CurrentLeftDepthFrame;
        public Image<Gray, byte> CurrentRightDepthFrame;

        public ImageData ImgDataLeft;
        public ImageData ImgDataRight;
        public ImageData ImgDataDepthLeft;
        public ImageData ImgDataDepthRight;
    }

    

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
            uniform vec4 vColor;
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
                vec4 colTex = vColor * texture2D(vTexture, vUV);    
                //Read Texture Value (Grey/Depth)                  
                float depthTexValue = (1-texture(textureDepth, vUV));
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
                gl_FragColor =  dot(vColor, vec4(0, 0, 0, 1))  *colTex * dot(vNormal, vec3(0, 0, -1));                            
            }";

        #endregion

        private bool disposed;
        private readonly RenderContext _rc;
        private readonly Stereo3D _stereo3D;
        private ShaderProgram _stereo3DShaderProgram;
        private IShaderParam _colorShaderParam;
        private IShaderParam _colorTextureShaderParam;
        private IShaderParam _depthTextureShaderParam;
        private IShaderParam _depthShaderParamScale;


        public Mesh ScreenMesh { get; set; }
    

        private List<Image<Bgr, byte>> _framesListLeft = new List<Image<Bgr, byte>>();
        private List<Image<Bgr, byte>> _framesListRight = new List<Image<Bgr, byte>>();
        private List<Image<Gray, byte>> _framesListDepthLeft = new List<Image<Gray, byte>>();
        private List<Image<Gray, byte>> _framesListDepthRight = new List<Image<Gray, byte>>();


        private IEnumerator<Image<Bgr, byte>> _framesListLeftEnumerator;
        private IEnumerator<Image<Bgr, byte>> _framesListRightEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthLeftEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthRightEnumerator;


        private List<ITexture> _iTexturesListLeft = new List<ITexture>();
        private List<ITexture> _iTexturesListRight = new List<ITexture>();
        private List<ITexture> _iTexturesListDepthLeft = new List<ITexture>();
        private List<ITexture> _iTexturesListDepthRight = new List<ITexture>();

        private IEnumerator<ITexture> _iTexturesListLeftEnumerator;
        private IEnumerator<ITexture> _iTexturesListRightEnumerator;
        private IEnumerator<ITexture> _iTexturesListDepthLeftEnumerator;
        private IEnumerator<ITexture> _iTexturesListDepthRightEnumerator;

        private struct CurrentVideoTextrures
        {
            public ITexture ITextureLeft;
            public ITexture ITextureRight;
            public ITexture ITextureDepthLeft;
            public ITexture ITextureDepthRight;
        }

        private CurrentVideoTextrures _currentVideoTextures = new CurrentVideoTextrures();


        public ITexture _iTextureLeft;
        public ITexture _iTextureRight;
        public ITexture _iTextureDepthLeft;
        public ITexture _iTextureDepthRight;

        public float3 Position { get; set; }
        private float3 _scaleFactor;

        public float Hit { get; private set; }

        private StereoBM _stereoSolver = new StereoBM();
        private ITexture _iTexLeft, _iTexLeftDepth;
        private ITexture _iTexRight, _iTexRightDepth;

        private IVideoStreamImp _videoStreamL, _videoStreamLD, _videoStreamR, _videoStreamRD;
        private Capture captureLeft;

        public ScreenS3D(RenderContext rc, Stereo3D s3D,  float3 pos, VideoConfig videoConfig) : this(rc,s3D,pos)
        {
            SetVideo(videoConfig.VideoDirectory + "/" + videoConfig.LeftVideoRgb,
                videoConfig.VideoDirectory +"/"+ videoConfig.RightVideoRgb,
                videoConfig.VideoDirectory + "/" + videoConfig.LeftVideoDepth,
                videoConfig.VideoDirectory + "/" + videoConfig.RightVideoDepth, videoConfig.FrameCount);
            Hit = videoConfig.Hit;
        }

        public ScreenS3D(RenderContext rc, Stereo3D s3D,  float3 pos)
        {
            ScreenMesh = new Mesh();
            //Hit = 0.065f*2f;
            _rc = rc;
            _stereo3D = s3D;
            _stereo3DShaderProgram = _rc.CreateShader(VsS3dDepth, PsS3dDepth);
            _colorShaderParam = _stereo3DShaderProgram.GetShaderParam("vColor");
            _colorTextureShaderParam = _stereo3DShaderProgram.GetShaderParam("vTexture");
            _depthTextureShaderParam = _stereo3DShaderProgram.GetShaderParam("textureDepth");
            _depthShaderParamScale = _stereo3DShaderProgram.GetShaderParam("scale");


            Position = pos;
            var faktor = 10;
            _scaleFactor = new float3(0.64f* faktor, 0.48f* faktor, 1f);
            CreatePlaneMesh();

        }

       
        /// <summary>
        /// Creates the Mesh where the Video is mapt on as a texture
        /// </summary>
        private void CreatePlaneMesh()
        {

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


            ScreenMesh.Vertices = vertecies;
            ScreenMesh.Triangles = triangles;
            ScreenMesh.Normals = normals;
            ScreenMesh.UVs = uVs;
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
            VideoFrames _videoFrames = new VideoFrames();
            for (int i = 0; i < _framesListLeft.Count; i++)
            {
                _videoFrames = GetVideoFrames();
                CrateTextures(_videoFrames);
            }
            _iTexturesListLeftEnumerator = _iTexturesListLeft.GetEnumerator();
            _iTexturesListRightEnumerator = _iTexturesListRight.GetEnumerator();
            _iTexturesListDepthLeftEnumerator = _iTexturesListDepthLeft.GetEnumerator();
            _iTexturesListDepthRightEnumerator = _iTexturesListDepthRight.GetEnumerator();


            _framesListLeft = null;
            _framesListRight = null;
            _framesListDepthLeft = null;
            _framesListDepthRight = null;

            _framesListLeftEnumerator = null;
            _framesListRightEnumerator = null;
            _framesListDepthLeftEnumerator = null;
            _framesListDepthRightEnumerator = null;
        }

        /// <summary>
        ///  Imports video (Color) file and stores frames into a List
        /// </summary>
        /// <param name="frameList"></param>
        /// <param name="path"></param>
        /// <param name="frameListEnumerator"></param>
        /// <param name="videoLength"></param>
        private void ImportVideo(List<Image<Bgr, byte>> frameList, string path, ref IEnumerator<Image<Bgr, byte>> frameListEnumerator, int videoLength)
        {

            using (Capture capture = new Capture(path))
            {
                var  c = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.PosFrames);
                var tempFrame = capture.QueryFrame().ToImage<Bgr, byte>();
                var TotalFrames = capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameCount);
                   
                var framecounter = 0;
                while (tempFrame != null)
                {
                    frameList.Add(tempFrame);
                    tempFrame = capture.QueryFrame().ToImage<Bgr, byte>();
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



        // looping videos and returning  current fram of color and depth video
        private VideoFrames GetVideoFrames()
        {
            VideoFrames vf;
            //Iterating over the frames List of the Color Video
            if (!_framesListLeftEnumerator.MoveNext())
            {
                _framesListLeftEnumerator.Reset();
                _framesListLeftEnumerator.MoveNext();
            }
            vf.CurrentLeftFrame = _framesListLeftEnumerator.Current;
            var imgDataLeft = new ImageData();
            imgDataLeft.Width = vf.CurrentLeftFrame.Width;
            imgDataLeft.Height = vf.CurrentLeftFrame.Height;
            imgDataLeft.PixelFormat = ImagePixelFormat.RGB;
            imgDataLeft.PixelData = vf.CurrentLeftFrame.Bytes;
            vf.ImgDataLeft = imgDataLeft;

            //Iterating over the frames List - Right
            if (!_framesListRightEnumerator.MoveNext())
            {
                _framesListRightEnumerator.Reset();
                _framesListRightEnumerator.MoveNext();
            }
            vf.CurrentRightFrame = _framesListRightEnumerator.Current;
            var imgDataRight = imgDataLeft;
            imgDataRight.PixelData = vf.CurrentRightFrame.Bytes;
            vf.ImgDataRight = imgDataRight;

            //Iterating over the frames List - Depth Left
            if (!_framesListDepthLeftEnumerator.MoveNext())
            {
                _framesListDepthLeftEnumerator.Reset();
                _framesListDepthLeftEnumerator.MoveNext();
            }
            vf.CurrentLeftDepthFrame = _framesListDepthLeftEnumerator.Current;
            var imgDataDepthLeft = imgDataLeft;
            imgDataDepthLeft.PixelFormat = ImagePixelFormat.Gray;
            imgDataDepthLeft.PixelData = vf.CurrentLeftDepthFrame.Bytes;
            vf.ImgDataDepthLeft = imgDataDepthLeft;


            //Iterating over the frames List - Depth Right
            if (!_framesListDepthRightEnumerator.MoveNext())
            {
                _framesListDepthRightEnumerator.Reset();
                _framesListDepthRightEnumerator.MoveNext();
            }
            vf.CurrentRightDepthFrame = _framesListDepthRightEnumerator.Current;
            var imgDataDepthRight= imgDataLeft;
            imgDataDepthRight.PixelFormat = ImagePixelFormat.Gray;
            imgDataDepthRight.PixelData = vf.CurrentRightDepthFrame.Bytes;
            vf.ImgDataDepthRight = imgDataDepthRight;


            //CreateDisparityMap(vf.CurrentLeftDepthFrame.Mat, vf.CurrentRightDepthFrame.Mat);
            return vf;
        }


        // looping videos and returning  current fram of color and depth video
        private CurrentVideoTextrures GetCurrentVideoITextures()
        {
           

            CurrentVideoTextrures cvt;
            //Iterating over the ITexture List of the Color Video
            if (!_iTexturesListLeftEnumerator.MoveNext())
            {
                _iTexturesListLeftEnumerator.Reset();
                _iTexturesListLeftEnumerator.MoveNext();
            }
            cvt.ITextureLeft = _iTexturesListLeftEnumerator.Current;

            //Iterating over the frames List - Right
            if (!_iTexturesListRightEnumerator.MoveNext())
            {
                _iTexturesListRightEnumerator.Reset();
                _iTexturesListRightEnumerator.MoveNext();
            }
            cvt.ITextureRight = _iTexturesListRightEnumerator.Current;

            //Iterating over the frames List - Depth Left
            if (!_iTexturesListDepthLeftEnumerator.MoveNext())
            {
                _iTexturesListDepthLeftEnumerator.Reset();
                _iTexturesListDepthLeftEnumerator.MoveNext();
            }
            cvt.ITextureDepthLeft = _iTexturesListDepthLeftEnumerator.Current;

            //Iterating over the frames List - Depth Right
            if (!_iTexturesListDepthRightEnumerator.MoveNext())
            {
                _iTexturesListDepthRightEnumerator.Reset();
                _iTexturesListDepthRightEnumerator.MoveNext();
            }
            cvt.ITextureDepthRight = _iTexturesListDepthRightEnumerator.Current;

            

            return cvt;
        }

      

        private void CrateTextures(VideoFrames vf)
        {
            ITexture left, right, depthleft, depthright;
            //iTexture left
            if (vf.ImgDataLeft.PixelData != null)
            {
              //  if (left == null)
                    left = _rc.CreateTexture(vf.ImgDataLeft);
                _iTexturesListLeft.Add(left);
               // _rc.UpdateTextureRegion(left, vf.ImgDataLeft, 0, 0, vf.ImgDataLeft.Width, vf.ImgDataLeft.Height);
            }

            //iTexture right
            if (vf.ImgDataRight.PixelData != null)
            {
                // if (_iTextureRight == null)
                right = _rc.CreateTexture(vf.ImgDataRight);
                _iTexturesListRight.Add(right);
                //_rc.UpdateTextureRegion(right, vf.ImgDataRight, 0, 0, vf.ImgDataRight.Width, vf.ImgDataRight.Height);
            }

          
            //depth texture left
            if (vf.ImgDataDepthLeft.PixelData != null)
            {
                //if (_iTextureDepthLeft == null)
                depthleft = _rc.CreateTexture(vf.ImgDataDepthLeft);
                _iTexturesListDepthLeft.Add(depthleft);
                //_rc.UpdateTextureRegion(depthleft, vf.ImgDataDepthLeft, 0, 0, vf.ImgDataDepthLeft.Width, vf.ImgDataDepthLeft.Height);
            }

            //depth texture right
            if (vf.ImgDataDepthRight.PixelData != null)
            {
                //if (_iTextureDepthRight == null)
                depthright = _rc.CreateTexture(vf.ImgDataDepthRight);
                _iTexturesListDepthRight.Add(depthright);
                //_rc.UpdateTextureRegion(depthright, vf.ImgDataDepthRight, 0, 0, vf.ImgDataDepthRight.Width, vf.ImgDataDepthRight.Height);
            }
        }

        private int count = 0;
        public void Update()
        {
           

            //preloaded videos
            _currentVideoTextures = GetCurrentVideoITextures();
           

            if (Input.Instance.IsKey(KeyCodes.W))
                Position += new float3(0, 0, 0.5f);

            if (Input.Instance.IsKey(KeyCodes.S))
                Position += new float3(0, 0, -0.5f);

            //Hit
            if (Input.Instance.IsKey(KeyCodes.Add))
                Hit += 0.01f;
                
            if (Input.Instance.IsKey(KeyCodes.Subtract))
                Hit -= 0.01f;



            if (Input.Instance.IsKey(KeyCodes.H))
                Console.WriteLine("Hit: "+ Hit);
        }




        public void Render3DScreen(float4x4 lookat, float4x4 rot)
        {
            float hit = 0;
            ITexture textureColor = null;
            ITexture textureDepth = null; 
            switch (_stereo3D.CurrentEye)
            {
                case Stereo3DEye.Left:
                    textureColor = _currentVideoTextures.ITextureLeft;
                    textureDepth = _currentVideoTextures.ITextureDepthLeft;
                    hit = (-Hit/2);
                    break;
                case Stereo3DEye.Right:
                    textureColor =  _currentVideoTextures.ITextureRight;
                    textureDepth = _currentVideoTextures.ITextureDepthRight;
                    hit = (Hit/2);
                    break;
            }

            if (textureColor != null && textureDepth != null)
            {
                _rc.SetShader(_stereo3DShaderProgram);
                _rc.SetShaderParam(_colorShaderParam, new float4(new float3(1, 1, 1), 1));
                _rc.SetShaderParamTexture(_colorTextureShaderParam, textureColor);
                _rc.SetShaderParamTexture(_depthTextureShaderParam, textureDepth);
                _rc.SetShaderParam(_depthShaderParamScale, 5f);
                var mv = lookat* rot *float4x4.CreateTranslation(Position) * float4x4.CreateRotationY((float)Math.PI)*float4x4.CreateTranslation(hit, 0, 0) * float4x4.CreateScale(_scaleFactor);
               
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
                _videoStreamL.Dispose();
                _videoStreamR.Dispose();
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
