using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Fusee.Engine;
using Fusee.Engine.SimpleScene;
using Fusee.Math;
using Fusee.Serialization;


namespace Examples.LevelTest
{
    public class LevelTest : RenderCanvas
    {
        //server varialbles
        private ThreadPoolTcpSrvr _tpts;

        //gui variables
        private GUIText _guiSubText;
        private GUIText _serverText;
        private IFont _guiLatoBlack;
        private GUIHandler _guiHandler;

        public static int GuiPosX;
        public static int GuiPosY;

        private GUI _gui;

        // angle variables
        private static float _angleHorz, _angleVert, _angleVelHorz, _angleVelVert;

        private const float RotationSpeed = 1f;
        private const float Damping = 0.92f;

        // model variables
        private SceneRenderer _srSky;
        private SceneContainer _sceneSky;

        private SceneRenderer _srLevel1;
        private SceneContainer _sceneLevel1;

        private SceneRenderer _srDeko;
        private SceneContainer _sceneDeko;

        private SceneRenderer _srFire;
        private SceneContainer _sceneFire;
        float3 rot;

        private SceneRenderer _srWater;
        private SceneContainer _sceneWater;

        private SceneRenderer _srAir;
        private SceneContainer _sceneAir;

        private SceneRenderer _srEarth;
        private SceneContainer _sceneEarth;

        private SceneRenderer _srBorder;
        private SceneContainer _sceneBorder;

        // variables for shader
        private ShaderProgram _spTexture;
        private ShaderProgram _spColor;

        private IShaderParam _colorParam;
        private IShaderParam _textureParam;

        private ITexture _iTex;

        private static List<Player> _playerList = new List<Player>();

        


        // some logic
        private bool _isEmpty;

        /*****  TEST PURPOSE  ****** /
        float3 _moveX = new float3(10, 0, 0);
        float3 _moveMinusX = new float3(-10, 0, 0);
        float3 _moveZ = new float3(0, 0, 10);
        float3 _moveMinusZ = new float3(0, 0, -10);
        float3 _testGravity = new float3(0, -9.81f, 0);
        /***************************/

        private float3 _averageNewPos;

        public static Physic LevelPhysic { get; private set; }

        public static int WindowHeight = 1080;
        public static int WindowWidth = 1920;

        // is called on startup
        public override void Init()
        {
            SetWindowSize(WindowWidth, WindowHeight, 0, 0);

            //creates thread for TcpServer, sets it as backgroundthread, starts the thread
            var tcpServer = new Thread(StartTcpServer);
            tcpServer.IsBackground = true;
            tcpServer.Start(this);


            _gui = new GUI(RC);

            RC.ClearColor = new float4(0.1f, 0.1f, 0.1f, 1);

            //Border
            var serBorder = new Serializer();
            using (var file = File.OpenRead(@"Assets/border.fus"))
            {
                _sceneBorder = serBorder.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }

            _srBorder = new SceneRenderer(_sceneBorder, "Assets");


            //Scene Skybox
            var serSky = new Serializer();
            using (var file = File.OpenRead(@"Assets/Skybox1.fus"))
            {
                _sceneSky = serSky.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }

            _srSky = new SceneRenderer(_sceneSky, "Assets");

            //Scene Level1
            var serLevel1 = new Serializer();
            using (var file = File.OpenRead(@"Assets/prepared_for_physics3.fus"))
            {
                _sceneLevel1 = serLevel1.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }

            _srLevel1 = new SceneRenderer(_sceneLevel1, "Assets");


            var serDeko = new Serializer();
            using (var file = File.OpenRead(@"Assets/assets_scale1.fus"))
            {
                _sceneDeko = serDeko.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }

            _srDeko = new SceneRenderer(_sceneDeko, "Assets");

            //Scene Spheres
            //Fire
            var serFire = new Serializer();
            using (var file = File.OpenRead(@"Assets/player_fire.fus"))
            {
                _sceneFire = serFire.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }
            _srFire = new SceneRenderer(_sceneFire, "Assets");


            //rotation of firesphere
            foreach (SceneNodeContainer node in _sceneFire.Children.FindNodes(node => node.Name.Equals("Fire")))
            {
                TransformComponent transform = node.GetTransform();
                //rotation component
                rot = transform.Rotation;
            }


            //Water
            var serWater = new Serializer();
            using (var file = File.OpenRead(@"Assets/Player_water.fus"))
            {
                _sceneWater = serWater.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }
            _srWater = new SceneRenderer(_sceneWater, "Assets");

            //Earth
            var serEarth = new Serializer();
            using (var file = File.OpenRead(@"Assets/player_earth.fus"))
            {
                _sceneEarth = serEarth.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }
            _srEarth = new SceneRenderer(_sceneEarth, "Assets");

            //Air
            var serAir = new Serializer();
            using (var file = File.OpenRead(@"Assets/Player_Air.fus"))
            {
                _sceneAir = serAir.Deserialize(file, null, typeof(SceneContainer)) as SceneContainer;
            }
            _srAir = new SceneRenderer(_sceneAir, "Assets");


            _spColor = MoreShaders.GetDiffuseColorShader(RC);

            _colorParam = _spColor.GetShaderParam("color");

            _spTexture = MoreShaders.GetTextureShader(RC);

            _textureParam = _spTexture.GetShaderParam("texture1");

            //Physics
            LevelPhysic = new Physic();
            LevelPhysic.InitScene();
        }

