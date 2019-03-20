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
using tools;
using System.IO;

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
        static CustomSerialPort mySer = null;
        //static CustomSerialPort mySer = new CustomSerialPort("COM3", 9600);
        static List<Logical> logicals = new List<Logical> { };
        static string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

       static int totalNum;
       static string rangeRule;
       static int rownum;
       static int columnum;
       static int usedcabinet;
        #endregion

        static void Main(string[] args)
        {
            #region Fileds
            string serverIP;
            AsyncTcpServer server = null;

            #endregion

            #region Initial

            #region 读取箱体配置文件
            try
            {
                ConfigLoad conip = new ConfigLoad(configPath);
                totalNum = conip.GetIntValue("totalNum");
                rangeRule = conip.GetStringValue("rangeRule");
                rownum = conip.GetIntValue("rowNum");
                columnum = conip.GetIntValue("colunNum");
                usedcabinet = conip.GetIntValue("usedNum");
            }
            catch (Exception ex)
            {
                Logger.Custom("log/", $"load config failed with error:{ex}");
            }
            #endregion

            #region 启动服务器
            try
            {
                Console.WriteLine("sever is starting.....");
                Logger.Custom("log/", "sever is starting.....");
                server = new AsyncTcpServer(IPAddress.Parse("192.168.2.20"), 10001);
                //server = new AsyncTcpServer(IPAddress.Parse(serverIP), 10001);
                server.ClientConnected += new EventHandler<TcpClientConnectedEventArgs>(Server_ClientConnected);
                server.ClientDisconnected += new EventHandler<TcpClientDisconnectedEventArgs>(Server_ClientDisconnected);
                server.PlaintextReceived += new EventHandler<TcpDatagramReceivedEventArgs<string>>(Server_PlaintextReceived);
                server.Start(); 
                Console.WriteLine($"server is started");
                Logger.Custom("log/", "server is started");
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Logger.Custom("log/", $"server starting excetpiton:{ex.Message}");
            }
            #endregion

            #region 启动串口
            string[] names = CustomSerialPort.GetPortNames();
            foreach (var item in names)
            {
                Console.WriteLine(item);
            }
            try
            {
                mySer= new CustomSerialPort("/dev/COM1", 9600);
                //mySer = new CustomSerialPort("COM1", 9600);
                if (mySer != null)
                {
                    mySer.ReceivedEvent += MySer_DataReceived;
                    if(mySer.Open())
                    {
                        Console.WriteLine("Serial Open Success!");
                        Logger.Custom("log/", $"Serial Open Success!");
                    }
                }     
               
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Logger.Custom("log/", $"Serial Opening exception:{ex.Message}");
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
                //delete threedays ago logger
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second < 5)
                {
                    //删除文件
                    for (int i = 0; i < Directory.GetFiles("log").ToList().Count; i++)
                    {
                        try
                        {

                            string filestr = Directory.GetFiles("log")[i];
                            string date = filestr.Substring(4, filestr.Length - 8);
                            DateTime recoderdate = Convert.ToDateTime(date);
                            TimeSpan timespan = DateTime.Now - recoderdate;
                            if (timespan > new TimeSpan(72, 0, 0))
                            {
                                File.Delete(Directory.GetFiles("log")[i]);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);

                        }

                    }
                }
            }
        }

        #region ServerEvent
        private static void Server_PlaintextReceived(object sender, TcpDatagramReceivedEventArgs<string> e)
        {
            isRequest = true;
            Console.WriteLine($"recv from { e.TcpClient.Client.RemoteEndPoint.ToString()}: {e.Datagram}");
            Logger.Custom("log/", $"recv from { e.TcpClient.Client.RemoteEndPoint.ToString()}: {e.Datagram}");
            #region 指令正确时
            if (e.Datagram.Length==10)
            {

                #region//【1】定义变量
                int index;
                string returnStr = "";
                #endregion

                #region//【2】数据处理
                try
                {
                    int.TryParse(e.Datagram.Substring(5, 3), out index);
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                    Logger.Custom("log/", $"Data Received from { e.TcpClient.Client.RemoteEndPoint.ToString()} exception：{ex.Message}");
                    byte[] msg = Encoding.Default.GetBytes("ErrorRequest!");
                    e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
                    return;
                }
                
                string cmd = e.Datagram.Remove(5, 3);
                #endregion


                #region【3】 处理逻辑
                if (mySer != null&& mySer.IsOpen)
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
                                while (!isPortRecv && retries1 < 3)
                                {
                                    Thread.Sleep(100);
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
                                    Thread.Sleep(300);
                                }
                            }
                            if(ischecked)
                            {
                                returnStr = SendMessage(cmd, logical.Success, index);
                                byte[] msg = Encoding.UTF8.GetBytes(returnStr);
                                e.TcpClient.GetStream().Write(msg, 0, msg.Length);
                            }
                            else if(!mySer.IsOpen)
                            {
                                
                                byte[] msg = Encoding.UTF8.GetBytes("SerialComunicationError!");
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
                    if (cmd == "C-R-D-R")
                    {
                        string strsend = totalNum.ToString("000") + "-" + rangeRule + "-" + rownum.ToString("000") + "-"
                            + columnum.ToString("000") + "-" + usedcabinet.ToString("000");

                        byte[] msg = Encoding.Default.GetBytes(strsend);
                        e.TcpClient.GetStream().Write(msg, 0, msg.Length);
                    }
                    else if(!logicals.Exists(t=>t.CmddData==cmd))
                    {
                        byte[] msg = Encoding.Default.GetBytes("ErrorRequest!-CMDRulesError");

                        e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
                    }

                }
                #endregion

                #region 串口未打开时的异常处理
                else
                {                   
                    try
                    {                   
                        mySer.Open();
                       // if (mySer.ReceivedEvent
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                        Logger.Custom("log/", $"Serial Opening exception:{ex.Message}");
                    }
                   
                    byte[] msg = Encoding.Default.GetBytes("SerialComunicationError!");
                    e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
                }
                #endregion

            }
            #endregion

            #region 指令异常时
            else
            {
                byte[] msg = Encoding.Default.GetBytes("ErrorRequest!-CMDLengthError");
   
                e.TcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
            }
            #endregion
        }

        private static void Server_ClientDisconnected(object sender, TcpClientDisconnectedEventArgs e)
        {
           
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
                Logger.Custom("log/", $"Port Received: { BitConverter.ToString(arg2)}");
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
