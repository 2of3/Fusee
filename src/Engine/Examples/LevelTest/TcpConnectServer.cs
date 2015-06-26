﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Examples.LevelTest
{
    public class ThreadPoolTcpSrvr
    {
        private TcpListener _listener;
        private List<TcpConnection> _connections;



        //Make List accessable in other classes
        public List<TcpConnection> GetConnections()
        {
            return _connections;
        }

        //Get IP Address
        public static string IpList()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }

        public ThreadPoolTcpSrvr()
        {
            _connections = new List<TcpConnection>();
        }
        
        public void StartListening()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(IpList()), 3000);
            
            _listener = new TcpListener(endpoint);
            _listener.Start();

            Console.WriteLine("The local End point is  :" +
                              _listener.LocalEndpoint);

            Console.WriteLine("Waiting for clients...");
            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();
                
                var newconnection = new TcpConnection(this, client);
                newconnection.ThreadListener = _listener;
                lock (_connections)
                {
                    _connections.Add(newconnection);
                }
                ThreadPool.QueueUserWorkItem(newconnection.HandleConnection);
            }            
        }
    
    }

    public class TcpConnection
    {
        public TcpListener ThreadListener;
        public string Message = "";
        private ThreadPoolTcpSrvr _tpts;

        private IPAddress _address;
        private TcpClient _client;

        //constructor _tpts
        public TcpConnection(ThreadPoolTcpSrvr tcpSrvr, TcpClient client)
        {
            _client = client;
            _address = ((IPEndPoint)_client.Client.RemoteEndPoint).Address; //IP Address 1 //TODO: _address is checked as long as ns.CanRead...
            _tpts = tcpSrvr;
        }

        public IPAddress Address
        {
            get { return _address; }
        }

        public void HandleConnection(object dummy)
        {
            StringBuilder RecvMessage;
            int recv;
            byte[] data = new byte[1024];

            NetworkStream ns = _client.GetStream();
            Console.WriteLine("New client accepted"); //": {0} active connections");

            List<Player> tmpPlayers = LevelTest.GetPlayerList();
            String tmpElement = "ERROR";
            foreach (var tmpPlayer in tmpPlayers)
            {
                if (tmpPlayer.IpAddress.Equals(Address))
                {
                    tmpElement = tmpPlayer.ElementString;
                }
               
            }

            /*const */ string element = tmpElement;
            Console.WriteLine("~~~~ THIS IS ELEMENT:" + element);
            data = Encoding.ASCII.GetBytes(element);
            ns.Write(data, 0, data.Length);
            //ns.Flush();
            RecvMessage = new StringBuilder();
            int iMsgEnd = 0;

            while (ns.CanRead && _client.Connected)
            {
                try //TODO: other way to prevent from IOExcaption?
                {
                    recv = ns.Read(data, 0, data.Length);
                    //TODO: if client disconnects --> IOExeption, fix it (maybe client.Close() in the Android App!
                    iMsgEnd = RecvMessage.Length;
                    RecvMessage.AppendFormat("{0}", Encoding.ASCII.GetString(data, 0, recv));
                    
                    for (; iMsgEnd < RecvMessage.Length; iMsgEnd++)
                    {
                        if (RecvMessage[iMsgEnd] == ';') //Protocol; in case server receives incomplete data
                        {
                            Message = RecvMessage.ToString(0, iMsgEnd); //Message is now in List "Connections"
                            RecvMessage.Remove(0, iMsgEnd + 1);
                            Console.WriteLine(Message);
                        }
                    }
                }
                catch (System.IO.IOException ex)
                {
                    Console.WriteLine(ex.ToString());
                    break;
                }
            }
            //locks other threads
            lock (_tpts.GetConnections())
            {
                _tpts.GetConnections().Remove(this);
            }
            ns.Close();
            _client.Close();
            Console.WriteLine("Client disconnected");
        }
    }
}