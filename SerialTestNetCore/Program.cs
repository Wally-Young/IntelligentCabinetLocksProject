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
using System.Collections;

namespace SerialTestNetCore
{
    class Program
    {

        #region Fields
        private static readonly object objPortLock = new object();
        private static readonly object objReceivedLock = new object();
        private static readonly object objFlagLock = new object();
        static CustomSerialPort mySer = null;
        static volatile byte[] sourcebyte = new byte[9];
       
        //static CustomSerialPort mySer = new CustomSerialPort("COM3", 9600);
        static List<Logical> logicals = new List<Logical> { };
        static string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

       
        static string strsend;
        static byte[] readOneall = { 0x80, 0x01, 0x00 ,0x33, 0xB2 };
        static byte[] readsecondAll = { 0x80, 0x02, 0x00, 0x33, 0xB1 };

        #endregion

        static void Main(string[] args)
        {
            #region Fileds
            AsyncTcpServer server = null;
            int totalNum=0,rownum = 0, columnum=0, usedcabinet=0;
            string rangeRule="";
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
             strsend = totalNum.ToString("000") + "-" + rangeRule + "-" + rownum.ToString("000") + "-"
                    + columnum.ToString("000") + "-" + usedcabinet.ToString("000");
            Console.WriteLine(strsend);
            #endregion

            #region 启动服务器
            try
            {
                Console.WriteLine("sever is starting.....");
                Logger.Custom("log/", "sever is starting.....");
                server = new AsyncTcpServer(IPAddress.Parse("192.168.2.20"), 10001);
                //server = new AsyncTcpServer(IPAddress.Parse("127.0.0.1"), 10001);
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
               mySer = new CustomSerialPort("/dev/COM1", 9600);
               //mySer = new CustomSerialPort("COM4", 9600);
                Console.WriteLine($"PortName:{mySer.PortName}");
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
            logicals.Add(new Logical("N-O-D-R", 1,"2", "1",2));
            logicals.Add(new Logical("A-O-D-R", 1,"2", "1",2));
            logicals.Add(new Logical("A-R-D-R", 1,"2", "1",-1));
            logicals.Add(new Logical("C-R-D-R", -1,"-1", "1",-1));
            #endregion

            Console.WriteLine($"状态数组当前的大小时:{sourcebyte.Length}");
            foreach (var item in sourcebyte)
            {
                Console.WriteLine(item.ToString("X"));
            }
            #endregion

            while (true)
            {
               
                lock(objPortLock)
                {
                    
                    
                        mySer.Write(readOneall);
                        Thread.Sleep(500);
                        mySer.Write(readsecondAll);
                        Thread.Sleep(500);
                    
                    
                }
                //delete threedays ago logger
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second <10)
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

           
            Console.WriteLine($"recv from { e.TcpClient.Client.RemoteEndPoint.ToString()}: {e.Datagram}");
            Logger.Custom("log/", $"recv from { e.TcpClient.Client.RemoteEndPoint.ToString()}: {e.Datagram}");

            #region 指令正确时
            if (e.Datagram.Length==10)
            {

                #region【1】定义变量
                int index;
                #endregion

                #region【2】数据处理
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
                    DealwithCmd(cmd, index, e.TcpClient);

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
                Console.WriteLine($"Port Received: {BitConverter.ToString(arg2)}");
                Logger.Custom("log/", $"Port Received: { BitConverter.ToString(arg2)}");
                lock (objReceivedLock)
                {
                  if (arg2.Count() == 7)
                    {
                        Array.Copy(arg2, 4, sourcebyte, 0, 1);
                        Array.Copy(arg2, 3, sourcebyte, 1, 1);
                   
                    }
                 else if (arg2.Count() == 10)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            Array.Copy(arg2, 8 - i, sourcebyte, i + 2, 1);
                        }
                   
                }
                
            }
        }
 
        private static string SendMessage(string result,int code)
        {


            string msg = "D" + code.ToString("000") + "-" + result;
            return msg;
        }

