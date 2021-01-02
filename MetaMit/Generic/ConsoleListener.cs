using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMit.Generic
{
    public class ConsoleListener
    {
        public event EventHandler<ConsoleCancelEventArgs> OnConsoleMessageSent;
        public class ConsoleMessageSentEventArgs : EventArgs
        {

        }
    }
}
