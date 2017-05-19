using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketEx.Sample.ServerExSample
{
    public partial class frmServerExSample : Form
    {
        private ServerEx<Big5Session> m_server = null;
        public frmServerExSample()
        {
            InitializeComponent();
        }

        private void frmServerExSample_Load(object sender, EventArgs e)
        {
            m_server = new ServerEx<Big5Session>();
            m_server.OnReceiveMsg += M_server_OnReceiveMsg;
            m_server.OnSendMsg += M_server_OnSendMsg;
            m_server.OnSessionConnect += M_server_OnSessionConnect;
            m_server.OnSessionDisconnect += M_server_OnSessionDisconnect;
            m_server.OnServerStart += M_server_OnServerStart;
            m_server.OnServerFail += M_server_OnServerFail;
        }
        private void M_server_OnServerFail(Exception exp)
        {
            Invoke(new Action(() => {
                lstMsg.Items.Add($"服務啟動失敗-{exp.ToString()}");
            }));
        }

        private void M_server_OnServerStart()
        {
            Invoke(new Action(() => {
                lstMsg.Items.Add("服務已啟動");
            }));
        }

        private void M_server_OnSessionDisconnect(string sessionID)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"Session：{sessionID} 斷線");
                chklstSession.Items.Remove(sessionID);
            }));
        }

        private void M_server_OnSessionConnect(string sessionID, Socket socket)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"Session：{sessionID} IP/Port：{socket.RemoteEndPoint}連線");
                chklstSession.Items.Add(sessionID);
            }));
        }

        private void M_server_OnSendMsg(string msg, string sessionID)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"Session：{sessionID} 送出電文：{msg}");
            }));
        }

        private void M_server_OnReceiveMsg(string msg, string sessionID)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"Session：{sessionID} 收到電文：{msg}");
            }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(txtIP.Text), int.Parse(txtPort.Text));
            m_server.Start(endPoint);
        }
        
        private void btnStop_Click(object sender, EventArgs e)
        {
            m_server.Stop();
        }

        private void btnBroadcast_Click(object sender, EventArgs e)
        {
            m_server.Broadcast(txtMsg.Text);
        }

        private void btnSendMessageBySessionID_Click(object sender, EventArgs e)
        {
            string[] sessionIDs = chklstSession.CheckedItems.OfType<string>().ToArray();
            m_server.SendMessageBySessions(txtMsg.Text, sessionIDs);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lstMsg.Items.Clear();
        }
    }
}
