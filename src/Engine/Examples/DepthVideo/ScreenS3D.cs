using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Linq;
using System.Text;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{
    internal struct VideoFrames
    {
        public Image<Bgr, byte> CurrentLeftFrame;
        public Image<Bgr, byte> CurrentRightFrame;
        public Image<Gray, byte> CurrentDepthFrame;


        public ImageData ImgDataLeft;
        public ImageData ImgDataRight;
        public ImageData ImgDataDepth;
    }

    public class ScreenS3D
    {
        private readonly RenderContext _rc;
        private readonly Stereo3D _stereo3D;
        private ShaderProgram _stereo3DShaderProgram;
        private IShaderParam _colorShaderParam;
        private IShaderParam _colorTextureShaderParam;
        private IShaderParam _depthTextureShaderParam;


        private Mesh _screenMesh = new Mesh();

        private readonly List<Image<Bgr, byte>> _framesListLeft = new List<Image<Bgr, byte>>();
        private readonly List<Image<Bgr, byte>> _framesListRight = new List<Image<Bgr, byte>>();
        private readonly List<Image<Gray, byte>> _framesListDepth = new List<Image<Gray, byte>>();
        private IEnumerator<Image<Bgr, byte>> _framesListLeftEnumerator;
        private IEnumerator<Image<Bgr, byte>> _framesListRightEnumerator;
        private IEnumerator<Image<Gray, byte>> _framesListDepthEnumerator;

        private VideoFrames _videoFrames = new VideoFrames();

        private ITexture _iTextureLeft;
        private ITexture _iTextureRight;
        private ITexture _iTextureDepth;

        public ScreenS3D(RenderContext rc, Stereo3D s3d, ShaderProgram sp)
        {
            _rc = rc;
            _stereo3D = s3d;
            _stereo3DShaderProgram = sp;
            _colorShaderParam = _stereo3DShaderProgram.GetShaderParam("vColor");
            _colorTextureShaderParam = _stereo3DShaderProgram.GetShaderParam("vTexture");
            _depthTextureShaderParam = _stereo3DShaderProgram.GetShaderParam("textureDepth");


            CreatePlaneMesh();

        }



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
                0, 2, 1, 0, 3, 2
            };

            var normals = new[]
            {
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(0, 0, 1)
            };
            var uVs = new[]
            {
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0)
            };


            _screenMesh.Vertices = vertecies;
            _screenMesh.Triangles = triangles;
            _screenMesh.Normals = normals;
            _screenMesh.UVs = uVs;
        }


        /// <summary>
        /// Imort the videos to the ScreenS3D object
        /// </summary>
        /// <param name="pathLeftVideo">Path to left Video</param>
        /// <param name="pathRightVideo">Path to right Video</param>
        /// <param name="pathDepthVideo">Path to depth Video</param>
        /// <param name="videoLength">Length of the videos in frames. (All three videos must have the same amount of frames and recorded with the same frame rate)</param>
        public void SetVideo(string pathLeftVideo, string pathRightVideo, string pathDepthVideo, int videoLength)
        {
            ImportVideo(_framesListLeft, pathLeftVideo, ref _framesListLeftEnumerator, videoLength);
            ImportVideo(_framesListRight, pathRightVideo, ref _framesListRightEnumerator, videoLength);
            ImportVideo(_framesListDepth, pathDepthVideo, ref _framesListDepthEnumerator, videoLength);
        }

        /// <summary>
        ///  Imports video (Color) file and stores frames into a List
        /// </summary>
        /// <param name="frameList"></param>
        /// <param name="path"></param>
        /// <param naem="frameListEnumerator"></param>
        private void ImportVideo(List<Image<Bgr, byte>> frameList, string path, ref IEnumerator<Image<Bgr, byte>> frameListEnumerator, int videoLength)
        {
            var capture = new Capture(path);

            var tempFrame = capture.QueryFrame().ToImage<Bgr, byte>();

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
            capture.Dispose();
            frameListEnumerator = frameList.GetEnumerator();
        }

        /// <summary>
        ///  Imports video (gray) file and stores frames into a List
        /// </summary>
        /// <param name="frameList"></param>
        /// <param name="path"></param>
        /// <param naem="frameListEnumerator"></param>
        private void ImportVideo(List<Image<Gray, byte>> frameList, string path, ref IEnumerator<Image<Gray, byte>> frameListEnumerator, int videoLength)
        {
            var captureDepth = new Capture(path);

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
            captureDepth.Dispose();
            frameListEnumerator = frameList.GetEnumerator();
        }


        // looping videos and returning  current fram of color and depth video
        private VideoFrames GetCurrentVideoFrames()
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
            //Iterating over the frames List - Depth
            if (!_framesListDepthEnumerator.MoveNext())
            {
                _framesListDepthEnumerator.Reset();
                _framesListDepthEnumerator.MoveNext();
            }
            vf.CurrentDepthFrame = _framesListDepthEnumerator.Current;
            var imgDataDepth = imgDataLeft;
            imgDataDepth.PixelFormat = ImagePixelFormat.Gray;
            imgDataDepth.PixelData = vf.CurrentDepthFrame.Bytes;
            vf.ImgDataDepth = imgDataDepth;
            return vf;
        }


        private void CrateTextures(VideoFrames vf)
        {
            //iTexture left
            if (vf.ImgDataLeft.PixelData != null)
            {
                if (_iTextureLeft == null)
                    _iTextureLeft = _rc.CreateTexture(vf.ImgDataLeft);

                _rc.UpdateTextureRegion(_iTextureLeft, vf.ImgDataLeft, 0, 0, vf.ImgDataLeft.Width, vf.ImgDataLeft.Height);
            }

            //iTexture right
            if (vf.ImgDataRight.PixelData != null)
            {
                if (_iTextureRight == null)
                    _iTextureRight = _rc.CreateTexture(vf.ImgDataRight);

                _rc.UpdateTextureRegion(_iTextureRight, vf.ImgDataRight, 0, 0, vf.ImgDataRight.Width, vf.ImgDataRight.Height);
            }

            //depth texture
            if (vf.ImgDataDepth.PixelData != null)
            {
                if (_iTextureDepth == null)
                    _iTextureDepth = _rc.CreateTexture(vf.ImgDataDepth);

                _rc.UpdateTextureRegion(_iTextureDepth, vf.ImgDataDepth, 0, 0, vf.ImgDataDepth.Width, vf.ImgDataDepth.Height);
            }
        }

        private void Update()
        {
            _videoFrames = GetCurrentVideoFrames();
            CrateTextures(_videoFrames);
        }

        public void RenderScreen(float4x4 rot, float4x4 lookat)
        {
            Update();
           
           
            for (var x = 0; x < 2; x++)
            {
                var lookAt = lookat;

                var renderOnly = (_stereo3D.CurrentEye == Stereo3DEye.Left);
                var b = 0.02f;
                if (_stereo3D.CurrentEye == Stereo3DEye.Left)
                {

                    //left
                    _rc.SetShader(_stereo3DShaderProgram);
                    _rc.SetShaderParam(_colorShaderParam, new float4(new float3(1, 1, 1), 1.0f));
                    _rc.SetShaderParamTexture(_colorTextureShaderParam, _iTextureLeft);
                    _rc.SetShaderParamTexture(_depthTextureShaderParam, _iTextureDepth);
                    _rc.ModelView = lookAt * rot * float4x4.CreateTranslation(0, 0, 30) *float4x4.CreateRotationY((float)Math.PI) *
                                   float4x4.CreateScale(0.64f * 10, 0.48f * 10, 1f);
                    _rc.Render(_screenMesh);
                }
                else
                {
                    //right

                    _rc.SetShader(_stereo3DShaderProgram);
                    _rc.SetShaderParam(_colorShaderParam, new float4(new float3(1, 1, 1), 1.0f));
                    _rc.SetShaderParamTexture(_colorTextureShaderParam, _iTextureRight);
                    _rc.SetShaderParamTexture(_depthTextureShaderParam, _iTextureDepth);
                    _rc.ModelView = lookAt * rot * float4x4.CreateTranslation(0, 0, 30) *float4x4.CreateRotationY((float)Math.PI) *
                                   float4x4.CreateScale(0.64f * 10, 0.48f * 10, 1f);
                    _rc.Render(_screenMesh);
                }


                _stereo3D.Save();

                if (x == 0)
                    _stereo3D.Prepare(Stereo3DEye.Right);
            }
        }
    }
}
