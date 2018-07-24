using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Worker
{
    class ClientHandler
    {
        private WebSocket clientSocket;

        private Logger logger;
        public Logger Logger
        {
            get
            {
                if (logger == null)
                    logger = new Logger("client.log", LogLevel.Debug);
                return logger;
            }
        }

        public Packets.WorkerStatus Status = Packets.WorkerStatus.None;

        public const string Platform = "PC C#";

        private JObject config;

        private void DoWork(object inputObj)
        {
            logger.Debug("Started working");
            byte[] input = (byte[])inputObj;
            //CHANGE LATER ------->     \/
            IMainVoid mainVoid = new MainVoid();
            //CHANGE LATER ------->     /\
            clientSocket.Send(new SimplePacket()
            {
                Type = PacketType.WorkOutput,
                Data = mainVoid.DoIt(input, (x) =>
                {
                    clientSocket.Send(new Packets.WorkerInfo()
                    {
                        OkPart = x,
                        Status = Packets.WorkerStatus.Working,
                        System = Platform
                    }.GetPacket());
                })
            }.GetBytes());
            logger.Debug("End working");
        }

        public ClientHandler(JObject config)
        {
            this.config = config;
            SetupSocket();
        }

        private void SetupSocket()
        {
            clientSocket = new WebSocket(config["wsaddr"].Value<string>());
            clientSocket.OnMessage += ClientSocket_OnMessage;
            clientSocket.OnClose += ClientSocket_OnClose;
            clientSocket.OnOpen += ClientSocket_OnOpen;
        }

        private void ClientSocket_OnOpen(object sender, EventArgs e)
        {

        }

        private void ClientSocket_OnClose(object sender, CloseEventArgs e)
        {
            Logger.Log("Socket closed");
            Timer reconnectTimer = new Timer((x) =>
            {
                Logger.Log("Trying to reconnect");
                SetupSocket();
                clientSocket.Connect();
            }, null, 1000, 0);
        }

        private void ClientSocket_OnMessage(object sender, MessageEventArgs e)
        {
            if (!e.IsBinary)
                return;
            SimplePacket packet = StreamUtils.ReadPacket(e.RawData);
            switch (packet.Type)
            {
                case PacketType.WorkInput:
                    if (Status != Packets.WorkerStatus.None)
                    {
                        clientSocket.Close();
                        return;
                    }
                    clientSocket.Send(new Packets.WorkerInfo()
                    {
                        OkPart = 0,
                        Status = Packets.WorkerStatus.Working,
                        System = Platform
                    }.GetPacket());
                    StartProcessing(packet.Data);
                    break;
                case PacketType.WorkerAuthRequest:
                    Packets.WorkerAuthResponse response = new Packets.WorkerAuthResponse();
                    clientSocket.Send(new Packets.WorkerAuthResponse()
                    {  
                        Password = config["password"].Value<string>(),
                        RandomBytes = packet.Data
                    }.GetPacket());

                    break;
            }
            return;
        }

        private Thread workerThread;

        private void StartProcessing(byte[] data)
        {
            workerThread = new Thread(DoWork);
            workerThread.Start();
        }

        public void Start()
        {
            try
            {
                Logger.Log("Starting client handler");
                clientSocket.Connect();
                Logger.Log("Started client handler successfully");
            }
            catch (Exception e)
            {
                Logger.Log($"Error while starting client handler: {e}");
            }
        }

        public void Stop()
        {
            try
            {
                Logger.Log("Stopping client handler");
                clientSocket.Close();
                Logger.Log("Stopping client handler successfully");
            }
            catch (Exception e)
            {
                Logger.Log($"Error while stopping client handler: {e}");
            }
        }
    }
}
