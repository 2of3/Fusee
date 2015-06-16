using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Fusee.Math;

namespace Examples.LevelTest
{
    class Player
    {
        private static string[] _elements = {"fire", "water", "doof" ,"earth", "air"};

        private static int i = 0;
        private readonly IPAddress _ipAddress;
        public Player(string id, float3 playerPos, int playerNumber, IPAddress ipAddress)
        {
            
            Id = id;
            PlayerPos = playerPos;
            PlayerNumber = playerNumber;
            ElementString = _elements[i++];
            _ipAddress = ipAddress;
            IsActive = true;
        }
        public Player(string id)
        {

            Id = id;
            
            
            ElementString = _elements[i++];
            
            IsActive = true;
        }
        public Player(IPAddress ipAddress)
        {
            _ipAddress = ipAddress;
            Id = ipAddress.ToString();
            IsActive = true;
        }

        public string Id { get; private set; } 

        public string PlayerName { get; set; }

        public float3 PlayerPos { get; set; }

        public bool IsActive { get; set; }

        public float2 SensorDataFloat2 { get; set; }

        public int PlayerNumber { get; private set; }

        // fire, water, earth, air
        public string ElementString { get; private set; }

        public IPAddress IpAddress
        {
            get { return _ipAddress; }
        }

        public void Move(float3 veFloat3)
        {
            PlayerPos += veFloat3;
        }
    }
}
