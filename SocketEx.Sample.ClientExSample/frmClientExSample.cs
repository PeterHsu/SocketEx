using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketEx.Sample.ClientExSample
{
    public partial class frmClientExSample : Form
    {
        private Big5Client m_big5Client = null;
        public frmClientExSample()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (m_big5Client != null)
            {
                lstMsg.Items.Add("連線已存在");
                return;
            }
            DnsEndPoint endPoint = new DnsEndPoint(txtIP.Text, int.Parse(txtPort.Text));
            m_big5Client = new Big5Client(endPoint);
            m_big5Client.OnStateChange += M_rayinAPI_OnStateChange;
            m_big5Client.OnReceive += M_rayinAPI_OnReceiveMsg;
            m_big5Client.OnSend += M_rayinAPI_OnSendMsg;
            m_big5Client.Connect();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            m_big5Client?.Dispose();
            m_big5Client = null;
        }


        private void M_rayinAPI_OnStateChange(int code, string msg)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"{code}:{msg}");
            }));
        }

        private void M_rayinAPI_OnSendMsg(string msg)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"送出電文:{msg}");
            }));
        }

        private void M_rayinAPI_OnReceiveMsg(string msg)
        {
            Invoke(new Action(() =>
            {
                lstMsg.Items.Add($"收到電文:{msg}");
            }));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (m_big5Client?.State == ClientExState.Connected)
            {
                m_big5Client.Send(txtMsg.Text);
            }
            else
            {
                lstMsg.Items.Add("尚未連線, 無法傳送");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lstMsg.Items.Clear();
        }
    }
}
