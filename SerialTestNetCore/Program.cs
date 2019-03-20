using AsyncTcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;
using System.Text;
using flyfire.IO.Ports;

namespace SerialTestNetCore
{
    class Program
    {

        #region Fields
        static bool isPortRecv = false;
        static bool isRequest = false;
        private static readonly object objPortLock = new object();
        private static readonly object objReceivedLock = new object();
        private static readonly object objFlagLock = new object();
        static int recvData = 0;
        //static SerialDevice mySer = new SerialDevice("/dev/serial0", BaudRate.B115200);
        //static CustomSerialPort mySer = new CustomSerialPort("/dev/COM1", 9600);
        static CustomSerialPort mySer = new CustomSerialPort("COM3", 9600);
        static List<Logical> logicals = new List<Logical> { };
        #endregion

        static void Main(string[] args)
        {
            #region Fileds
            string serverIP;
            AsyncTcpServer server = null;

            #endregion

            #region Initial
            string[] names= CustomSerialPort.GetPortNames();
            foreach (var item in names)
            {
                Console.WriteLine(item);
            }
            mySer.ReceivedEvent += MySer_DataReceived;
            List<string> ipv4 = new List<string>();

            #region 初始化服务器IP
            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipv4.Add(ip.ToString());


                }
            }
            serverIP = ipv4.Last();
            Console.WriteLine(serverIP.ToString());
            #endregion

            #region 启动服务器
            try
            {
                Console.WriteLine("sever is starting.....");
                //Socket s = new Socket(AddressFamily.);
                //server = new AsyncTcpServer(IPAddress.Parse("192.168.2.20"), 10001);
                server = new AsyncTcpServer(IPAddress.Parse(serverIP), 10001);
                server.ClientConnected += new EventHandler<TcpClientConnectedEventArgs>(Server_ClientConnected);
                server.ClientDisconnected += new EventHandler<TcpClientDisconnectedEventArgs>(Server_ClientDisconnected);
                server.PlaintextReceived += new EventHandler<TcpDatagramReceivedEventArgs<string>>(Server_PlaintextReceived);
                server.Start();
                //Append(richTextBox1, $"started server @ {serverIP} : {serverPort}");
                Console.WriteLine($"server is started--{serverIP}:{10001}");
            }

            catch (Exception ex)
            {
                //MessageBox.Show($"failed to start server!\n{ex.ToString()}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Append(richTextBox1, $"start server failed with error:{ex}");
                Console.WriteLine(ex.Message);
            }
            #endregion

            #region 启动串口


