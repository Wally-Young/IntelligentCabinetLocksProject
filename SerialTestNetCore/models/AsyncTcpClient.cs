using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncTcp
{
    public class AsyncTcpClient : IDisposable
    {
        #region Fields

        private TcpClient tcpClient;
        private bool disposed = false;
        private int retries = 0;
        bool connected;

        #endregion

        #region Properties

        public bool Connected
        {
            get
            {
                if (tcpClient.Client != null)
                    return tcpClient.Client.Connected;
                else
                    return false;
            }
            private set { Connected = value; }
        }
        public IPAddress[] Addresses { get; private set; }
        public int Port { get; private set; }
        public int Retries { get; set; }
        public int RetryInterval { get; set; }
        public IPEndPoint RemoteIPEndPoint { get { return new IPEndPoint(Addresses[0], Port); } }
        protected IPEndPoint LocalIPEndPoint { get; private set; }
        public Encoding Encoding { get; set; }

        #endregion

        #region Ctors

        public AsyncTcpClient(IPEndPoint remoteEP) : this(new[] {remoteEP.Address}, remoteEP.Port)
        {
        }

        public AsyncTcpClient(IPEndPoint remoteEP, IPEndPoint localEP)
            : this(new[] {remoteEP.Address}, remoteEP.Port, localEP)
        {
        }

        public AsyncTcpClient(IPAddress remoteIPAddress, int remotePort) : this(new[] {remoteIPAddress}, remotePort)
        {
        }

        public AsyncTcpClient(IPAddress remoteIPAddress, int remotePort, IPEndPoint localEP)
            : this(new[] {remoteIPAddress}, remotePort, localEP)
        {
        }

        public AsyncTcpClient(string remoteHostName, int remotePort)
            : this(Dns.GetHostAddresses(remoteHostName), remotePort)
        {
        }

        public AsyncTcpClient(string remoteHostName, int remotePort, IPEndPoint localEP)
            : this(Dns.GetHostAddresses(remoteHostName), remotePort, localEP)
        {
        }

        public AsyncTcpClient(IPAddress[] remoteIPAddresses, int remotePort) : this(remoteIPAddresses, remotePort, null)
        {
        }

        public AsyncTcpClient(IPAddress[] remoteIPAddresses, int remotePort,IPEndPoint localEP)
        {
            this.Addresses = remoteIPAddresses;
            this.Port = remotePort;
            this.LocalIPEndPoint = localEP;
            this.Encoding = Encoding.Default;

            if (this.LocalIPEndPoint != null)
            {
                this.tcpClient = new TcpClient(this.LocalIPEndPoint);
            }
            else
            {
                this.tcpClient = new TcpClient();
            }

            Retries = 3;
            RetryInterval = 5;
        }

        #endregion

        #region Connect
        public AsyncTcpClient Connect()
        {
            if (!Connected)
            {
                tcpClient.BeginConnect(Addresses, Port, HandleTcpServerConnected, tcpClient);
            }
            return this;
        }

        public AsyncTcpClient Close()
        {
            if (Connected)
            {
                retries = 0;
                tcpClient.Close();
                RaiseServerDisconnected(Addresses, Port);
            }
            return this;
        }

        #endregion

        #region Receive

        private void HandleTcpServerConnected(IAsyncResult ar)
        {
            try
            {
                tcpClient.EndConnect(ar);
                RaiseServerConnected(Addresses,Port);
                retries = 0;

                byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
                tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, HandleDatagramReceived, buffer);
            }
            catch (Exception ex)
            {
                //if (retries>0)
                //{
                //   // Logger.Debug(string.Format(CultureInfo.InvariantCulture, "Connect to server with retry {0} failed.",retries));
                //}
                //retries++;
                //if (retries > Retries)
                //{
                //    RaiseServerExceptionOccurred(Addresses, Port, ex);
                //    return;
                //}
                //else
                //{
                //    //Logger.Debug(string.Format(CultureInfo.InvariantCulture,"Waiting {0} seconds before retrying to connect to server.", RetryInterval));
                //    Thread.Sleep(TimeSpan.FromSeconds(RetryInterval));
                //    Connect();
                //    return;
                //}
                RaiseServerExceptionOccurred(Addresses, Port, ex);
            }

           
        }

        private void HandleDatagramReceived(IAsyncResult ar)
        {
            NetworkStream stream = tcpClient.GetStream();

            int numberOfReadBytes = 0;
            try
            {
                numberOfReadBytes = stream.EndRead(ar);
            }
            catch
            {
                numberOfReadBytes = 0;
            }

            if (numberOfReadBytes == 0)
            {
                Close();
                return;
            }

            byte[] buffer = (byte[]) ar.AsyncState;
            byte[] receivedBytes = new byte[numberOfReadBytes];
            Buffer.BlockCopy(buffer, 0, receivedBytes, 0, numberOfReadBytes);
            RaiseDatagramReceived(tcpClient, receivedBytes);
            RaisePlaintextReceived(tcpClient, receivedBytes);

            stream.BeginRead(buffer, 0, buffer.Length, HandleDatagramReceived, buffer);
        }
        
        #endregion

        #region Events

        public event EventHandler<TcpDatagramReceivedEventArgs<byte[]>> DatagramReceived;
        public event EventHandler<TcpDatagramReceivedEventArgs<string>> PlaintextReceived;

        private void RaiseDatagramReceived(TcpClient sender, byte[] datagram)
        {
            if (DatagramReceived != null)
            {
                DatagramReceived(this, new TcpDatagramReceivedEventArgs<byte[]>(sender, datagram));
            }
        }

        private void RaisePlaintextReceived(TcpClient sender, byte[] datagram)
        {
            if (PlaintextReceived != null)
            {
                PlaintextReceived(this,
                    new TcpDatagramReceivedEventArgs<string>(sender,
                        this.Encoding.GetString(datagram, 0, datagram.Length)));
            }
        }

        public event EventHandler<TcpServerConnectedEventArgs> ServerConnected;
        public event EventHandler<TcpServerDisconnectedEventArgs> ServerDisconnected;
        public event EventHandler<TcpServerExceptionOccurredEventArgs> ServerExceptionOccurred;

        private void RaiseServerConnected(IPAddress[] ipAddresses, int port)
        {
            if (ServerConnected != null)
            {
                ServerConnected(this, new TcpServerConnectedEventArgs(ipAddresses, port));
            }
        }

        private void RaiseServerDisconnected(IPAddress[] ipAddresses, int port)
        {
            if (ServerDisconnected != null)
            {
                ServerDisconnected(this, new TcpServerDisconnectedEventArgs(ipAddresses, port));
            }
        }

        private void RaiseServerExceptionOccurred(IPAddress[] ipAddresses, int port, Exception innerException)
        {
            if (ServerExceptionOccurred != null)
            {
                ServerExceptionOccurred(this, new TcpServerExceptionOccurredEventArgs(ipAddresses, port, innerException));
            }
        }

        #endregion

        #region Send

        public void Send(byte[] datagram)
        {

            if (datagram == null)
                throw new ArgumentNullException("datagram");

            if (!Connected)
            {
                RaiseServerDisconnected(Addresses, Port);
                Connected = false;
                throw new InvalidProgramException("This client has not connected to server.");
              
            }

            tcpClient.GetStream().BeginWrite(datagram, 0, datagram.Length, HandleDatagramWritten, tcpClient);
        }

        private void HandleDatagramWritten(IAsyncResult ar)
        {
            ((TcpClient) ar.AsyncState).GetStream().EndWrite(ar); //无法访问已经释放的对象TcpClient
        }

        public void Send(string datagram)
        {
            Send(this.Encoding.GetBytes(datagram));
        }

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    try
                    {
                        Close();

                        if (tcpClient != null)
                        {
                            tcpClient = null;
                        }
                    }
                    catch (SocketException)
                    { 
                    }
                }
                disposed = true;
            }
        }

        #endregion
    }
}
