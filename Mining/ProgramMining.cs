using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockChainLibrary;
using static BlockChainLibrary.Block;
using static BlockChainLibrary.Block.Ledger;
using static BlockChainLibrary.BlockChain;
using static BlockChainLibrary.MessageUDP;
using static BlockChainLibrary.Command;
using static BlockChainLibrary.Function;

using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Mining
{
    class ProgramMining
    {
        static string poolIp;
        static int poolPort = 1112;

        private static Socket _clientSocket;





        static BlockChain latestChain;

        static void Main(string[] args)
        {
            Console.WriteLine("Please enter pool ip adress");
            poolIp = Console.ReadLine();

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(poolIp), poolPort);

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket.BeginConnect(ip, new AsyncCallback(ConnectCallBack), null);



            SendCommand(new Command(0));

            Console.ReadLine();

        }

        static byte[] recbuffer = new byte[20000];
        static Thread recevoir;

        private static void ConnectCallBack(IAsyncResult ar)
        {
            _clientSocket.EndConnect(ar);
            Console.WriteLine("Connected to the server");
            recevoir = new Thread(new ThreadStart(Receive));
            recevoir.Start();
        }










        
        static private void Receive()
        {
            _clientSocket.BeginReceive(recbuffer, 0, recbuffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            int ren = _clientSocket.EndReceive(ar);

            MessageUDP msg = new MessageUDP()
            {
                Data = recbuffer
            };

            //Console.WriteLine(Encoding.Default.GetString(msg.Data));

            Object obj = Function.Deserialize(msg);
            ProcessMessage(obj);

            _clientSocket.BeginReceive(recbuffer, 0, recbuffer.Length, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), null);

        }



        static private void SendCommand(Object Order)
        {
            var msg = Function.Serialize(Order);
            _clientSocket.BeginSend(msg.Data, 0, msg.Data.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);

        }

        private static void SendCallback(IAsyncResult ar)
        {
            _clientSocket.EndSend(ar);
        }


        

        static private void ProcessMessage(object objectReceived)
        {
            if (objectReceived is Command)
            {

            }
            else if (objectReceived is BlockChain)
            {

                latestChain = objectReceived as BlockChain;
                if (!latestChain.IsGoodChain())
                {
                    //latestChain = null;
                }
                Console.WriteLine("Block Received start Mining");
                latestChain.GetLatest().MineBlock(17);
                Console.WriteLine("Block # {0} mined", latestChain.GetLatest().Id);
                Console.WriteLine("Send To the pool");
                SendCommand(latestChain);

            }
        }


    }
}
