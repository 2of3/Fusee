using Emgu.CV;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.DepthVideo
{
    [FuseeApplication(Name = "DepthVideo", Description = "Integtrating a video with depth information.")]
    public class DepthVideo : RenderCanvas
    {
        
        private Mesh _meshCube;

        // variables for shader
        private ShaderProgram _spTexture;

        private IShaderParam _textureParam;
        private ITexture _iTexture;




        // is called on startup
        public override void Init()
        {
            RC.ClearColor = new float4(0.1f, 0.1f, 0.5f, 1);

            //Set zNear and zFar (1, 10)
            Resize();

            //init mesh
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");
            var img = RC.LoadImage("Assets/world_map.jpg");
            _iTexture = RC.CreateTexture(img);

            //init shader
            _spTexture = Shaders.GetTextureShader(RC);
            _textureParam = _spTexture.GetShaderParam("texture1");
        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            var mtxCam = float4x4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);
            var modelViewMesh1 = mtxCam * float4x4.Scale(0.01f) * float4x4.CreateTranslation(0, 0, 0);

            RC.SetShader(_spTexture);
            RC.ModelView = modelViewMesh1;
            RC.SetShaderParamTexture(_textureParam, _iTexture);
            RC.Render(_meshCube);

            Present();
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
