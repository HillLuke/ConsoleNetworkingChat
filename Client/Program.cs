﻿ using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using shared;

namespace Client
{
    class Program
    {
        static TcpClient _client;
        static IPAddress _ip;

        static string _ID;
        static string _username;
        static int _port;
        static bool _isConnected;
        static Queue<string> _messageQueue = new Queue<string>();

        static Thread _recieveData;
        static Thread _sendData;
        static Thread _handleInput;


        static readonly object _messageQueueLock = new object();

        static void Main(string[] args)
        {
            _ID = Guid.NewGuid().ToString();

            Console.WriteLine("Enter Username");
            _username = Console.ReadLine();
            Console.WriteLine($"Username set as {_username}");

            _ip = IPAddress.Parse("127.0.0.1");
            _port = 8001;
            _client = new TcpClient();

            try
            {
                _client.Connect(_ip, _port);
                _isConnected = true;
                Console.WriteLine($"Connected as {_ID}");
            }
            catch (Exception)
            {
                Console.WriteLine($"Error connecting");
                Console.ReadLine();
                return;
            }

            _recieveData = new Thread(o => ReceiveData());
            _recieveData.Start(_client);
            _sendData = new Thread(o => SendData());
            _sendData.Start(_client);
            _handleInput = new Thread(o => HandleInput());
            _handleInput.Start(_client);
        }

        static string GetInput()
        {
            return Console.ReadLine();
        }

        static void Close()
        {
            _isConnected = false;
            _recieveData.Interrupt();
            _sendData.Interrupt();
            _handleInput.Interrupt();
            _client.Close();
            Console.WriteLine("Disconnected from server, press any key to exit.");
            Console.ReadKey();
        }

        static void spam(NetworkStream stream)
        {
            while (_isConnected)
            {
                byte[] buffer = Encoding.ASCII.GetBytes($"Hello from {_username}");
                stream.Write(buffer, 0, buffer.Length);
                Thread.Sleep(500);
            }
        }

        static void ReceiveData()
        {
            NetworkStream ns = _client.GetStream();
            byte[] receivedBytes = new byte[1024];
            int byte_count;

            while (_isConnected)
            {
                while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
                {
                    Console.Write(Encoding.ASCII.GetString(receivedBytes, 0, byte_count));
                }
            }
        }

        static void SendData()
        {
            NetworkStream ns = _client.GetStream();

            while (_isConnected)
            {
                if (_messageQueue.Count > 0)
                {
                    IPacket packet = new MessagePacket
                    {
                        Message = _messageQueue.Peek(),
                        From = _username
                    };                                        

                    byte[] jsonBuffer = Encoding.UTF8.GetBytes(packet.Serialize());
                    byte[] lengthBuffer = BitConverter.GetBytes(Convert.ToUInt16(jsonBuffer.Length));

                    byte[] packetBuffer = new byte[lengthBuffer.Length + jsonBuffer.Length];
                    lengthBuffer.CopyTo(packetBuffer, 0);
                    jsonBuffer.CopyTo(packetBuffer, lengthBuffer.Length);

                    ns.Write(packetBuffer, 0, packetBuffer.Length);

                    lock (_messageQueueLock)
                    {
                        _messageQueue.Dequeue();
                    }
                }
            }
        }

        static void HandleInput()
        {
            while (_isConnected)
            {
                string input = GetInput();

                if (string.IsNullOrEmpty(input))
                {
                    Close();
                }

                if (input.Equals("/commands"))
                {

                }
                else
                {
                    lock (_messageQueueLock)
                    {
                        _messageQueue.Enqueue(input);
                    }
                }
            }
        }
    }
}
