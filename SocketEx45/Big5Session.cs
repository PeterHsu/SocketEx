using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketEx
{
    public class Big5Session : SessionEx<Big5Session>
    {
        public override void HandleReceive(byte[] data)
        {
            string msg = Encoding.GetEncoding("Big5").GetString(data);
            Server.RaiseOnReceiveMsg(msg, SessionGUID);
        }
    }
}
