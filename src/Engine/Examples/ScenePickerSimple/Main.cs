using Fusee.Engine;
using Fusee.Engine.SimpleScene;
using Fusee.SceneManagement;
using Fusee.Math;
using Fusee.Serialization;


namespace Examples.ScenePickerSimple
{
    public class ScenePickerSimple : RenderCanvas
    {

        private ScenePicker _sp;
        
        private Mesh _meshTea;
        private Mesh _meshCube;

        private ShaderProgram _spColor;
        private IShaderParam _colorParam;

        // is called on startup
        public override void Init()
        {

            RC.ClearColor = new float4(1, 1, 1, 1);

            _sp = new ScenePicker(RC);

            _meshTea = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");
            _meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");

            _spColor = MoreShaders.GetDiffuseColorShader(RC);
            _colorParam = _spColor.GetShaderParam("color");
        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            var mtxCam = float4x4.LookAt(0, 200, 500, 0, 0, 0, 0, 1, 0);

            RC.SetShader(_spColor);

            RC.View = mtxCam;

            //Teapot
            RC.Model = float4x4.CreateTranslation(-100, -50, 0);
            RC.SetShaderParam(_colorParam, new float4(0.5f, 0.8f, 0, 1));
            RC.Render(_meshTea);

            RC.Model = float4x4.CreateTranslation(100, 0, 0) * float4x4.Scale(0.6f);
            RC.SetShaderParam(_colorParam, new float4(0, 0.5f,0.8f,1));
            RC.Render(_meshCube);

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
            var app = new ScenePickerSimple();
            app.Run();
        }
    }
}
