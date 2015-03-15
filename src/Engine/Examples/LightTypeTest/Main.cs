using System.IO;
using Fusee.Engine;
using Fusee.Math;
using Fusee.SceneManagement;

namespace Examples.LightTypeTest
{
    [FuseeApplication(Name = "LightTypeTest", Description = "Tests a spotlight with different shaders.")]
    public class LightTypeTest : RenderCanvas
    {

        Camera scenecamera;

        public override void Init()
        {
            SceneManager.RC = RC;

            Geometry sphere = MeshReader.ReadWavefrontObj(new StreamReader(@"Assets/Sphere.obj.model"));
            Geometry spacebox = MeshReader.ReadWavefrontObj(new StreamReader(@"Assets/spacebox.obj.model"));
            Geometry lamp = MeshReader.ReadWavefrontObj(new StreamReader(@"Assets/lamp2.obj.model"));

            var _emptyRoot = new SceneEntity("emptyRoot", new MouseAction());
            var _emptySphere = new SceneEntity("emptySphere", new ActionCode());
            var _emptyLight = new SceneEntity("emptyLight", new ActionCode());

            SceneManager.Manager.AddSceneEntity(_emptyRoot);
            SceneManager.Manager.AddSceneEntity(_emptySphere);
            SceneManager.Manager.AddSceneEntity(_emptyLight);

            SceneEntity cameraholder;
            SceneEntity WorldOrigin;
            WorldOrigin = new SceneEntity("WorldOrigin", new MouseAction());
            SceneManager.Manager.AddSceneEntity(WorldOrigin);
            cameraholder = new SceneEntity("CameraOwner", new CamScript(), WorldOrigin);
            cameraholder.transform.GlobalPosition = new float3(0, 0, -10);
            scenecamera = new Camera(cameraholder);
            scenecamera.Resize(Width, Height);

            SceneEntity spaceBox = new SceneEntity("Spacebox", new DiffuseMaterial(Shaders.GetDiffuseTextureShader(RC), "Assets/sky.jpg"), new Renderer(spacebox));
            SceneManager.Manager.AddSceneEntity(spaceBox);

            // Sphere
            new SceneEntity("Sphere1", new ActionCode(), _emptySphere,
                new SpecularMaterial(Shaders.GetSpecularShader(RC), "Assets/metall2.jpg"), new Renderer(sphere))
            {
                transform =
                {
                    GlobalPosition = new float3(2, 0, 0),
                    GlobalScale = new float3(0.5f, 0.5f, 0.5f)
                }
            };

            // LightObject
            //var spot = new SpotLight(new float3(0, 0, 0), new float3(0, 0, 1), new float4(0.7f, 0.7f, 0.7f, 1), new float4(0.3f, 0.3f, 0.3f, 1), new float4(0.1f, 0.1f, 0.1f, 1), 2.0f, 0);
            var spot = new DirectionalLight(new float3(0, 0, 0), new float3(0, 0, 1), new float4(0.7f, 0.7f, 0.7f, 1), new float4(0.3f, 0.3f, 0.3f, 1), new float4(0.1f, 0.1f, 0.1f, 1), 0);

            new SceneEntity("DirLight", new RotateAction(new float3(0, 20, 0)), _emptyLight,
                new DiffuseMaterial(Shaders.GetDiffuseTextureShader(RC), "Assets/metall2.jpg"), new Renderer(lamp))
            {
                transform =
                {
                    GlobalPosition = new float3(0, 0, 0),
                    GlobalScale = new float3(0.7f, 0.7f, 0.7f)
                }
            }.AddComponent(spot);

            new SceneEntity("Root1", new ActionCode(), _emptyRoot)
            {
                transform =
                {
                    GlobalPosition = new float3(0, 0, 0),
                    GlobalScale = new float3(1, 1, 1)
                }
            };

            SceneManager.Manager.StartActionCode();

            RC.ClearColor = new float4(1, 0, 0, 1);
        }

        public override void RenderAFrame()
        {
            SceneManager.Manager.Traverse(this);
        }

        public override void Resize()
        {
            RC.Viewport(0, 0, Width, Height);
            scenecamera.Resize(Width, Height);
        }

        public static void Main()
        {
            var app = new LightTypeTest();
            app.Run();
        }
    }
}