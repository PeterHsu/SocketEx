using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SocketEx
{
    public class ServerEx<TSessionEx> : IDisposable where TSessionEx : SessionEx<TSessionEx>
    {
        private Socket m_socket;
        public ConcurrentDictionary<String, TSessionEx> SessionCollection = new ConcurrentDictionary<String, TSessionEx>();
        public string[] SessionIgnoreIP = { };
        public event Action<String, String> OnReceiveMsg = null;
        public event Action<String, String> OnSendMsg = null;
        public event Action OnServerStart = null;
        public event Action<Exception> OnServerFail = null;
        public event Action<String, Socket> OnSessionConnect = null;
        public event Action<String> OnSessionDisconnect = null;
        public void Start(IPEndPoint endPoint)
        {
            if (m_socket != null)
            {
                this.OnServerFail(new Exception("服務已存在"));
                return;
            }
            try
            {
                m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_socket.Bind(endPoint);
                m_socket.Listen(4096);
                StartAccept(null);
                OnServerStart?.Invoke();
            }
            catch (Exception ex) 
            {
                OnServerFail?.Invoke(ex);
            }
        }

        public void Stop()
        {
            m_socket?.Close();
            m_socket?.Dispose();
            m_socket = null;
            foreach (var session in SessionCollection)
            {
                session.Value.SessionStop();
            }
        }

        public void Broadcast(string msg)
        {
            foreach(var session in SessionCollection)
            {
                session.Value.SendMessage(msg);
            }
        }
        public void SendMessageBySessions(string msg, string[] sessionIDs)
        {
            foreach(var sessionID in sessionIDs)
            {
                SessionCollection[sessionID].SendMessage(msg);
            }
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += OnAcceptCompleted;
            }
            else
            {
                //socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            if (!m_socket.AcceptAsync(acceptEventArg))
            {
                ProcessAccept(acceptEventArg);
            }
        }
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                //建立新的AppSession處理每個Socket Client
                Socket socket = e.AcceptSocket;

                if (socket.Connected)
                {
                    if (SessionIgnoreIP.Length > 0 && SessionIgnoreIP.Contains(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()))
                    {
                        socket.Close();
                        //StartAccept(e);
                    }
                    else
                    {
                        String strGUID = Guid.NewGuid().ToString().ToUpper();

                        if (OnSessionConnect != null) { OnSessionConnect(strGUID, socket); }

                        TSessionEx session = (TSessionEx)Activator.CreateInstance(typeof(TSessionEx));
                        session.Init(this, strGUID, socket);
                        SessionCollection.GetOrAdd(strGUID, session);
                    }
                    StartAccept(e);//通知繼續接收Socke連線
                }
            }
        }

        internal void CloseSessionSocket(string strGUID)
        {
            TSessionEx session;
            SessionCollection.TryRemove(strGUID, out session);
            if(session!=null) OnSessionDisconnect?.Invoke(strGUID);
        }
        internal void RaiseOnSendMsg(string msg,string sessionID)
        {
            OnSendMsg?.Invoke(msg, sessionID);
        }
        public void RaiseOnReceiveMsg(string msg, string sessionID)
        {
            OnReceiveMsg?.Invoke(msg, sessionID);
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
                    m_socket.Close();
                }
                disposed = true;
            }
        }

        ~ServerEx()
        {
            Dispose(false);
        }
        #endregion
    }
}
