using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MetaMit.Generic
{
    public class ConsoleListener
    {
        #region Fields
        public event EventHandler<ConsoleMessageSentEventArgs> OnConsoleMessageSent;
        public class ConsoleMessageSentEventArgs : EventArgs
        {
            public string message;
        }
        private Thread listeningThread;
        private CancellationTokenSource consoleListenerCts = new CancellationTokenSource();
        #endregion Fields



        public ConsoleListener()
        {

        }



        public void Start()
        {
            // Find a way to remove Task.Run, and cancel more cleanly
            Task.Run(() =>
            {
                listeningThread = new Thread(new ThreadStart(() =>
                {
                    while (!consoleListenerCts.Token.IsCancellationRequested)
                    {
                        string message = Console.ReadLine();
                        OnConsoleMessageSent?.Invoke(this, new ConsoleMessageSentEventArgs
                        {
                            message = message
                        });
                    }
                }));
            }, consoleListenerCts.Token);
        }
        public void Stop()
        {
            consoleListenerCts.Cancel();
        }
    }
}
