using System.Dynamic;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.LevelTest
{
    // ReSharper disable once InconsistentNaming
    class GUI
    {
        private readonly GUIHandler _guiHandler;

        private IFont _fontSmall;
        private IFont _fontMedium;
        private IFont _fontBig;

        private readonly GUIPanel _guiPanel;

        private GUIText _fps, _serverMsg, _waitMsg;

        private readonly float4 _color1 = new float4(1f, 1f, 1f, 1);
        private readonly float4 _color2 = new float4(1, 1, 1, 1);
        private readonly float4 _color3 = new float4(0, 0.1f, 1, 1);

        public static int _windowWidth;
        public static int _windowHeight;


        public GUI(RenderContext rc)
        {
            //Basic Init

            _fontSmall = rc.LoadFont("Assets/Lato-Black.ttf", 12);
            _fontMedium = rc.LoadFont("Assets/Lato-Black.ttf", 18);
            _fontBig = rc.LoadFont("Assets/Lato-Black.ttf", 40);

            _guiHandler = new GUIHandler();
            _guiHandler.AttachToContext(rc);

            int panelPosY = LevelTest.WindowHeight / 2;
            int panelPosX = LevelTest.WindowWidth / 2;
            int panelHeight = 150;
            int panelWidth = LevelTest.WindowWidth / 3;

            //Start Pannel Init
            _guiPanel = new GUIPanel("", _fontBig, 100, 0, 300, 150);
            _guiPanel.PanelColor = new float4(1, 1, 1, 0);
            _guiPanel.BorderColor = new float4(1, 1, 1, 0);

            _fps = new GUIText("FPS", _fontMedium, 20, 20, _color2);
            _waitMsg = new GUIText("Waiting for Connections...", _fontBig, panelPosX - 350, panelPosY, _color1);

            //TODO: write number of connected players / player number
            //_serverMsg = new GUIText("Message received:", _fontMedium, 30, 95, _color2);


            _guiPanel.ChildElements.Add(_fps);
            //_guiPanel.ChildElements.Add(_serverMsg);
            _guiPanel.ChildElements.Add(_waitMsg);

            ShowGUI();
        }


        public void RenderFps(float fps)
        {
            _fps.Text = "FPS: " + fps;
            _guiHandler.RenderGUI();

        }

        /*public void RenderMsg(string serverMsg)
        {
            _serverMsg.Text = "Message received: " + serverMsg; 
            
        }*/

        public void RenderWait(string wait)
        {
            _waitMsg.Text = wait;
        }

        public void ShowGUI()
        {
            _guiHandler.Clear();
            _guiHandler.Add(_guiPanel);
        }


        public void Resize()
        {
            _guiHandler.Refresh();
        }
    }
}
