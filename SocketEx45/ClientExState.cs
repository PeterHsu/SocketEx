using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketEx
{
    public enum ClientExState
    {
        None, // 未連線
        Connecting, // 連線中
        Connected, // 已連線
        Disconnected // 已斷線
    }
}
