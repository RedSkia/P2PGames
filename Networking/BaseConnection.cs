using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public abstract class BaseConnection
    {
        public event EventHandler<EventArgs> ConnectionOpen;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<EventArgs> ConnectionSucessful;
        public event EventHandler<EventArgs> ConnectionFailed;
        public void OnConnectionOpen()
        {

        }
    }
}
