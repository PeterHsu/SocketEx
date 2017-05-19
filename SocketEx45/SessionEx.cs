using System;
using System.Net.Sockets;
using System.Text;

namespace SocketEx
{
    public abstract class SessionEx<TSessionEx> : IDisposable where TSessionEx : SessionEx<TSessionEx>
    {
        private object lockRef = new object();
        public String SessionGUID { get; set; }
        public String SessionEncoding { get; set; }
        public Socket m_socket;
        public ServerEx<TSessionEx> Server;

        internal void Init(ServerEx<TSessionEx> _Server, String _strGUID, Socket _Socket)
        {
            SessionEncoding = "Big5";
            Server = _Server;
            SessionGUID = _strGUID;
            m_socket = _Socket;

            SocketAsyncEventArgs ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.Completed += OnReceiveCompleted;
            ReceiveEventArgs.SetBuffer(new Byte[Int16.MaxValue], 0, Int16.MaxValue);
            Action(SessionAction.Receive, ReceiveEventArgs);
        }

        private void Action(SessionAction action, SocketAsyncEventArgs args)
        {
            lock (lockRef)
            {
                switch (action)
                {
                    case SessionAction.Receive:
                        if (!m_socket.ReceiveAsync(args))
                        {
                            ProcessReceive(args);
                        }
                        break;
                    case SessionAction.Send:
                        if (!m_socket.SendAsync(args))
                        {
                            ProcessSend(args);
                        }
                        break;
                }
            }
        }

        #region 收資料相關
        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, data.Length);
                Array.Clear(e.Buffer, 0, e.Buffer.Length);
                HandleReceive(data);//處理電文方式
                Action(SessionAction.Receive, e);
            }
            else
            {
                SessionStop();
            }
        }

        public abstract void HandleReceive(byte[] data);
        #endregion

        #region 送資料相關
        public void SendMessage(string strMsg)
        {
            byte[] data = Encoding.GetEncoding(SessionEncoding).GetBytes(strMsg);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.UserToken = strMsg;
            args.Completed += OnSendCompleted;
            Action(SessionAction.Send, args);
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e) 
        {
            this.ProcessSend(e);
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            e.Completed -= new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
            if (e.SocketError == SocketError.Success)
            {
                Server.RaiseOnSendMsg(e.UserToken.ToString(), SessionGUID);
            }
            else 
            {
                SessionStop();
            }
            e.Dispose();
        }
        #endregion

        public void SessionStop() 
        {
            Server.CloseSessionSocket(SessionGUID);
            Dispose();
        }

        #region Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    lock (m_socket)
                    {
                        if (m_socket != null)
                        {
                            m_socket.Close();
                            m_socket.Dispose();
                            m_socket = null;
                        }
                    }
                }
                disposed = true;
            }
        }

        ~SessionEx() 
        {
            Dispose(false);
        }
        #endregion
    }

    enum SessionAction
    {
        Send, // 傳送資料
        Receive // 接收資料
    }
}
