using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketEx
{
    public class Big5Client : ClientEx, IClientEx<string>
    {
        public event Action<string> OnSend;
        public event Action<string> OnReceive;

        public Big5Client(DnsEndPoint endPoint) : base(endPoint) { }
        public void Send(string msg)
        {
            byte[] data = Encoding.GetEncoding("Big5").GetBytes(msg);
            base.Send(data);
        }
        protected override void OnSendBase(byte[] msg)
        {
            string data = Encoding.GetEncoding("Big5").GetString(msg);
            OnSend?.Invoke(data);
        }
        protected override void OnReceiveBase(byte[] msg)
        {
            string data = Encoding.GetEncoding("big5").GetString(msg);
            OnReceive?.Invoke(data);
        }
    }
}
