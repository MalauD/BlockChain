using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockChainLibrary;
using static BlockChainLibrary.Block;
using static BlockChainLibrary.BlockChain;
using static BlockChainLibrary.Block.Ledger;
using static BlockChainLibrary.MessageUDP;
using static BlockChainLibrary.Command;
using static BlockChainLibrary.Function;
using System.Net.Sockets;
using System.Threading;
using System.Net;


namespace Pool
{
    class ProgramPool
    {

        static int poolPort = 1112;

        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 10000;
        
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];


        static BlockChain latestChain;

        static void Main(string[] args)
        {
            Console.WriteLine("Creating BlockChain ...");
            latestChain = new BlockChain();
            latestChain.CreateGenesis();
            latestChain.AddBlock(new Block(1, "27/01", new Ledger(), ""));
            Console.WriteLine("BlockChain Created");

            Console.WriteLine("Setup Pool ...");
            Console.WriteLine("Connect To tracker");
            ConnectToTracker();
            SetupServer();
            
            




            Console.ReadLine();
            CloseAllSockets();
        }




        private static void SetupServer()
        {
            try
            {
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, poolPort));
                
                Console.Clear();
                Console.WriteLine("Pool Ok");
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

            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Miner Connected - {0} Clients", clientSockets.Count);
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
                Console.WriteLine("Miner Deconnecté - {0} Clients", clientSockets.Count);
                return;
            }

            Console.WriteLine("Message recu");

            MessageUDP msg = new MessageUDP(){
                Data = buffer
            };
            Object obj = Function.Deserialize(msg);
            ProcessMessage(obj,current);


            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);

        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }



        static private void SendCommand(Object Obj,Socket socket)
        {
            var msg = Function.Serialize(Obj);
            socket.SendBufferSize = 20000;
            socket.Send(msg.Data);
        }


     
        static private void ProcessMessage(object objectReceived,Socket socket)
        {
            if (objectReceived is Command)
            {
                Command cmd = objectReceived as Command;
                switch (cmd.CommandId)
                {
                    case 0:
                        Console.WriteLine("Sending BlockChain");
                        SendCommand(latestChain,socket);
                        break;

                }
            }
            else if (objectReceived is BlockChain)
            {

                latestChain = objectReceived as BlockChain;
                if (!latestChain.IsGoodChain())
                {
                    //latestChain = null;
                }
                Console.WriteLine("Mined Time" + latestChain.GetLatest().MinedTime);
            }
        }












        static string trackerIp = "127.0.0.1";
        static int trackerPort = 1114;

        private static Socket _clientSocket;


        private static void ConnectToTracker()
        {

            Console.WriteLine("Try to connnect to the tracker: " + trackerIp);

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(trackerIp), trackerPort);

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket.BeginConnect(ip, new AsyncCallback(ConnectCallBack), null);

            SendCommandNode(new Command(2));


        }

        static byte[] recbuffer = new byte[20000];
        static Thread recevoir;

        private static void ConnectCallBack(IAsyncResult ar)
        {
            _clientSocket.EndConnect(ar);

            Console.WriteLine("Connected to tracker " + trackerIp);
               
            

            recevoir = new Thread(new ThreadStart(Receive));
            recevoir.Start();
        }


        private static void Receive()
        {
            _clientSocket.BeginReceive(recbuffer, 0, recbuffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveCallbackNode), null);
        }


        private static void ReceiveCallbackNode(IAsyncResult ar)
        {
            int ren = _clientSocket.EndReceive(ar);

            MessageUDP msg = new MessageUDP()
            {
                Data = recbuffer
            };

            //Console.WriteLine(Encoding.Default.GetString(msg.Data));

            Object obj = Function.Deserialize(msg);
            ProcessMessageNode(obj);

            _clientSocket.BeginReceive(recbuffer, 0, recbuffer.Length, SocketFlags.None,
                new AsyncCallback(ReceiveCallbackNode), null);

        }



        static private void SendCommandNode(Object Order)
        {
            var msg = Function.Serialize(Order);
            _clientSocket.BeginSend(msg.Data, 0, msg.Data.Length, SocketFlags.None, new AsyncCallback(SendCallbackNode), null);

        }
        private static void SendCallback(IAsyncResult ar)
        {
            _clientSocket.EndSend(ar);
        }
        private static void SendCallbackNode(IAsyncResult ar)
        {
            _clientSocket.EndSend(ar);
        }





        static List<IPEndPoint> nodeList = new List<IPEndPoint>();



        private static void ProcessMessageNode(object objectReceived)
        {

            if (objectReceived is List<IPEndPoint>)
            {

                nodeList = objectReceived as List<IPEndPoint>;

                Console.WriteLine("Node list received");
                    

            }
        }

    }

}
