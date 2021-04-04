﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitStandard.Utils
{
    public class ConsoleUtils
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
