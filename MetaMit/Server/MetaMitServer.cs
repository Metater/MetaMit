﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace MetaMit.Server
{
    public class MetaMitServer
    {
        public Base.MetaMitServerBase server;
        public Dictionary<Guid, Base.ClientConnection> Clients { get; private set; } = new Dictionary<Guid, Base.ClientConnection>();
        public List<Base.ClientConnection> ConnectingClients { get; private set; } = new List<Base.ClientConnection>();



        public MetaMitServer(int port, int backlog)
        {
            server = new Base.MetaMitServerBase(port, backlog);
            Subscribe();
        }
        #region PublicMethods
        public void Start()
        {
            server.StartListening();
        }
        public void Stop()
        {
            KickAll("Server shutdown");
            server.StopListening();
        }


        public void KickClient(Guid guid, string message)
        {
            Base.ClientConnection connection = Clients[guid];
            server.SendStringEOT(connection, message);
            server.DisconnectClient(connection);
            Clients.Remove(guid);
        }
        public void KickAll(string message)
        {
            foreach (Guid client in Clients.Keys)
                KickClient(client, message);
        }
        public void BroadcastString(string message)
        {
            foreach (Base.ClientConnection connection in Clients.Values)
                server.SendString(connection, message);
        }
        public void BroadcastStringBut(string message, Guid guid)
        {
            foreach (Base.ClientConnection connection in Clients.Values)
                if (connection.guid != guid)
                    server.SendString(connection, message);
        }
        #endregion PublicMethods



        #region Events
        private void Subscribe()
        {
            server.OnServerStartEvent += Server_OnServerStartEvent;
            server.OnServerStopEvent += Server_OnServerStopEvent;

            server.OnConnectionPendingEvent += Server_OnConnectionPendingEvent;
            server.OnConnectionAcceptedEvent += Server_OnConnectionAcceptedEvent;
            server.OnConnectionEndedEvent += Server_OnConnectionEndedEvent;
            server.OnConnectionLostEvent += Server_OnConnectionLostEvent;

            server.OnDataReceivedEvent += Server_OnDataReceivedEvent;
        }



        private void Server_OnServerStartEvent()
        {
            Console.WriteLine("Server started!");
        }
        private void Server_OnServerStopEvent()
        {
            Console.WriteLine("Server stopped!");
        }



        private void Server_OnConnectionPendingEvent(object sender, Base.ServerBaseEventArgs.ConnectionPending e)
        {
            Console.WriteLine("Connection pending...");
            Guid guid = Guid.NewGuid();
            Base.ClientConnection connection = new Base.ClientConnection(guid, e.socket);
            server.AcceptClient(connection, Clients.Count);
        }
        private void Server_OnConnectionAcceptedEvent(object sender, Base.ServerBaseEventArgs.ConnectionAccepted e)
        {
            lock (Clients)
            {
                Clients.Add(e.connection.guid, e.connection);
            }
            Console.WriteLine("Connection accepted, guid: " + e.connection.guid);
        }
        private void Server_OnConnectionEndedEvent(object sender, Base.ServerBaseEventArgs.ConnectionEnded e)
        {
            Console.WriteLine("Connection ended, guid: " + e.client);
        }
        private void Server_OnConnectionLostEvent(object sender, Base.ServerBaseEventArgs.ConnectionLost e)
        {
            Console.WriteLine("Connection lost, guid: " + e.client);
        }



        private void Server_OnDataReceivedEvent(object sender, Base.ServerBaseEventArgs.DataReceived e)
        {
            Console.WriteLine("Data received, data: " + e.data);
        }
        #endregion Events



        #region Notes
        /*
        




        You have to ensure things went through, like disconnecting someone with a message, make sure message went through first.
        Also shutting down the server make sure all clients go the message




        May want to remove the listner progress, and make everything events

        Move max client handler away from the base server

        For disconneting:
          Client requests to server to be disconnected for graceful disconnect
 
        Add a Polling listener, checks for loss of connection
        */
        #endregion Notes
    }
}
