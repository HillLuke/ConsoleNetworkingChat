﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Server
{
    class Program
    {

        static List<TcpClient> _clients = new List<TcpClient>();
        static readonly object _lock = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting server");

            IPAddress localAddress = IPAddress.Parse("127.0.0.1");
            TcpListener server = new TcpListener(localAddress, 8001);

            server.Start();
            server.BeginAcceptTcpClient(new AsyncCallback(acceptTCP), server);

            Console.WriteLine("Server started");
            Console.Read();
        }

        private static void acceptTCP(IAsyncResult asyncResult)
        {
            TcpListener listener = (TcpListener)asyncResult.AsyncState;
            listener.BeginAcceptTcpClient(new AsyncCallback(acceptTCP), listener);

            TcpClient client = listener.EndAcceptTcpClient(asyncResult);
            _clients.Add(client);

            Thread thread = new Thread(handler);
            thread.Start(_clients.Count-1);

            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");
        }

        private static void handler(object o)
        {
            int id = (int)o;
            TcpClient client;

            lock (_lock) client = _clients[id];

            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int byte_count = stream.Read(buffer, 0, buffer.Length);

                if (byte_count == 0)
                {
                    break;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
                broadcast(data);
                Console.WriteLine(data);
            }

            lock (_lock) _clients.RemoveAt(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        public static void broadcast(string data)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data + Environment.NewLine);

            lock (_lock)
            {
                foreach (TcpClient c in _clients)
                {
                    NetworkStream stream = c.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}