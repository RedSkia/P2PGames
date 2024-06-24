using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public abstract class BaseServer : BaseConnection
    {
        public event EventHandler<EventArgs> OnServerConnected = delegate { };
        public virtual void Start() => this.OnServerConnected?.Invoke(this, EventArgs.Empty);
    }
}
