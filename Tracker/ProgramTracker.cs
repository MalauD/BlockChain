using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BlockChainLibrary;
using static BlockChainLibrary.MessageUDP;
using static BlockChainLibrary.Command;
using static BlockChainLibrary.Function;


namespace Tracker
{
    class ProgramTracker
    {
        static int trackerPort = 1114;

        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 10000;

        private static readonly byte[] buffer = new byte[BUFFER_SIZE];


        static void Main(string[] args)
        {
            SetupServer();
            Console.ReadLine();
        }

        private static void SetupServer()
        {
            try
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, trackerPort));

                Console.Clear();
                Console.WriteLine("Tracker ok");
            }
            catch { }
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);

            
        }
        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(ar);

            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            foreach(var client in clientSockets)
            {
                SendCommand(GetIpEndPointClients(client), client);
            }

            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Node Connected - {0} Node", clientSockets.Count);
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            Socket current = (Socket)ar.AsyncState;
            int received;
            try
            {
                received = current.EndReceive(ar);
            }
            catch (SocketException)
            {
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Node Deconnecté - {0} Nodes", clientSockets.Count);
                foreach (var client in clientSockets)
                {
                    SendCommand(GetIpEndPointClients(client), client);
                }
                return;
            }

            Console.WriteLine("Message recu");

            MessageUDP msg = new MessageUDP()
            {
                Data = buffer
            };
            Object obj = Function.Deserialize(msg);
            ProcessMessage(obj, current);


            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);


        }

        static private void SendCommand(Object Obj, Socket socket)
        {
            var msg = Function.Serialize(Obj);
            socket.SendBufferSize = 20000;
            socket.Send(msg.Data);

        }

        static private List<IPEndPoint> GetIpEndPointClients(Socket Me)
        {
            List<IPEndPoint> a = new List<IPEndPoint>();
            Console.WriteLine("Sending Ip:");

            foreach (var socket in clientSockets)
            {
                if (socket != Me)
                {
                    a.Add((IPEndPoint)socket.LocalEndPoint);
                    Console.WriteLine(((IPEndPoint)socket.LocalEndPoint).Address + ":" + ((IPEndPoint)socket.LocalEndPoint).Port);

                }
            }
            Console.WriteLine();
            Console.WriteLine("End of node list");
            return a;
        }

        static private void ProcessMessage(object objectReceived, Socket socket)
        {
            if (objectReceived is Command)
            {
                Command cmd = objectReceived as Command;
                switch (cmd.CommandId)
                {
                    case 2:
                        Console.WriteLine("Sending Node list to client");
                        SendCommand(GetIpEndPointClients(socket), socket);
                        break;

                }
            }
        }
    }
}
