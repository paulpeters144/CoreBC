using CoreBC.BlockModels;
using CoreBC.DataAccess;
using CoreBC.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoreBC.P2PLib
{
    public class PeerClient
    {
        private int BuffSize { get; set; }
        private string ID { get; set; }
        private MessageHandler MessageHandler;
        public PeerClient(string id, MessageHandler messageHandler, int buffSize)
        {
            ID = id;
            BuffSize = buffSize;
            MessageHandler = messageHandler;
        }

        public void Connect(string ip, int port)
        {
            string serverName = $"{ip}:{port}";

            if (!P2PHelpers.ConnectedClients.ContainsKey(serverName))
            {
                var serverConnect = new TcpClient();
                serverConnect.Connect(ip, port);

                string preppedMsg = P2PHelpers.PrepMessage(ID, MessageHeader.NeedBoostrap);
                byte[] outStream = Encoding.ASCII.GetBytes(preppedMsg);
                var serverStream = serverConnect.GetStream();
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();

                Thread messageThread = new Thread(() => listenForMessages(serverConnect));
                messageThread.Start();
                Console.WriteLine("Connected to: " + serverName);
            }
            else
            {
                Console.WriteLine("Already connected to a server");
            }
        }

        private void listenForMessages(TcpClient serverSocket)
        {
            MessageParser message = new MessageParser();
            bool connected = true;
            while (connected)
            {
                try
                {
                    var serverStream = serverSocket.GetStream();
                    byte[] inStream = new byte[BuffSize];
                    serverStream.Read(inStream, 0, BuffSize);
                    message.ConsumeBites(inStream);

                    if (message.EndOfFile)
                    {
                        if (!P2PHelpers.ConnectedClients.ContainsKey(message.SenderId))
                            P2PHelpers.ConnectedClients.Add(message.SenderId, serverSocket);

                        MessageHandler.Handle(message, serverSocket);
                    }
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                    if (error.ToLower().Contains("connection was forcibly closed by the remote host"))
                    {
                        P2PHelpers.ConnectedClients.Remove(message.SenderId);
                        Console.WriteLine(message.SenderId + " disconnected");
                    }
                }
            }
        }
    }
}
