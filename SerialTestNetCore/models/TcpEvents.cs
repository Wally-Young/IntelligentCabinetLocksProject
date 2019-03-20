using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncTcp
{
    /// <summary>
    /// 与客户端的连接已建立事件参数
    /// </summary>
    public class TcpClientConnectedEventArgs : EventArgs
    {
        public TcpClientConnectedEventArgs(TcpClient tcpClient)
        {
            if(tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            this.TcpClient = tcpClient;
        }
        public TcpClient TcpClient { get; private set; }
    }

    /// <summary>
    /// 与客户端的连接已断开事件参数
    /// </summary>
    public class TcpClientDisconnectedEventArgs : EventArgs
    {
        public TcpClientDisconnectedEventArgs(TcpClient tcpClient)
        {
            if(tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            this.TcpClient = tcpClient;
        }
        public TcpClient TcpClient { get; private set; }
    }

    /// <summary>
    /// 接收到数据报文事件参数
    /// </summary>
    /// <typeparam name="T">报文类型</typeparam>
    public class TcpDatagramReceivedEventArgs<T> : EventArgs
    {
        public TcpDatagramReceivedEventArgs(TcpClient tcpClient, T datagram)
        {
            TcpClient = tcpClient;
            Datagram = datagram;
        }

        public TcpClient TcpClient { get; private set; }
        public T Datagram { get; set; }
    }

    /// <summary>
    /// 与服务器的连接发生异常事件参数
    /// </summary>
    public class TcpServerExceptionOccurredEventArgs : EventArgs
    {
        public TcpServerExceptionOccurredEventArgs(IPAddress[] ipAddresses, int port, Exception innerException)
        {
            if (ipAddresses == null)
                throw new ArgumentNullException("ipAddresses");

            this.Addresses = ipAddresses;
            this.Port = port;
            this.Exception = innerException;
        }

        public IPAddress[] Addresses { get; private set; }
        public int Port { get; private set; }
        public Exception Exception { get; private set; }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var item in Addresses)
            {
                s = s + item.ToString() + ',';
            }
            s = s.TrimEnd(',');
            s = s + ":" + Port.ToString(CultureInfo.InvariantCulture);

            return s;
        }
    }

    /// <summary>
    /// 与服务器的连接已建立事件参数
    /// </summary>
    public class TcpServerConnectedEventArgs : EventArgs
    {
        public TcpServerConnectedEventArgs(IPAddress[] ipAddresses, int port)
        {
            if (ipAddresses == null)
                throw new ArgumentNullException("ipAddresses");

            this.Addresses = ipAddresses;
            this.Port = port;
        }

        public IPAddress[] Addresses { get; private set; }
        public int Port { get; private set; }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var item in Addresses)
            {
                s = s + item.ToString() + ',';
            }
            s = s.TrimEnd(',');
            s = s + ":" + Port.ToString(CultureInfo.InvariantCulture);

            return s;
        }
    }

    /// <summary>
    /// 与服务器的连接已断开事件参数
    /// </summary>
    public class TcpServerDisconnectedEventArgs : EventArgs
    {
        public TcpServerDisconnectedEventArgs(IPAddress[] ipAddresses, int port)
        {
            if (ipAddresses == null)
                throw new ArgumentNullException("ipAddresses");

            this.Addresses = ipAddresses;
            this.Port = port;
        }

        public IPAddress[] Addresses { get; private set; }
        public int Port { get; private set; }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var item in Addresses)
            {
                s = s + item.ToString() + ',';
            }
            s = s.TrimEnd(',');
            s = s + ":" + Port.ToString(CultureInfo.InvariantCulture);

            return s;
        }
    }

}
