using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace MetaMit.Utils
{
    public class ConsoleUtils
    {
        public class ConsoleListener
        {
            #region Fields
            public event EventHandler<ConsoleMessageEventArgs> OnConsoleMessage;
            public class ConsoleMessageEventArgs : EventArgs
            {
                public string message;
            }
            public Task consoleListenerTask;
            private Thread listeningThread;
            private CancellationTokenSource consoleListenerCts = new CancellationTokenSource();
            #endregion Fields



            public ConsoleListener()
            {

            }



            public void Start()
            {
                // Find a way to remove Task.Run, and cancel more cleanly
                consoleListenerTask = Task.Run(() =>
                {
                    listeningThread = new Thread(new ThreadStart(() =>
                    {
                        while (!consoleListenerCts.Token.IsCancellationRequested)
                        {
                            string message = Console.ReadLine();
                            OnConsoleMessage?.Invoke(this, new ConsoleMessageEventArgs
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



        public static class ConsoleQuestions
        {
            public static IPAddress AskIP()
            {
                IPAddress ip = IPAddress.None;

                string server = "";
                bool ipValid = false;
                while (!ipValid)
                {
                    Console.Write("Enter server ip: ");
                    server = Console.ReadLine();
                    if (IPAddress.TryParse(server, out ip))
                        ipValid = true;
                }
                return ip;
            }
            public static ushort AskPort()
            {
                ushort port = 0;
                string portStr = "";
                bool portValid = false;
                while (!portValid)
                {
                    Console.Write("Enter server port: ");
                    portStr = Console.ReadLine();
                    if (ushort.TryParse(portStr, out port))
                        portValid = true;
                }
                return port;
            }
            public static string AskUsername()
            {
                string username = "";
                bool nameValid = false;
                while (!nameValid)
                {
                    Console.Write("Enter your username: ");
                    username = Console.ReadLine();
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrWhiteSpace(username))
                        nameValid = true;
                }
                return username;
            }
        }
    }
}
