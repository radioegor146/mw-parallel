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
            Stopwatch sw = new Stopwatch();
            sw.Start();
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
                byte[] dataBytes = new SimplePacket()
                {
                    Type = PacketType.WorkOutput,
                    Data = mainVoid.DoIt(input, (x) =>
                    {
                        try
                        {
                            if (sw.ElapsedMilliseconds < 750)
                                return;
                            sw.Restart();
                            if (x < 0 || x > 1)
                                return;
                            if (!clientSocket.IsAlive)
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
                }.GetBytes();
                Status = Packets.WorkerStatus.None;
                if (clientSocket.IsAlive)
                    clientSocket.Send(dataBytes);
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException)
                {
                    Logger.Log($"Thread aborted: {e.Message}");
                }
                else
                {
                    try
                    {
                        if (clientSocket.IsAlive)
                            clientSocket.Send(new SimplePacket()
                            {
                                Type = PacketType.WorkOutput,
                                Data = Encoding.UTF8.GetBytes($"Exception: {e}")
                            }.GetBytes());
                    }
                    catch
                    {
                        Logger.Log($"Nice error: {e}");
                    }
                }
            }
            Status = Packets.WorkerStatus.None;
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
            clientSocket.WaitTime = TimeSpan.FromSeconds(5);
            clientSocket.OnMessage += ClientSocket_OnMessage;
            clientSocket.OnClose += ClientSocket_OnClose;
            clientSocket.OnOpen += ClientSocket_OnOpen;
            clientSocket.OnError += ClientSocket_OnError;
        }

        private bool previousState = false;

        private void ClientSocket_OnOpen(object sender, EventArgs e)
        {
            Logger.Log("Connected");
            keepAliveSw = new Stopwatch();
            keepAliveTimer = new Timer(x =>
            {
                bool nowState = clientSocket.IsAlive;
                if (!nowState && previousState)
                {
                    Logger.Log("Disconnected because of timeout");
                    keepAliveSw.Stop();
                    clientSocket.Close();
                    keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                if (nowState)
                {
                    lock (keepAliveSw)
                    {
                        if (!keepAliveSw.IsRunning)
                            keepAliveSw.Start();
                        if (keepAliveSw.ElapsedMilliseconds > 5000)
                        {
                            Logger.Log("Disconnected because of timeout");
                            keepAliveSw.Stop();
                            clientSocket.Close();
                            keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            return;
                        }
                    }
                    if (clientSocket.IsAlive)
                        clientSocket.Send(new SimplePacket()
                        {
                            Data = new byte[0],
                            Type = PacketType.Nop
                        }.GetBytes());
                }
                previousState = nowState;
            }, null, 500, 500);
        }

        private void ClientSocket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            
        }

        private void ClientSocket_OnClose(object sender, CloseEventArgs e)
        {
            Logger.Log("Socket closed");
            try
            {
                Logger.Log("Aborting main thread");
                workerThread.Abort();
                while (!(workerThread.ThreadState == System.Threading.ThreadState.Stopped || workerThread.ThreadState == System.Threading.ThreadState.Unstarted || workerThread.ThreadState == System.Threading.ThreadState.Aborted) ) { Thread.Sleep(1); }
            }
            catch
            {
                Logger.Log("Thread is not working");
            }
            Logger.Log("Thread stopped");
            previousState = false;
            Status = Packets.WorkerStatus.None;
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
            lock (keepAliveSw)
                if (keepAliveSw.IsRunning)
                    keepAliveSw.Restart();
            switch (packet.Type)
            {
                case PacketType.WorkInput:
                    if (Status != Packets.WorkerStatus.None)
                    {
                        Logger.Log("Bad state");
                        clientSocket.Close();
                        return;
                    }
                    clientSocket.Send(new Packets.WorkerInfo()
                    {
                        OkPart = 0,
                        Status = Packets.WorkerStatus.Working,
                        System = Platform
                    }.GetPacket());
                    Status = Packets.WorkerStatus.Working;
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
                        Status = Status,
                        System = Platform
                    }.GetPacket());
                    break;
                case PacketType.Signal:
                    Packets.Signal signal = new Packets.Signal(packet);
                    switch (signal.Type)
                    {
                        case Packets.SignalEnum.Abort:
                            workerThread.Abort();
                            while (!(workerThread.ThreadState == System.Threading.ThreadState.Stopped || workerThread.ThreadState == System.Threading.ThreadState.Unstarted || workerThread.ThreadState == System.Threading.ThreadState.Aborted)) { Thread.Sleep(1); }
                            Logger.Log("Aborted");
                            clientSocket.Send(new Packets.Signal()
                            {
                                Data = new byte[0],
                                Type = Packets.SignalEnum.Abort
                            }.GetPacket().GetBytes());
                            Status = Packets.WorkerStatus.None;
                            break;
                    }
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
