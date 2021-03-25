using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MetaMitStandard.Utils
{
    public static class NetworkUtils
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
        public static IPEndPoint GetLocalEndPoint(int port, bool ipv4)
        {
            if (ipv4)
                return new IPEndPoint(GetLocalIPv4(), port);
            else
                return new IPEndPoint(GetLocalIPv6(), port);
        }
        public static IPAddress GetLocalIPv4()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        public static bool TryGetIP(AddressFamily addressFamily, out IPAddress ipAddress)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == addressFamily)
                {
                    ipAddress = ip;
                    return true;
                }
            }
            ipAddress = default(IPAddress);
            return false;
        }
        public static IPAddress GetLocalIPv6()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv6 address in the system!");
        }
    }
}
