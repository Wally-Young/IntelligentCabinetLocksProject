using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AsyncTcp
{
    public class AsyncTcpServer : IDisposable
    {
        #region  Fields

        private TcpListener listener;
        public List<TcpClientState> clients; 
        private bool disposed = false;

        #endregion

        #region Ctors

        public AsyncTcpServer(int listenPort) : this(IPAddress.Any, listenPort)
        {
        }

        public AsyncTcpServer(IPEndPoint localEP) : this(localEP.Address, localEP.Port)
        {
        }

        public AsyncTcpServer(IPAddress localIpAddress, int listenPort)
        {
            Address = localIpAddress;
            Port = listenPort;
            this.Encoding = Encoding.Default;
            this.LoginPassword = "umore";
            
            clients = new List<TcpClientState>();

            listener = new TcpListener(Address, Port);
            //listener.AllowNatTraversal(true);
        }
        
        #endregion

        #region Properties
        public bool IsRunning { get; private set; }
        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        public Encoding Encoding { get; set; }
        public string LoginPassword { get; set; }
        #endregion

        #region Server

        public AsyncTcpServer Start()
        {
            if (!IsRunning)
            {
                
                listener.Start();
                listener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), listener);
                IsRunning = true;
            }
            return this;
        }

        public AsyncTcpServer Start(int backlog)
        {
            if (!IsRunning)
            {
                IsRunning = true;
                listener.Start(backlog);
                listener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), listener);
            }
            return this;
        }

        public AsyncTcpServer Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                listener.Stop();

                lock (this.clients)
                {
                    for (int i = 0; i < this.clients.Count; i++)
                    {
                        this.clients[i].TcpClient.Client.Disconnect(false);
                    }
                    this.clients.Clear();
                }
            }
            return this;
        }

        #endregion

        #region Receive

        private void HandleTcpClientAccepted(IAsyncResult ar)
        {
            if (IsRunning)
            {
                TcpListener tcpListener = (TcpListener) ar.AsyncState;

                TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
                byte[] buffer = new byte[tcpClient.ReceiveBufferSize];

                TcpClientState internalClient = new TcpClientState(tcpClient, buffer);
                lock (this.clients)
                {
                    this.clients.Add(internalClient);
                    RaiseClientConnected(tcpClient);
                }

                try
                {
                    NetworkStream networkStream = internalClient.NetworkStream;
                    networkStream.BeginRead(internalClient.Buffer, 0, internalClient.Buffer.Length, HandleDatagramReceived,
                        internalClient);

                    tcpListener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpClientAccepted), ar.AsyncState);
                }
                catch 
                {

                    
                }
               
            }
        }

        private void HandleDatagramReceivedToVerify(IAsyncResult ar)
        {
            if (IsRunning)
            {
                TcpClientState internalClient = (TcpClientState) ar.AsyncState;
                NetworkStream networkStream = internalClient.NetworkStream;

                int numberOfReadBytes = 0;
                try
                {
                    numberOfReadBytes = networkStream.EndRead(ar);
                }
                catch
                {
                    numberOfReadBytes = 0;
                }

                if (numberOfReadBytes == 0)
                {
                    //connection has been closed
                    lock (this.clients)
                    {
                        this.clients.Remove(internalClient);
                        RaiseClientDisconnected(internalClient.TcpClient);
                        return;
                    }
                }

                //received byte and trigger event notification
                byte[] receivedBytes = new byte[numberOfReadBytes];
                Buffer.BlockCopy(internalClient.Buffer, 0, receivedBytes, 0, numberOfReadBytes);
                RaiseDatagramReceived(internalClient.TcpClient, receivedBytes);
                RaisePlaintextReceived(internalClient.TcpClient, receivedBytes);

                if (this.Encoding.GetString(receivedBytes) == LoginPassword)
                {
                    //continue listening for tcp datagram packets
                    networkStream.BeginRead(internalClient.Buffer, 0, internalClient.Buffer.Length,
                        HandleDatagramReceived,
                        internalClient);
                }
                else
                {
                    lock (this.clients)
                    {
                        this.clients.Remove(internalClient);
                        RaiseClientDisconnected(internalClient.TcpClient);
                        return;
                    }
                }
            }
        }

        private void HandleDatagramReceived(IAsyncResult ar)
        {
            if (IsRunning)
            {
                TcpClientState internalClient = (TcpClientState) ar.AsyncState;
                NetworkStream networkStream = internalClient.NetworkStream;

                int numberOfReadBytes = 0;
                try
                {
                    numberOfReadBytes = networkStream.EndRead(ar);
                }
                catch
                {
                    numberOfReadBytes = 0;
                }

                if (numberOfReadBytes == 0)
                {
                    //connection has been closed
                    lock (this.clients)
                    {
                        this.clients.Remove(internalClient);
                        RaiseClientDisconnected(internalClient.TcpClient);
                        return;
                    }
                }

                //received byte and trigger event notification
                byte[] receivedBytes = new byte[numberOfReadBytes];
                Buffer.BlockCopy(internalClient.Buffer, 0, receivedBytes, 0, numberOfReadBytes);
                RaiseDatagramReceived(internalClient.TcpClient, receivedBytes);
                RaisePlaintextReceived(internalClient.TcpClient, receivedBytes);

                //continue listening for tcp datagram packets
                try
                {
                    networkStream.BeginRead(internalClient.Buffer, 0, internalClient.Buffer.Length, HandleDatagramReceived,
                        internalClient);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
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
                //string recv_str = "recv";
                //byte[] recv_t = this.Encoding.GetBytes(recv_str);
                //byte[] recv = { 4, recv_t[0],recv_t[1],recv_t[2],recv_t[3] };
                //this.Send(sender, recv);
            }
        }

        public event EventHandler<TcpClientConnectedEventArgs> ClientConnected;
        public event EventHandler<TcpClientDisconnectedEventArgs> ClientDisconnected;

        private void RaiseClientConnected(TcpClient tcpClient)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, new TcpClientConnectedEventArgs(tcpClient));
            }
            //Send(tcpClient,"Please insert the password:\t\n");
        }

        private void RaiseClientDisconnected(TcpClient tcpClient)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, new TcpClientDisconnectedEventArgs(tcpClient));
            }
        }
        #endregion

        #region Send

        public void Send(TcpClient tcpClient, byte[] datagram)
        {
            if (!IsRunning)
                throw new InvalidProgramException("This TCP server has not been started.");

            if (tcpClient == null)
                throw new ArgumentNullException("tcpClient");

            if (datagram == null)
                throw new ArgumentNullException("datagram");

            tcpClient.GetStream().BeginWrite(datagram, 0, datagram.Length, HandleDatagramWritten, tcpClient);
        }

        private void HandleDatagramWritten(IAsyncResult ar)
        {
            try
            {
                ((TcpClient) ar.AsyncState).GetStream().EndWrite(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void Send(TcpClient tcpClient, string datagram)
        {
            Send(tcpClient, this.Encoding.GetBytes(datagram));
        }

        public void Send(string addr, string datagram)
        {
            for(int i = 0; i < clients.Count; i++)
            {
                if(clients[i].TcpClient.Client.RemoteEndPoint.ToString() == addr)
                {
                    Send(clients[i].TcpClient, datagram);
                    return;
                }
            }
        }

        public void SendAll(byte[] datagram)
        {
            if (!IsRunning)
                throw new InvalidProgramException("This TCP server has not been started.");

            for (int i = 0; i < this.clients.Count; i++)
            {
                Send(this.clients[i].TcpClient, datagram);
            }
        }

        public void SendAll(string datagram)
        {
            if (!IsRunning)
                throw new InvalidProgramException("This TCP server has not been started.");

            SendAll(this.Encoding.GetBytes(datagram));
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
                        Stop();

                        if (listener != null)
                        {
                            listener = null;
                        }
                    }
                    catch(SocketException ex)
                    {
                        
                    }
                }

                disposed = true;
            }
        }
        #endregion

        public void SetLoginPassword(string password)
        {
            this.LoginPassword = password;
        }
    }
}
