using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Fusee.Engine;
using Fusee.Math;

namespace Examples.LevelTest
{
    class Player
    {
        private static string[] _elements = {"fire", "water" ,"earth", "air"};

        private static int i = 0;
        private readonly IPAddress _ipAddress;
        private float3 _velocity;
        private RigidBody _rigidBody;
        public Player(string id, float3 playerPos, IPAddress ipAddress)
        {
            
            Id = id;
            PlayerPos = playerPos;
            ElementString = _elements[i++];
            _ipAddress = ipAddress;
            IsActive = true;
            if (i > 3) i = 0;
            _rigidBody = LevelTest.LevelPhysic.InitSphere(playerPos);

        }
        public Player(string id)
        {
            Id = id;
            ElementString = _elements[i++];
            IsActive = true;
            if (i > 3) i = 0;
        }
        public Player(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
            Id = ipAddress.ToString();
            IsActive = true;
        }
        public string Id { get; private set; } 

        public float3 PlayerPos { get; set; }

        public float3 NewPlayerPos { get; set; }

        public bool IsActive { get; set; }

        public float2 SensorDataFloat2 { get; set; }

        // fire, water, earth, air
        public string ElementString { get; private set; }

        public IPAddress IpAddress
        {
            get { return _ipAddress; }
        }

        public void Move(float3 veFloat3)
        {
            PlayerPos = NewPlayerPos;

            _velocity = (veFloat3)/(float)(1/Time.Instance.FramePerSecond);
            
            
            _rigidBody.LinearVelocity = new float3(_velocity);
            NewPlayerPos = _rigidBody.Position;
        }

        
    }
}
