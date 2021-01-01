using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace MetaMit.Generic
{
    public static class NetUtils
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
