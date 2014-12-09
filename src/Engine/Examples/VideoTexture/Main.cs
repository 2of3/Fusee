using System;
using Fusee.Engine;
using Fusee.Math;


namespace Examples.VideoTextureExample
{
    public class VideoTextureExample : RenderCanvas
    {

        // angle variables
        private static float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;

        private const float RotationSpeed = 1f;
        private const float Damping = 0.92f;

        // model variables
        private Mesh _meshCube;
        private Mesh _meshCube2;


        // variables for shader
        private ShaderProgram _spTexture;
        private ShaderProgram _spTexture_2;

        private IShaderParam _textureParam;

        private ITexture _iTex;
        private ITexture _iTex_2;

        private IVideoStreamImp _videoStream;
        private IVideoStreamImp _videoStream_2;


        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(0.1f, 0.1f, 0.5f, 1);

            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            _meshCube2 = MeshReader.LoadMesh(@"Assets/Cube.obj.model");

            _spTexture = MoreShaders.GetTextureShader(RC);
            _spTexture_2 = MoreShaders.GetTextureShader(RC);

            _textureParam = _spTexture.GetShaderParam("texture1");

            _videoStream = VideoManager.Instance.LoadVideoFromFile(@"Assets/pot.webm", true);
            _videoStream_2 = VideoManager.Instance.LoadVideoFromFile(@"Assets/Rollin_Wild.mp4", true);
            //_videoStream = VideoManager.Instance.LoadVideoFromCamera(0, false);
        }


        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            // Use this function to update a texture from a video stream if you plan to have
            // a web build and if you don't need to access the pixel data directly.
            // This method is much more performant than the other one.


            //RC.UpdateTextureFromVideoStream(_videoStream, _iTex);
            // RC.UpdateTextureFromVideoStream(_videoStream_2, _iTex_2);


            // If you don't plan to have a web-build or if you want to have direct access to the video's
            // pixel data, use this method. Note that this works in the web-build too, but the performance
            // is really bad and it might cause a memory overflow in the web-build.
            //var imgData = _videoStream.GetCurrentFrame();
            //if (imgData.PixelData != null)
            //{
            //    if (_iTex == null)
            //        _iTex = RC.CreateTexture(imgData);
            //    RC.UpdateTextureRegion(_iTex, imgData, 0, 0);
            //}


            if (Input.Instance.IsKey(KeyCodes.P))
                _videoStream.Stop();
            if (Input.Instance.IsKey(KeyCodes.Space))
                _videoStream.Start();

            // move per mouse
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                _angleVelHorz = -RotationSpeed * Input.Instance.GetAxis(InputAxis.MouseX);
                _angleVelVert = RotationSpeed * Input.Instance.GetAxis(InputAxis.MouseY);
            }
            else
            {
                var curDamp = (float)Math.Exp(-Damping * Time.Instance.DeltaTime);

                _angleVelHorz *= curDamp;
                _angleVelVert *= curDamp;
            }

            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            if (Input.Instance.IsKey(KeyCodes.P))
                _videoStream.Stop();
            if (Input.Instance.IsKey(KeyCodes.Space))
                _videoStream.Start();

            // move per keyboard
            if (Input.Instance.IsKey(KeyCodes.Left))
                _angleHorz -= RotationSpeed * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKey(KeyCodes.Right))
                _angleHorz += RotationSpeed * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKey(KeyCodes.Up))
                _angleVert -= RotationSpeed * (float)Time.Instance.DeltaTime;

            if (Input.Instance.IsKey(KeyCodes.Down))
                _angleVert += RotationSpeed * (float)Time.Instance.DeltaTime;

            var mtxRot = float4x4.CreateRotationX(_angleVert) * float4x4.CreateRotationY(_angleHorz);
            var mtxCam = float4x4.LookAt(0, 200, 500, 0, 0, 0, 0, 1, 0);

<<<<<<< HEAD
            if (_videoStream.Width != 0)
            {
                if (_iTex == null)
                    _iTex = RC.CreateTexture(RC.CreateImage(_videoStream.Width, _videoStream.Height, "#000000"));

                RC.UpdateTextureFromVideoStream(_videoStream, _iTex);
            }

            RC.SetShader(_spTexture);
            if (_iTex != null)
                RC.SetShaderParamTexture(_textureParam, _iTex);

            // second mesh

            RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(-150, 0, 0);
=======
            RC.SetShader(_spTexture);
            if (_iTex != null)
                RC.SetShaderParamTexture(_textureParam, _iTex);
            
            RC.ModelView = mtxCam *  mtxRot * float4x4.CreateTranslation(-150, 0, 0);
            RC.Render(_meshCube);
            //second Mesh
            RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(150, 0, 0);
>>>>>>> origin/feat_video_fixes
            RC.Render(_meshCube);

            if (_videoStream_2.Width != 0)
            {
                if (_iTex_2 == null)
                    _iTex_2 = RC.CreateTexture(RC.CreateImage(_videoStream_2.Width, _videoStream_2.Height, "#000000"));

                RC.UpdateTextureFromVideoStream(_videoStream_2, _iTex_2);
            }

            RC.SetShader(_spTexture_2);
            if (_iTex_2 != null)
                RC.SetShaderParamTexture(_textureParam, _iTex_2);

            RC.ModelView = mtxCam * mtxRot * float4x4.CreateTranslation(150, 0, 0);
            RC.Render(_meshCube2);




            Present();
        }

        // is called when the window was resized
        public override void Resize()
        {
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 10000);
        }

        public static void Main()
        {
            var app = new VideoTextureExample();
            app.Run();
        }
    }
}
