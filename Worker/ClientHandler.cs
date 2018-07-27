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
using System.Diagnostics;

namespace Worker
{
    class ClientHandler
    {
        private WebSocket clientSocket;
        private Timer keepAliveTimer;
        private Stopwatch keepAliveSw;

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
            IMainVoid mainVoid;
            try
            {
                Logger.Log($"Loading class {config["mainvoid"].Value<string>()}");
                object controllerInstance = Activator.CreateInstance(Type.GetType(config["mainvoid"].Value<string>()));
                mainVoid = (IMainVoid)controllerInstance;
                Logger.Log($"Class {config["mainvoid"].Value<string>()} loaded successfully");
            }
            catch (Exception e)
            {
                Logger.Log($"Can't load class {config["mainvoid"].Value<string>()}; Exception: {e}; Loading default class");
                mainVoid = new MainVoid();
            }
            mainVoid.Setup(config);
            //CHANGE LATER ------->     /\
            try
            {
                clientSocket.Send(new SimplePacket()
                {
                    Type = PacketType.WorkOutput,
                    Data = mainVoid.DoIt(input, (x) =>
                    {
                        try
                        {
                            if (x < 0 || x > 1)
                                return;
                            clientSocket.Send(new Packets.WorkerInfo()
                            {
                                OkPart = x,
                                Status = Packets.WorkerStatus.Working,
                                System = Platform
                            }.GetPacket());
                        }
                        catch
                        {

                        }
                    })
                }.GetBytes());
            }
            catch (Exception e)
            {
                clientSocket.Send(new SimplePacket()
                {
                    Type = PacketType.WorkOutput,
                    Data = Encoding.UTF8.GetBytes($"Exception: {e}")
                }.GetBytes());
            }
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
            keepAliveSw = new Stopwatch();
            keepAliveTimer = new Timer(x =>
            {
                if (!clientSocket.IsAlive)
                    return;
                if (!keepAliveSw.IsRunning)
                    keepAliveSw.Start();
                if (keepAliveSw.ElapsedMilliseconds > 2000)
                {
                    clientSocket.Close();
                    Logger.Log("Disconnected because of timeout");
                    keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    return;
                }
                clientSocket.Send(new SimplePacket()
                {
                    Data = new byte[0],
                    Type = PacketType.Nop
                }.GetBytes());
            }, null, 0, 1000);
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
                    clientSocket.Send(new Packets.WorkerInfo()
                    {
                        OkPart = 0,
                        Status = Packets.WorkerStatus.None,
                        System = Platform
                    }.GetPacket());
                    break;
                case PacketType.Nop:
                    keepAliveSw.Reset();
                    break;
            }
            return;
        }

        private Thread workerThread;

        private void StartProcessing(byte[] data)
        {
            workerThread = new Thread(DoWork);
            workerThread.Start(data);
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
