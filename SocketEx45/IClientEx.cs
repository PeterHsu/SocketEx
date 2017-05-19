using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketEx
{
    public interface IClientEx<T>
    {
        ClientExState State { get; set; }

        void Connect();
        void Send(T msg);
        
        event Action<int, string> OnStateChange;
        event Action<T> OnSend;
        event Action<T> OnReceive;
    }
}
