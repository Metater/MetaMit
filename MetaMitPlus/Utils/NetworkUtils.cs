using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MetaMitPlus.Utils
{
    internal class NetworkUtils
    {
        internal static IPEndPoint GetEndPoint(string ipAddress, int port)
        {
            return new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }
        internal static IPEndPoint GetEndPoint(IPAddress ipAddress, int port)
        {
            return new IPEndPoint(ipAddress, port);
        }

        internal static IPEndPoint GetLocalEndPoint(int port, bool ipv4 = true)
        {
            if (ipv4)
                return new IPEndPoint(GetLocalIPv4(), port);
            else
                return new IPEndPoint(GetLocalIPv6(), port);
        }

        internal static IPAddress GetLocalIPv4()
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
        internal static IPAddress GetLocalIPv6()
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
