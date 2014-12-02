using System.Collections.Generic;
using System.IO;
using Fusee.Engine;
using Fusee.Engine.SimpleScene;
using Fusee.SceneManagement;
using Fusee.Math;
using Fusee.Serialization;


namespace Examples.ScenePickerSimple
{
    public class CubeContainer : MeshContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Cube" /> class.
        /// Cube is a derivate of the <see cref="Mesh" /> class.
        /// The default cube is 1 unit big and contains various default vertex colors.
        /// The vertex colors are only visible during rendering when a vertexcolor shader is applied on the Mesh.
        /// </summary>
        public CubeContainer()
        {
            #region Fields

            // TODO: Remove redundant vertices
            Vertices = new[]
            {
                new float3 {x = +0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = -0.5f, z = -0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = -0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = -0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = -0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = -0.5f},
                new float3 {x = +0.5f, y = -0.5f, z = -0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = -0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = -0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = +0.5f, z = -0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = -0.5f},
                new float3 {x = -0.5f, y = +0.5f, z = +0.5f},
                new float3 {x = +0.5f, y = -0.5f, z = -0.5f},
                new float3 {x = +0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = +0.5f},
                new float3 {x = -0.5f, y = -0.5f, z = -0.5f}

            };

            Triangles = new ushort[]
            {
                // front face
                0, 2, 1, 0, 3, 2,

                // right face
                4, 6, 5, 4, 7, 6,
                
                // back face
                8, 10, 9, 8, 11, 10,
               
                // left face
                12, 14, 13, 12, 15, 14,
                
                // top face
                16, 18, 17, 16, 19, 18,

                // bottom face
                20, 22, 21, 20, 23, 22

            };

            Normals = new[]
            {
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(0, 0, 1),
                new float3(1, 0, 0),
                new float3(1, 0, 0),
                new float3(1, 0, 0),
                new float3(1, 0, 0),
                new float3(0, 0, -1),
                new float3(0, 0, -1),
                new float3(0, 0, -1),
                new float3(0, 0, -1),
                new float3(-1, 0, 0),
                new float3(-1, 0, 0),
                new float3(-1, 0, 0),
                new float3(-1, 0, 0),
                new float3(0, 1, 0),
                new float3(0, 1, 0),
                new float3(0, 1, 0),
                new float3(0, 1, 0),
                new float3(0, -1, 0),
                new float3(0, -1, 0),
                new float3(0, -1, 0),
                new float3(0, -1, 0)
            };

            UVs = new[]
            {
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0),
                new float2(1, 0),
                new float2(1, 1),
                new float2(0, 1),
                new float2(0, 0)
            };
        #endregion  
        }
    }

    public class ScenePickerSimple : RenderCanvas
    {

        SceneContainer CreateTestCubeScene()
        {
            return new SceneContainer()
            {
                Header = new SceneHeader()
                {
                    CreatedBy = "Test",
                    CreationDate = "Today",
                    Generator = "Code",
                    Version = 1
                },
                Children = new List<SceneObjectContainer>(
                    new []
                    {
                        new SceneObjectContainer()
                        {
                            Transform = new TransformContainer
                            {
                                Translation = new float3(0, 0, 0),
                                Rotation = new float3(0, 0, 0),
                                Scale = new float3(1, 1, 1)
                            },
                            Material = new MaterialContainer()
                            {
                                Diffuse = new MatChannelContainer()
                                {
                                    Color = new float3(1, 0, 0)
                                }
                            },
                            Mesh = new CubeContainer()    
                        }
                    })
            };

        }

        private ScenePicker _sp;
        
        private Mesh _meshTea;
        private Mesh _meshCube;

        private ShaderProgram _spColor;
        private IShaderParam _colorParam;
        private SceneRenderer _sr;

        SceneContainer _scene;

        // is called on startup
        public override void Init()
        {

            RC.ClearColor = new float4(0.6f, 1, 1, 1);
            
            /*
            var ser = new Serializer();
            using (var file = File.OpenRead(@"Assets/Wuggy.fus"))
            {
                _scene = ser.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
                _sr = new SceneRenderer(_scene, "Assets");
            }
            */
            _scene = CreateTestCubeScene();
            _sr = new SceneRenderer(_scene, "Assets");
            _sp = new ScenePicker(RC);

            //_meshTea = MeshReader.LoadMesh(@"Assets/Teapot.obj.model");
            //_meshCube = MeshReader.LoadMesh(@"Assets/Cube.obj.model");

            _spColor = MoreShaders.GetDiffuseColorShader(RC);
            _colorParam = _spColor.GetShaderParam("color");
        }

        // is called once a frame
        public override void RenderAFrame()
        {
            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            /*
            var mtxCam = float4x4.LookAt(0, 200, 500, 0, 0, 0, 0, 1, 0);
            RC.Model = float4x4.CreateTranslation(0, -200, -200);
            RC.View = mtxCam;
            */

            RC.Model = float4x4.Identity;
            var mtxCam = float4x4.LookAt(0, 0, -5, 0, 0, 0, 0, 1, 0);
            RC.View = mtxCam;


            Point pickPos = new Point();
            if (Input.Instance.IsButton(MouseButtons.Left))
            {
                pickPos = Input.Instance.GetMousePos();
                _sp.Pick(_scene, pickPos);
            }

           /* //Teapot
            RC.Model = float4x4.CreateTranslation(-100, -50, 0);
            RC.SetShaderParam(_colorParam, new float4(0.5f, 0.8f, 0, 1));
            RC.Render(_meshTea);

            //Cube
            RC.Model = float4x4.CreateTranslation(100, 0, 0) * float4x4.Scale(0.6f);
            RC.SetShaderParam(_colorParam, new float4(0, 0.5f,0.8f,1));
            RC.Render(_meshCube); */

            _sr.Render(RC);

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
