using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CoreBC.P2PLib
{
    public class PeerServer
    {
        private TcpListener Listener { get; set; }
        public bool Running { get; set; }
        public int BuffSize { get; set; }
        public string ID { get; set; }
        private MessageHandler MessageHandler;
        public PeerServer(string id, MessageHandler messageHandler, int buffSize)
        {
            ID = id;
            BuffSize = buffSize;
            MessageHandler = messageHandler;
        }
        public void ListenOn(int port)
        {
            if (Running)
            {
                Console.WriteLine("Already connected on.");
                return;
            }

            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();
            Console.WriteLine("Listening on port: " + port);
            MessageParser messageParser = new MessageParser();
            Running = true;
            while (Running)
            {
                try
                {
                    var clientSocket = Listener.AcceptTcpClient();
                    Console.WriteLine(clientSocket.Client.RemoteEndPoint);
                    byte[] bytesFrom = new byte[BuffSize];
                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Read(bytesFrom, 0, BuffSize);
                    messageParser.ConsumeBites(bytesFrom);
                    if (messageParser.EndOfFile)
                    {
                        string clientId = messageParser.SenderId;

                        if (!P2PHelpers.ConnectedClients.ContainsKey(clientId))
                            P2PHelpers.ConnectedClients.Add(clientId, clientSocket);

                        MessageHandler.Handle(messageParser, clientSocket);
                        string prepMsg = P2PHelpers.PrepMessage(ID, MessageHeader.NeedBoostrap);
                        MessageHandler.SendMessageToSocket(prepMsg, clientSocket);
                        Thread messageThread = new Thread(() => handleClient(messageParser, clientSocket));
                        messageThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                    if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
                    {
                        P2PHelpers.ConnectedClients.Remove(messageParser.SenderId);
                        Console.WriteLine(messageParser.SenderId + " disconnected");
                    }
                    else
                    {
                        string er = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine(er);
                    }
                }
            }
        }

        private void handleClient(MessageParser messageParser, TcpClient clientSocket)
        {
            while (true)
            {
                try
                {
                    byte[] bytesFrom = new byte[BuffSize];
                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Read(bytesFrom, 0, BuffSize);
                    messageParser.ConsumeBites(bytesFrom);
                    if (messageParser.EndOfFile)
                        MessageHandler.Handle(messageParser, clientSocket);
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                    if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
                    {
                        P2PHelpers.ConnectedClients.Remove(messageParser.SenderId);
                        Console.WriteLine(messageParser.SenderId + " disconnected");
                    }
                    else
                    {
                        string er = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
                        Console.WriteLine(er);
                    }
                }
            }
        }
    }
}
