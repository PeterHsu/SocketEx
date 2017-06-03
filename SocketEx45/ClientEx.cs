using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace SocketEx
{
    public abstract class ClientEx : IDisposable
    {
        private bool connectOnce = false;
        private Socket m_socket = null;
        private object lockRef = new object();
        private bool m_autoReConnect;
        private DnsEndPoint m_endPoint;
        
        protected abstract void OnSendBase(byte[] msg);
        protected abstract void OnReceiveBase(byte[] msg);

        public event Action<int, string> OnStateChange;

        public ClientExState State { get; set; } = ClientExState.None;

        public ClientEx(DnsEndPoint remoteEndPoint, bool autoReConnect = true)
        {
            this.m_endPoint = remoteEndPoint;
            this.m_autoReConnect = autoReConnect;
        }

        public void Connect()
        {
            if(connectOnce)
            {
                OnStateChange?.Invoke(State == ClientExState.Connected ? 0 : 1, "只能連線一次");
                return;
            }
            connectOnce = true;
            m_Connect();
        }

        private void m_Connect()
        {
            if (m_socket != null)
            {
                lock (lockRef)
                {
                    m_socket.Close(); m_socket.Dispose(); m_socket = null;
                }
            }
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            State = ClientExState.Connecting;
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = m_endPoint;
            args.Completed += OnConnectCompleted;
            lock(lockRef)
            {
                // 如果是false，表示已同步執行連線作業完成，不會觸發Completed事件
                if (!m_socket.ConnectAsync(args)) OnConnectCompleted(null, args);
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(new Byte[Int16.MaxValue], 0, Int16.MaxValue);
                args.Completed += OnReceiveCompleted;
                lock (lockRef)
                {
                    if (!m_socket.ReceiveAsync(args)) OnReceiveCompleted(null,args);
                }
                State = ClientExState.Connected;
                OnStateChange?.Invoke(0, "連線成功");
            }
            else if (e.SocketError != SocketError.ConnectionAborted)
            {
                State = ClientExState.Disconnected;
                OnStateChange?.Invoke(1, e.SocketError.ToString());
                if (m_autoReConnect && connectOnce)
                {
                    // 自動5秒重連
                    Timer timer = new Timer((state) => m_Connect(), null, 5000, -1);
                }
            }
        }
        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                byte[] buffer = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, buffer, 0, buffer.Length);
                Array.Clear(e.Buffer, 0, e.Buffer.Length);
                OnReceiveBase(buffer);
                lock (lockRef)
                {
                    if (m_socket != null)
                    {
                        if (!m_socket.ReceiveAsync(e)) OnReceiveCompleted(null, e);
                    }
                }
            }
            else
            {
                if (e.SocketError == SocketError.ConnectionReset)
                {
                    State = ClientExState.Disconnected;
                    if (m_autoReConnect && connectOnce) m_Connect();
                }
            }
        }
        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success) OnSendBase((byte[])e.UserToken);
            else if (e.SocketError == SocketError.ConnectionReset)
            {
                State = ClientExState.Disconnected;
                if (m_autoReConnect && connectOnce) m_Connect();
            }
        }
        protected void Send(byte[] msg)
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(msg, 0, msg.Length);
            args.UserToken = msg;
            args.Completed += OnSendCompleted;
            lock (lockRef)
            {
                if (!m_socket.SendAsync(args)) OnSendCompleted(null, args);
            }
            
        }
        #region Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    lock (lockRef)
                    {
                        if (m_socket != null)
                        {
                            m_socket.Close();
                            m_socket.Dispose();
                            m_socket = null;
                            OnStateChange?.Invoke(0, "連線中斷");
                        }
                    }
                }
                disposed = true;
            }
        }

        ~ClientEx()
        {
            Dispose(false);
        }
        #endregion
    }
 
}