        public static List<Player> GetPlayerList()
        {
            return _playerList;
        } 

        // is called once a frame
        public override void RenderAFrame()
        {

            RC.Clear(ClearFlags.Color | ClearFlags.Depth);

            //Physic
            LevelPhysic.World.StepSimulation((float)Time.Instance.DeltaTime, (Time.Instance.FramePerSecondSmooth / 60), 1 / 60);
            

            /****************  TEST PURPOSE - hit spacebar to render test player   ********************** /

            if (Input.Instance.IsKey(KeyCodes.Space))
            {
                IPAddress itsmeIpAddress = IPAddress.Parse("127.0.0.1");
                var initPos = new float3(_playerList.Count * 10, 200, 0);
                _playerList.Add(new Player("Testplayer", initPos, itsmeIpAddress));
            }

            if (_playerList.Exists(x => Equals(x.Id, "Testplayer")))
            {
                var testplayer = _playerList.FirstOrDefault(x => x.Id == "Testplayer");
                //if (testplayer != null)
                // testplayer.Move(_testGravity);
               
                if (Input.Instance.IsKey(KeyCodes.Up))
                {
                    if (testplayer != null)
                        testplayer.Move(_moveMinusX);
                }
                if (Input.Instance.IsKey(KeyCodes.Down))
                {
                    if (testplayer != null)
                        testplayer.Move(_moveX);
                }
                if (Input.Instance.IsKey(KeyCodes.Left))
                {
                    if (testplayer != null)
                        testplayer.Move(_moveMinusZ);
                }
                if (Input.Instance.IsKey(KeyCodes.Right))
                {
                    if (testplayer != null)
                        testplayer.Move(_moveZ);
                }

            }
            /*************************************************/

            _isEmpty = !_tpts.GetConnections().Any();
            if (_isEmpty)
            {
                //Console.WriteLine("Awaiting Connections");
            }
            else
            {

                foreach (var connection in _tpts.GetConnections())
                {
                    //var item = _playerList.Find(x => x.IpAddress == (connection.Address));
                    var ipExists = _playerList.Exists(x => Equals(x.IpAddress, connection.Address));
                    if (!ipExists || _playerList.Count == 0)
                    {
                        var ipAddress = connection.Address;
                        var id = "Spieler" + (_playerList.Count + 1);

                        // Set initial position for each player
                        var initPos = new float3(_playerList.Count * 10, 60, 0);

                        //if (i > 4) i = 1;
                        _playerList.Add(new Player(id, initPos, ipAddress));
                    }
                }
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (TcpConnection connection in _tpts.GetConnections())
                {
                    sb.Append(connection.Message);
                    sb.Append("// ");
                    //Console.WriteLine(ExtractNumbers(connection.Message).Length);
                }
                // _gui.RenderMsg(sb.ToString());

            }
            catch (NullReferenceException)
            {
                //_gui.RenderMsg("Nichts empfangen!");
            }

            //Array for Players Position 
            float3[] playerPos = new float3[3];

            //Array for new Players Position
            float3[] newPlayerPos = new float3[3];

            //Array for input
            float3[] move = new float3[3];


            //Camera Minimum and Maximum
            var camMin = new float3(0, 0, 0);
            var camMax = new float3(0, 0, 0);

            var inputA = 0;
            var inputB = 0;
            var inputC = 0;
            var inputD = 0;
            var inputE = 0;
            var inputF = 0;


            _angleHorz += _angleVelHorz;
            _angleVert += _angleVelVert;

            _averageNewPos = new float3(0, 0, 0);
            for (int i = 0; i < _playerList.Count; i++)
            {

                _averageNewPos += _playerList[i].GetPostion();

                //  Console.WriteLine(move[i]);
            }
            if (_playerList.Count >= 2)
            {
                _averageNewPos *= (float)(1.0 / _playerList.Count);
                Console.WriteLine(_averageNewPos);
            }

            camMin = new float3(_averageNewPos.x - 750, 0, _averageNewPos.z - 550);
            camMax = new float3(_averageNewPos.x + 750, 0, _averageNewPos.z + 950);

            _angleHorz = 1.56f;
            _angleVert = -0.445f;
            var mtxRot = float4x4.Identity;

            var mtxCam = float4x4.CreateTranslation(_averageNewPos.x, 0, _averageNewPos.z) * float4x4.CreateRotationY(-_angleHorz) * float4x4.CreateRotationX(-_angleVert) * float4x4.CreateTranslation(0, 0, -2500);
            mtxCam.Invert();

            foreach (var player in _playerList)
            {
                var Pos = player.GetPostion();

                if (player.GetPostion().x <= camMin.x)
                {
                    var position = new float3(camMin.x, Pos.y, Pos.z);
                    player.SetPosition(position);

                }
                else
                {
                    if (player.GetPostion().x >= camMax.x)
                    {
                        var position = new float3(camMax.x, Pos.y, Pos.z);
                        player.SetPosition(position);

                    }
                    else
                    {
                        Pos.x = player.GetPostion().x;
                    }
                }

                if (player.GetPostion().z <= camMin.z)
                {
                    var position = new float3(Pos.x, Pos.y, camMin.z);
                    player.SetPosition(position);

                }
                else
                {
                    if (player.GetPostion().z >= camMax.z)
                    {
                        var position = new float3(Pos.x, Pos.y, camMax.z);
                        player.SetPosition(position);
                    }
                    else
                    {
                        Pos.z = player.GetPostion().z;
                    }
                }
            }

            RC.SetShader(_spColor);
            // border 
            var mtxR = float4x4.CreateTranslation(_averageNewPos.x, -20, _averageNewPos.z);
            RC.ModelView = mtxCam * mtxR;
            _srBorder.Render(RC);

            //_srEarth.Render(RC);
            foreach (var player in _playerList)
            {
                //var mtxM1 = float4x4.CreateTranslation(player.GetPostion());
                var mtxM1 = float4x4.Transpose(player.GetRigidBody().WorldTransform);
                //var mtxScalePlayer = float4x4.CreateScale(5);
                RC.ModelView = mtxCam * mtxM1;

                switch (player.ElementString)
                {
                    case "fire":
                        // if(player.GetPostion().y >= -100)
                        _srFire.Render(RC);
                        break;
                    case "water":
                        _srWater.Render(RC);
                        break;
                    case "air":
                        _srAir.Render(RC);
                        break;
                    case "earth":
                        _srEarth.Render(RC);
                        break;
                }

                // move the player(s)
                foreach (var tcpConnection in _tpts.GetConnections())
                {
                    var tcpAddress = tcpConnection.Address;
                    var playerObject = _playerList.Find(x => x.IpAddress == (tcpAddress));
                    if (playerObject != null)
                    {
                        var moveCoord = DecryptMessage(tcpConnection.Message);


                        playerObject.Move(moveCoord);
                        var isAbgestuerzt = playerObject.GetPostion().y <= -20;
                        if (isAbgestuerzt)
                        {
                            playerObject.Respawn();


                        }
                    }
                }
            }

            //Skybox
            var mtxScale = float4x4.CreateScale(1.5f);
            RC.ModelView = mtxCam * mtxRot * mtxR * mtxScale;
            _srSky.Render(RC);

            //Level1
            //var mtxTranslLevel = float4x4.CreateTranslation(0, -101, 0);
            var mtxScaleLevel = float4x4.CreateScale(0.7f);
            RC.ModelView = mtxCam;
            _srLevel1.Render(RC);

            _srDeko.Render(RC);

            if (Input.Instance.IsKey(KeyCodes.Escape))
            {
                CloseGameWindow();
            }


            //************* Insert in Case 1 "Wait"********************//
            //GUI

            GuiPosY = Width / 2;
            GuiPosX = -(Height / 2);

            var playerPosition = new float3[] {};

            float fps = Time.Instance.FramePerSecond;
            _gui.RenderCount(_playerList.Count);
            int index = 0;
            foreach (var player in _playerList)
            {
                playerPosition[index] = player.GetPostion();
                index++;
            }
               
            switch (_playerList.Count)
            {
                case 2:
                    _gui.RenderPlayerPos(playerPosition[0], playerPosition[1]);
                    break;
                case 3:
                    _gui.RenderPlayerPos(playerPosition[0], playerPosition[1], playerPosition[2]);
                    break;
                case 4:
                    _gui.RenderPlayerPos(playerPosition[0], playerPosition[1], playerPosition[2], playerPosition[3]);
                    break;
            }
            _gui.RenderFps(fps);
            _gui.RenderWait("Waiting for Connections...");
      
            //*********************************************************//


            //******************* VIDEO TEST *************************//
            if (Input.Instance.IsKey(KeyCodes.W))
            {

                string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    path = Directory.GetParent(path).ToString();
                }

                Process[] proc = Process.GetProcessesByName("MediaPlayerCSTest");
                if (proc.Length == 0)
                {
                    Process.Start(path + @"\Documents\GitHub\MediaPlayer\MediaPlayerCSTest\bin\Debug\MediaPlayerCSTest.exe");
                }
            //******************* VIDEO TEST *************************//
            }
            

            Present();
        }

        private float3 DecryptMessage(string message)
        {
            if (message.Length == 0) return new float3(0, 0, 0);
            var split = message.Split(new char[] { ':', ' ', ',', ';' });
            var sensorData = new float3();
            sensorData.x = float.Parse(split[2]) / 10;
            sensorData.y = -3;
            sensorData.z = -float.Parse(split[6]) / 10;
            return sensorData;
        }

        // is called when the window was resized
        public override void Resize()
        {
            RC.Viewport(0, 0, Width, Height);

            var aspectRatio = Width / (float)Height;
            RC.Projection = float4x4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1, 100000);
        }

        public static void StartTcpServer(object self)
        {
            ((LevelTest)self)._tpts = new ThreadPoolTcpSrvr();
            ((LevelTest)self)._tpts.StartListening();
        }

        public override void DeInit()
        {
            base.DeInit();
            LevelPhysic.World.Dispose();
        }

        public static void Main()
        {
            var app = new LevelTest();
            app.Run();
        }
    }
}
