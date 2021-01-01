using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MetaMit.Server
{
    public static class Generic
    {
        public static IPAddress GetIP()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            return ipAddress;
        }
        public static IPEndPoint GetEndPoint(IPAddress ipAddress, int port)
        {
            return new IPEndPoint(ipAddress, port);
        }
    }
}
