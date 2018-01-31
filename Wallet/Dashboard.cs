using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BlockChainLibrary;
using static BlockChainLibrary.Block;
using static BlockChainLibrary.BlockChain;
using static BlockChainLibrary.Block.Ledger;
using static BlockChainLibrary.MessageUDP;
using static BlockChainLibrary.Command;
using static BlockChainLibrary.Function;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Wallet
{
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();
        }




        private void InitClientNode()
        {


        }
        static string trackerIp = "192.168.1.235";
        static int trackerPort = 1114;

        private static Socket _clientSocket;

        private void ConnectToTracker()
        {

            toolStripStatusLabel1.Text = "Try to connnect to the tracker: " + trackerIp;

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(trackerIp), trackerPort);

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _clientSocket.BeginConnect(ip, new AsyncCallback(ConnectCallBack), null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Tracker Exception : " + e);

            }
            

            SendCommand(new Command(2));


        }

        static byte[] recbuffer = new byte[20000];
        static Thread recevoir;

        private void ConnectCallBack(IAsyncResult ar)
        {
            _clientSocket.EndConnect(ar);
            Invoke((MethodInvoker)delegate
            {
                toolStripStatusLabel1.Text = "Connected to tracker " + trackerIp;
                Connect.Enabled = false;

            });

            recevoir = new Thread(new ThreadStart(Receive));
            recevoir.Start();
        }


        private void Receive()
        {
            
            try
            {
                _clientSocket.BeginReceive(recbuffer, 0, recbuffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Tracker Exception : " + e);

            }
        }
        bool acceptNode = true;

        private void ReceiveCallback(IAsyncResult ar)
        {
            int ren = _clientSocket.EndReceive(ar);

            if (acceptNode)
            {
                acceptNode = false;
                AcceptNodes();
            }
            

            MessageUDP msg = new MessageUDP()
            {
                Data = recbuffer
            };

            //Console.WriteLine(Encoding.Default.GetString(msg.Data));

            Object obj = Function.Deserialize(msg);
            ProcessMessage(obj);

            try
            {
                _clientSocket.BeginReceive(recbuffer, 0, recbuffer.Length, SocketFlags.None,
                    new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Tracker Exception : " + e);

            }

           

        }



        static private void SendCommand(Object Order)
        {
            var msg = Function.Serialize(Order);
            try
            {
                _clientSocket.BeginSend(msg.Data, 0, msg.Data.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Tracker Exception : " + e);

            }
            

        }
        private static void SendCallback(IAsyncResult ar)
        {
            _clientSocket.EndSend(ar);
        }





        static List<IPEndPoint> nodeList = new List<IPEndPoint>();



        private void ProcessMessage(object objectReceived)
        {

            if (objectReceived is List<IPEndPoint>)
            {

                nodeList = objectReceived as List<IPEndPoint>;
                Invoke((MethodInvoker)delegate
                {
                    toolStripStatusLabel1.Text = "Node list received";
                    listBox1.Items.Clear();
                    foreach (var node in nodeList)
                    {
                        listBox1.Items.Add(node.Address.ToString());
                        node.Port = nodePort;
                    }
                });
                if (init)
                {
                    init = false;
                    ConnectToNodes();
                }
                else
                {

                }
            }        
            
        }

        bool init = true;

        private void Dashboard_Load(object sender, EventArgs e)
        {
            InitClientNode();
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            ConnectToTracker();
        }

        int nodePort = 1120;

        List<Socket> nodes = new List<Socket>();

        private void ConnectToNodes()
        {
            foreach (var ip in nodeList)
            {   
                

                Socket sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var v = false;
                foreach(var s in nodes)
                {
                    if(sc == s)
                    {
                        v = true;
                    }
                }
                if (v)
                    continue;

                try
                {
                    sc.BeginConnect(ip, new AsyncCallback(NodeConnectCallBack), null);
                }
                catch (Exception e)
                {
                    MessageBox.Show("Connection to node Exception : " + e);

                }

                
                nodes.Add(sc);
            }
            Invoke((MethodInvoker)delegate
            {
                toolStripStatusLabel1.Text = "Connected to All node ";
            });
            
        }

        Socket nodesSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        const int BUFFER_SIZE = 10000;
        private byte[] buffer = new byte[BUFFER_SIZE];

        private void AcceptNodes()
        {
            try
            {
                nodesSocket.Bind(new IPEndPoint(IPAddress.Any, nodePort));
                nodesSocket.Listen(0);
                nodesSocket.BeginAccept(AcceptNodesCallback, null);
            }
            catch (Exception e)
            {
                MessageBox.Show("Accept to node Exception : " + e);

            }

            
        }

        private void AcceptNodesCallback(IAsyncResult ar)
        {
            Socket socket;
            

            try
            {
                socket = nodesSocket.EndAccept(ar);
                socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveNodesCallback, socket);
            }
            catch (Exception e)
            {
                MessageBox.Show("Accept node Exception : " + e);

            }
            
        }


        private void ReceiveNodesCallback(IAsyncResult ar)
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
                nodes.Remove(current);

                return;
            }
            MessageUDP msg = new MessageUDP()
            {
                Data = buffer
            };
            Object obj = Function.Deserialize(msg);
            ProcessNodesMessage(obj, current);
            try
            {
                current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
            }
            catch (Exception e)
            {
                MessageBox.Show("Receive from node Exception : " + e);

            }

            

        }

        private void ProcessNodesMessage(object obj, Socket current)
        {
            if (obj is String)
            {
                string s = obj as String;
            
            Invoke((MethodInvoker)delegate
            {
               listBox2.Items.Add(s);
            });
            
            }
        }

        private void NodeConnectCallBack(IAsyncResult ar)
        {
            try
            {
                _clientSocket.EndConnect(ar);
            }
            catch (Exception e)
            {
                MessageBox.Show("End connect node Exception : " + e);

            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var node in nodes)
            {
                SendCommand(textBox1.Text, node);
            }
        }
        static private void SendCommand(Object Order, Socket socket)
        {
            var msg = Function.Serialize(Order);
            //socket.BeginSend(msg.Data, 0, msg.Data.Length, SocketFlags.None, new AsyncCallback(SendNodesCallback), null);
            socket.Send(msg.Data, 0, msg.Data.Length, SocketFlags.None);
        }

        private static void SendNodesCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            
            try
            {
                socket.EndSend(ar);
            }
            catch (Exception e)
            {
                MessageBox.Show("Receive from node Exception : " + e);

            }
        }
    }
}
