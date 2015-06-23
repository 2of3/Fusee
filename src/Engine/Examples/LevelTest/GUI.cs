using System;
using System.Diagnostics;
using System.Dynamic;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.LevelTest
{
    // ReSharper disable once InconsistentNaming
    class GUI
    {
        private LevelTest _levelTest;
        private readonly GUIHandler _guiHandler;

        private IFont _fontSmall;
        private IFont _fontMedium;
        private IFont _fontBig;

        private readonly GUIPanel _guiPanel;

        private GUIText _fps, _serverMsg, _waitMsg, _playerCount, _firePos, _earthPos, _airPos, _waterPos;

        private readonly float4 _color1 = new float4(1f, 1f, 1f, 1);
        private readonly float4 _color2 = new float4(1, 1, 1, 1);
        private readonly float4 _color3 = new float4(0, 0.1f, 1, 1);
        
        public GUI(RenderContext rc)
        {
            //Basic Init
            
            _fontSmall = rc.LoadFont("Assets/Lato-Black.ttf", 12);
            _fontMedium = rc.LoadFont("Assets/Lato-Black.ttf", 18);
            _fontBig = rc.LoadFont("Assets/Lato-Black.ttf", 40);

            _guiHandler = new GUIHandler();
            _guiHandler.AttachToContext(rc);

            _fps = new GUIText("FPS", _fontMedium, 20, 20, _color2);
            _waitMsg = new GUIText("Waiting for Connections...", _fontBig, 20, 70, _color1);
            _playerCount = new GUIText("Anzahl der Spieler:", _fontMedium, 20, 120, _color1);
            _firePos = new GUIText("Position Feuer unbekannt", _fontMedium, 20, 170, _color2);
            _waterPos = new GUIText("Position Wasser unbekannt", _fontMedium, 20, 220, _color2);
            _airPos = new GUIText("Position Luft unbekannt", _fontMedium, 20, 270, _color2);
            _earthPos = new GUIText("Position Erde unbekannt", _fontMedium, 20, 320, _color2);


           //_serverMsg = new GUIText("Message received:", _fontMedium, 30, 95, _color2);

            ShowGUI();
        }


        public void RenderFps(float fps)
        {
            _fps.Text = "FPS: " + fps;
        }

        /*public void RenderMsg(string serverMsg)
        {
            _serverMsg.Text = "Message received: " + serverMsg; 
            
        }*/

        public void RenderWait(string wait)
        {
            _waitMsg.Text = wait;
            _guiHandler.RenderGUI();
        }

        public void RenderCount(int count)
        {
            _playerCount.Text = "Anzahl der Spieler: " + count;
        }

        public void RenderPlayerPos(float3 posFire, float3 posWater, float3 posAir, float3 posEarth)
        {
            _firePos.Text = "Position Feuer: " + posFire;
            _waterPos.Text = "Position Wasser: " + posWater;
            _airPos.Text = "Position Luft: " + posAir;
            _earthPos.Text = "Position Erde: " + posEarth;
        }

        public void ShowGUI()
        {
            _guiHandler.Clear();
            _guiHandler.Add(_waitMsg);
            _guiHandler.Add(_fps);
            _guiHandler.Add(_playerCount);
        }


        public void Resize()
        {
            _guiHandler.Refresh();
        }
    }
}
