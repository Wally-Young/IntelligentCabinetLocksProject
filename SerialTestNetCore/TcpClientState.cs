using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace AsyncTcp
{
    public class TcpClientState
    {
        public TcpClientState(TcpClient tcpClient, byte[] buffer)
        {
            if(tcpClient == null)
                throw new ArgumentNullException("tcpClient");
            if(buffer == null)
                throw new ArgumentNullException("buffer");

            this.TcpClient = tcpClient;
            this.Buffer = buffer;
        }
        #region Properties
        public TcpClient TcpClient { get; private set; }
        public byte[] Buffer { get; private set; }
        public NetworkStream NetworkStream
        {
            get { return TcpClient.GetStream(); }
        }
        #endregion
    }
}
