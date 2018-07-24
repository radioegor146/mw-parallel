using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using SuperWebSocket;
using SuperWebSocket.SubProtocol;
using Nito.Collections;

namespace Master
{
    class MasterHandler
    {
        private WebSocketServer mainServer;
        private WebSocketServer webSocketServer;

        private ConcurrentDictionary<string, MultiServerClient> clients = new ConcurrentDictionary<string, MultiServerClient>();

        private Deque<Task> taskQueue = new Deque<Task>();

        private ConcurrentDictionary<string, Task> currentTasks = new ConcurrentDictionary<string, Task>();

        private bool working = true;

        private JObject config;

        private Logger logger;
        public Logger Logger
        {
            get
            {
                if (logger == null)
                    logger = new Logger("master.log", LogLevel.Debug);
                return logger;
            }
        }

        public MasterHandler(JObject config)
        {
            Logger.Log("App started");
            this.config = config;
            webSocketServer = new WebSocketServer();
            webSocketServer.Setup(config["wsport"].Value<int>());
            webSocketServer.NewSessionConnected += WebSocketServer_NewSessionConnected;
            webSocketServer.NewMessageReceived += WebSocketServer_NewMessageReceived;
            mainServer = new WebSocketServer();
            mainServer.Setup(config["mainport"].Value<int>());
            mainServer.NewSessionConnected += MainServer_NewSessionConnected;
            mainServer.SessionClosed += MainServer_SessionClosed;
            mainServer.NewDataReceived += MainServer_NewDataReceived;
        }

        private void WebSocketServer_NewMessageReceived(WebSocketSession session, string value)
        {
            SendTask(new byte[0]);
        }

        private void WebSocketServer_NewSessionConnected(WebSocketSession session)
        {
            lock (taskQueue)
            {
                session.Send(new WebSocketPackets.GeneralInfoPacket()
                {
                    TasksCount = taskQueue.Count,
                    WorkerCount = clients.Count
                }.SerializeJson());
                session.Send(new WebSocketPackets.StartWorkerPacket()
                {
                    Workers = clients.Values.Select(x => new WebSocketPackets.WorkerInfo()
                    {
                        WorkerProgress = x.LastProgress,
                        WorkerId = x.GetId(),
                        WorkerStatus = x.Status,
                        WorkerSystem = x.System
                    }).ToArray()
                }.SerializeJson());
            }
        }

        private void MainServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            clients[session.SessionID].OnMessage(value);
        }

        private void MainServer_NewSessionConnected(WebSocketSession session)
        {
            var client = new MultiServerClient(this, new ClientInfo()
            {
                Id = session.SessionID,
                Config = config
            }, session);
            while (!clients.TryAdd(session.SessionID, client)) { Thread.Sleep(1); }
            SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
            {
                EventType = WebSocketPackets.EventType.WorkerConnected,
                WorkerId = session.SessionID,
            });
            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
            {
                TasksCount = taskQueue.Count,
                WorkerCount = clients.Count
            });
            Packets.WorkerAuthRequest request = new Packets.WorkerAuthRequest();
            client.RandomBytes = request.RandomBytes;
            client.SendPacket(request.GetPacket());
            logger.Debug($"Client {session.SessionID} connected");
        }

        private void MainServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            RemoveClient(session.SessionID);
        }

        private Thread mainSelectorThread;

        public void Start()
        { 
            try
            {
                Logger.Log("Starting master handler");
                mainServer.Start();
                webSocketServer.Start();
                mainSelectorThread = new Thread(mainSelectorThreadVoid);
                mainSelectorThread.Start();
                Logger.Log("Started master handler successfully");
            }
            catch (Exception e)
            {
                Logger.Log($"Error while starting master handler: {e}");
            }
        }

        public void Stop()
        {
            try
            {
                Logger.Log("Stopping master handler");
                working = false;
                mainServer.Stop();
                webSocketServer.Stop();
                Logger.Log("Stopped master handler successfully");
            }
            catch (Exception e)
            {
                Logger.Log($"Error while stopping master handler: {e}");
            }
        }

        int taskId = 0;

        public int SendTask(byte[] data)
        {
            lock (taskQueue)
            {
                taskId++;
                taskQueue.AddToBack(new Task()
                {
                    Id = taskId,
                    InputData = data
                });
                SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                {
                    TasksCount = taskQueue.Count,
                    WorkerCount = clients.Count
                });
                return taskId;
            }
        }

        private void mainSelectorThreadVoid()
        {
            while (working)
            {
                Thread.Sleep(2);
                lock (taskQueue)
                {
                    if (taskQueue.Count > 0)
                    {
                        string workerId = null;
                        foreach (var worker in clients)
                        {
                            if (worker.Value.Status == ClientWorkerStatus.None)
                            {
                                workerId = worker.Key;
                                break;
                            }
                        }
                        if (workerId == null)
                            continue;
                        while (!currentTasks.TryAdd(workerId, taskQueue.RemoveFromFront())) { Thread.Sleep(1); }
                        SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                        {
                            EventType = WebSocketPackets.EventType.GeneralInfo,
                            TasksCount = taskQueue.Count,
                            WorkerCount = clients.Count
                        });
                        if (!clients[workerId].StartWork(currentTasks[workerId].InputData, (x) =>
                        {
                            Task t; 
                            while (!currentTasks.TryRemove(workerId, out t)) { Thread.Sleep(1); }
                            clients[workerId].Status = ClientWorkerStatus.None;
                        }, (x) =>
                        {
                            taskQueue.AddToFront(currentTasks[workerId]);
                            Task t;
                            while (!currentTasks.TryRemove(workerId, out t)) { Thread.Sleep(1); }
                            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                            {
                                TasksCount = taskQueue.Count,
                                WorkerCount = clients.Count
                            });
                        }, (x) =>
                        {
                            SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
                            {
                                EventType = WebSocketPackets.EventType.WorkerStatusChange,
                                WorkerId = workerId,
                                WorkerProgress = x,
                                WorkerStatus = clients[workerId].Status,
                                WorkerSystem = clients[workerId].System
                            });
                        }))
                        {
                            taskQueue.AddToFront(currentTasks[workerId]);
                            Task t;
                            while (!currentTasks.TryRemove(workerId, out t)) { Thread.Sleep(1); }
                            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                            {
                                TasksCount = taskQueue.Count,
                                WorkerCount = clients.Count
                            });
                        }
                    }
                }
            }
        }

        public void RemoveClient(string clientId)
        {
            if (!clients.ContainsKey(clientId))
                return;
            SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
            {
                EventType = WebSocketPackets.EventType.WorkerDisconnected,
                WorkerId = clientId
            });
            MultiServerClient tc;
            while (!clients.TryRemove(clientId, out tc)) { Thread.Sleep(1); }
            if (currentTasks.ContainsKey(clientId))
            {
                lock (taskQueue)
                    taskQueue.AddToFront(currentTasks[clientId]);
                Task t;
                while (!currentTasks.TryRemove(clientId, out t)) { Thread.Sleep(1); }
            }
            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
            {
                TasksCount = taskQueue.Count,
                WorkerCount = clients.Count
            });
            Logger.Debug($"Client {clientId} removed successfully");
        }

        public void SendToWebSocket(IWebSocketPacket packet)
        {
            lock (webSocketServer)
                foreach (var session in webSocketServer.GetSessions(x => true))
                    session.Send(packet.SerializeJson());
        }
    }
}