        public static int CheckLockState(int index)
        {
            lock (objReceivedLock)
            {
                if(sourcebyte!=null)
                {
                    if (index <= 12 && index > 0)
                    {
                        BitArray bitArray = new BitArray(sourcebyte.Skip((index - 1) / 8).Take(1).ToArray());
                        return bitArray[(index - 1) % 8] ? 2 : 1;
                    }
                    else if (index > 12&&index<=60)
                    {
                        BitArray bitArray = new BitArray(sourcebyte.Skip((index - 13) / 8 + 2).Take(1).ToArray());
                        return bitArray[(index - 13) % 8] ? 2 : 1;
                    }
                    else
                        return -1;
                    
                }
                else
                {
                    return -1;
                }
              
            }

        }
        public static void SetLockState(int index,bool value)
        {
            lock (objReceivedLock)
            {
                if (sourcebyte != null)
                {
                    if (index <= 12&&index>0)
                    {
                        BitArray bitArray = new BitArray(sourcebyte.Skip((index - 1) / 8).Take(1).ToArray());
                        bitArray.Set(((index - 1) % 8), value);
                    }
                    else if(index>12&&index <= 60)
                    {
                        BitArray bitArray = new BitArray(sourcebyte.Skip((index - 13) / 8 + 2).Take(1).ToArray());
                        bitArray.Set(((index - 13) % 8), value);
                   }
                }
               

            }
        }




        #endregion

        #region  SelfMethod
        public static void  DealwithCmd(string cmd,int index,TcpClient tcpClient)
        {
            int retries = 0;
            bool ischecked = false;
            
            foreach (var logical in logicals)
            {
                if (logical.CmddData == cmd)
                {

                    if(cmd[2]=='O')
                    {
                        while (!ischecked && retries < 5)
                        {
                            lock (objPortLock)
                            {
                                SetLockState(index, true);
                                CabinetLock.DealWithLocks(index, mySer);
                            }

                            Thread.Sleep(1500);
                            int ii = CheckLockState(index);

                            if (sourcebyte != null && logical.Result == ii)
                            {

                                ischecked = true;
                                retries = 0;

                                break;
                            }
                           

                            retries++;
                        }
                        if (ischecked)
                        {
                            string returnStr = SendMessage(logical.Success, index);
                            byte[] msg = Encoding.UTF8.GetBytes(returnStr);
                            tcpClient.GetStream().Write(msg, 0, msg.Length);
                        }
                        else
                        {
                            string returnStr = SendMessage(logical.Error, index);
                            byte[] msg = Encoding.UTF8.GetBytes(returnStr);
                            tcpClient.GetStream().Write(msg, 0, msg.Length);
                            break;
                        }

                        DateTime dateTime = DateTime.Now;
                        Thread.Sleep(3000);
                        while (true && logical.NextState != -1)
                        {
                            try
                            {
                                if ((DateTime.Now - dateTime).Minutes > 30)
                                {
                                    string sendstr = SendMessage("1", index) + "-T";
                                    byte[] msg = Encoding.Default.GetBytes(sendstr);
                                    tcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation                  
                                    break;
                                }
                                if (CheckLockState(index) == logical.NextState)
                                {
                                    string sendstr = SendMessage("2", index) + "-S";
                                    byte[] msg = Encoding.Default.GetBytes(sendstr);
                                    tcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {

                                Logger.Custom("log/", $"CheckDoorState failed,the reason is:{ex.Message}");
                                break;
                            }

                            Thread.Sleep(100);
                        }
                        break;
                    }

                    else if(cmd[2]=='R'&&cmd[0]=='A')
                    {
                        string sendstr = "D" + index.ToString("000") +"-"+ CheckLockState(index).ToString();
                        byte[] msg = Encoding.UTF8.GetBytes(sendstr);
                        tcpClient.GetStream().Write(msg, 0, msg.Length);
                    }
                    else if(cmd[2] == 'R' && cmd[0] == 'C')
                    {
                        byte[] msg = Encoding.UTF8.GetBytes(strsend);
                        tcpClient.GetStream().Write(msg, 0, msg.Length);
                    }
                    
                }
               
            }

            if (!logicals.Exists(t => t.CmddData == cmd))
            {
                byte[] msg = Encoding.Default.GetBytes("ErrorRequest!-CMDRulesError");

                tcpClient.GetStream().Write(msg, 0, msg.Length); // return confirmation
            }

        }
        #endregion 

    }
}
