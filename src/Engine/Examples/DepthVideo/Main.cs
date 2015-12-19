using System;
using System.Collections.Generic;
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
        
        private Mesh _meshCube;

        // variables for shader
        private ShaderProgram _spTexture;

        private IShaderParam _textureParam;
        private ITexture _iTextureColor;
        private ITexture _iTextureDepth;

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
            Resize();

            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
           

            //init shader
            _spTexture = Shaders.GetTextureShader(RC);
            _textureParam = _spTexture.GetShaderParam("texture1");

            //Load Videos
            ImportVideos(_framesListColorVideo, "Assets/demoSmall.mkv", _framesListDepthVideo, "Assets/demoDepthSmall.mkv");
            _framesListColorEnumerator = _framesListColorVideo.GetEnumerator();
            _framesListDepthEnumerator = _framesListDepthVideo.GetEnumerator();
        }

       

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);
            if (Input.Instance.IsKey(KeyCodes.Escape))
                CloseGameWindow();

            _currentVideoFrames = GetCurrentVideoForames();
            CrateTextures(_currentVideoFrames);

            var mtxCam = float4x4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);
            var modelViewMesh1 = mtxCam * float4x4.Scale(0.01f) * float4x4.CreateTranslation(0, 0, 0);

            
            RC.SetShader(_spTexture);
            RC.ModelView = modelViewMesh1;
            RC.SetShaderParamTexture(_textureParam, _iTextureColor);
            RC.Render(_meshCube);

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
                if (framecounter >= 149)
                {
                    break;
                }
            }

            framecounter = 0;
            while (tempFrameDepth != null)
            {
                frameListDepth.Add(tempFrameDepth);
                tempFrameDepth = captureDepth.QueryFrame().ToImage<Gray, byte>();
                framecounter++;
                if (framecounter >= 149)
                {
                    break;
                }
            }
        }

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

        private void CrateTextures(CurrentVideoFrames cvf)
        {
            var _imgDataColor = new ImageData();
            _imgDataColor.Width = cvf.CurrentColorFrame.Width;
            _imgDataColor.Height = cvf.CurrentColorFrame.Height;
            _imgDataColor.PixelFormat = ImagePixelFormat.RGB;
            _imgDataColor.PixelData = cvf.CurrentColorFrame.Bytes;

            var _imgDataDepth = new ImageData();
            _imgDataDepth.Width = cvf.CurrentDepthFrame.Width;
            _imgDataDepth.Height = cvf.CurrentDepthFrame.Height;
            _imgDataDepth.PixelFormat = ImagePixelFormat.Gray;
            _imgDataDepth.PixelData = cvf.CurrentDepthFrame.Bytes;

            Console.WriteLine(_imgDataDepth.PixelData.Length + " : " + _iTextureColor);

            //color texture
            if (_imgDataColor.PixelData != null)
            {
                if (_iTextureColor == null)
                    _iTextureColor = RC.CreateTexture(_imgDataColor);

                RC.UpdateTextureRegion(_iTextureColor, _imgDataColor, 0, 0, _imgDataColor.Width, _imgDataColor.Height);
            }
            //depth texture
            if (_imgDataDepth.PixelData != null)
            {
                if (_iTextureDepth == null)
                    _iTextureDepth = RC.CreateTexture(_imgDataDepth);

                RC.UpdateTextureRegion(_iTextureDepth, _imgDataDepth, 0, 0, _imgDataDepth.Width, _imgDataDepth.Height);
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