            try
            {   if(mySer!=null)
                    mySer.Open();
                Console.WriteLine("Serial Open Success!");
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message); 
            }
            #endregion

            #region 初始化逻辑
            logicals.Clear();
            logicals.Add(new Logical("S-O-D-R", 1,"F", "S"));
            logicals.Add(new Logical("O-S-D-R", 2, "2", "1"));
            logicals.Add(new Logical("T-O-D-R",1, "F", "S"));
            logicals.Add(new Logical("O-T-D-R",2, "2", "1"));
            logicals.Add(new Logical("A-O-D-R", 1,"F", "S"));
            logicals.Add(new Logical("A-R-D-R", 1,"S", "F"));
            logicals.Add(new Logical("A-C-D-R", 2, "S", "F"));
            //logicals.Add(new Logical("C-R-D-R", 1,"S", "F"));
            #endregion

            #endregion

            while (true)
            {
               Thread.Sleep(1000);
               //Console.WriteLine($"DateTimeNow is{DateTime.Now.ToString()}");
            }
        }

        #region ServerEvent
        private static void Server_PlaintextReceived(object sender, TcpDatagramReceivedEventArgs<string> e)
        {
            isRequest = true;
            Console.WriteLine($"recv from { e.TcpClient.Client.RemoteEndPoint.ToString()}: {e.Datagram}");     
            if(e.Datagram.Length==10)
            {
              
                //【1】定义变量
                int index;
                string returnStr = "";

                //【2】数据处理
                try
                {
                    int.TryParse(e.Datagram.Substring(5, 3), out index);
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                    byte[] msg = Encoding.Default.GetBytes("ErrorRequest!");
                    e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
                    return;
                }
                
                string cmd = e.Datagram.Remove(5, 3);

                if(mySer.IsOpen)
                {
                    int retries = 0;
                    int retries1 = 0;
                    bool ischecked = false;
                    //【3】发送串口数据
                    foreach (var logical in logicals)
                    {
                                           
                        if (logical.CmddData == cmd)
                        {                      

                            while (!ischecked && retries < 5)
                            {
                                lock (objPortLock)
                                {
                                    CabinetLock.DealWithLocks(index, cmd, mySer);
                                }
                                //【4】等待串口返回数据
                                while (!isPortRecv&& retries1 < 3)
                                {
                                    Thread.Sleep(200);
                                    retries1++;
                                    Console.WriteLine($"waiting to port data-{retries}");
                                    
                                }
                                retries1 = 0;
                                lock (objFlagLock)
                                {
                                    isPortRecv = false;
                                }
                                //处理返回结果
                                if (GetReceiveData() == logical.Result)
                                {
                                    retries = 0;
                                   

                                 
                                    ischecked = true;
                                    break;
                                }
                                else
                                {
                                    retries++;
                                    Thread.Sleep(500);
                                }
                            }
                            if(ischecked)
                            {
                                returnStr = SendMessage(cmd, logical.Success, index);
                                byte[] msg = Encoding.UTF8.GetBytes(returnStr);
                                e.TcpClient.GetStream().Write(msg, 0, msg.Length);
                            }
                            else
                            {
                                returnStr = SendMessage(cmd, logical.Error, index);
                                byte[] msg = Encoding.UTF8.GetBytes(returnStr);
                                e.TcpClient.GetStream().Write(msg, 0, msg.Length);
                            }
                            break;
                        }                       
                    }
                    if(cmd== "C-R-D-R")
                    {
                        string strsend = "060-LT-005-012-020";
                        byte[] msg = Encoding.Default.GetBytes(strsend);
                        e.TcpClient.GetStream().Write(msg, 0, msg.Length);
                    }
                }
                else
                {
                    mySer.Open();
                    byte[] msg = Encoding.Default.GetBytes("SerialComunicationError!");

                    e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
                }
            

            }
            else
            {
                byte[] msg = Encoding.Default.GetBytes("ErrorRequest!");
   
                e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
            }
        }

        private static void Server_ClientDisconnected(object sender, TcpClientDisconnectedEventArgs e)
        {
            //Append(richTextBox1, $"client [{e.TcpClient.Client.RemoteEndPoint.ToString()}] disconnected");
            Console.WriteLine($"client [{e.TcpClient.Client.RemoteEndPoint.ToString()}] disconnected");
        }

        private static void Server_ClientConnected(object sender, TcpClientConnectedEventArgs e)
        {
          
            Console.WriteLine($"client [{e.TcpClient.Client.RemoteEndPoint.ToString()}] connected");
        }
        #endregion


        #region SerialDealMethod

        /// <summary>
        /// 串口接收数据
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private static void MySer_DataReceived(object arg1, byte[] arg2)
        {
            if(isRequest)
            {
                Console.WriteLine($"Port Received: {BitConverter.ToString(arg2)}");
                int data = 0;
                if (arg2.Count() == 5)
                {
                    switch (arg2[3])
                    {
                        case 0x00: data = 1; break;//开
                        case 0x11: data = 2; break;//关
                        default:
                            data = 3; break;//出错

                    }
                }
                SetReceiveData(data);
                lock (objFlagLock)
                {
                    isPortRecv = true;
                }
            }
 
        }

        ///
        private static string SendMessage(string msg,string result,int code)
        {

            msg= msg.Insert(5, code.ToString("000"));
            msg = msg.Remove(msg.Length - 1, 1) + result;
            return msg;
        }

        public static void  SetReceiveData(int param)
        {
            lock (objReceivedLock)
            {
                recvData = param;
            }
        }


        public static int  GetReceiveData()
        {
            lock (objReceivedLock)
            {
                return recvData;
            }
        }

        #endregion
    }
}
