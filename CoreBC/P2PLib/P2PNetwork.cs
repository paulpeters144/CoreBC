using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CoreBC.P2PLib
{
    public class P2PNetwork
    {
        public string ID { get; set; }
        public PeerClient Client { get; set; }
        public PeerServer Server { get; set; }
        public MessageHandler MessageHandler;

        public int BuffSize { get; set; }
        public P2PNetwork(int buffSize)
        {
            ID = Guid.NewGuid().ToString("N").ToUpper();
            BuffSize = buffSize;
            MessageHandler = new MessageHandler(ID);
            Client = new PeerClient(ID, MessageHandler, BuffSize);
            Server = new PeerServer(ID, MessageHandler, BuffSize);
            P2PHelpers.ConnectedClients = new Dictionary<string, TcpClient>();
        }

        public void ListenOn(int port)
        {
            Server.ListenOn(port);
        }

        public void ConnectTo(string ip, int port)
        {
            Client.Connect(ip, port);
        }

        public void BroadCast(string preppedMessage)
        {
            new MessageHandler(ID).Broadcast(preppedMessage);
        }
    }
}
