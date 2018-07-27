using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Collections.Concurrent;
using SuperWebSocket;
using SuperWebSocket.Config;
using System.Security.Cryptography;

namespace Master
{
    class MasterHandler
    {
        private WebSocketServer mainServer;
        private WebSocketServer webSocketServer;

        private ConcurrentDictionary<string, MultiServerClient> workers = new ConcurrentDictionary<string, MultiServerClient>();

        private ConcurrentDictionary<string, PermissionLevel> clients = new ConcurrentDictionary<string, PermissionLevel>();

        private IDataController dataController;

        private bool working = true;

        private JObject config;

        private Logger logger;

        private RSA rsa;
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
            Logger.Log("Generating RSA keypair");
            rsa = RSA.Create();
            Logger.Log("Generated RSA keypair");
            this.config = config;
            try
            {
                Logger.Log($"Loading class {config["datacontroller"].Value<string>()}");
                object controllerInstance = Activator.CreateInstance(Type.GetType(config["datacontroller"].Value<string>()), config["controllercfg"]);
                dataController = (IDataController)controllerInstance;
                Logger.Log($"Class {config["datacontroller"].Value<string>()} loaded successfully");
            }
            catch (Exception e)
            {
                Logger.Log($"Can't load class {config["datacontroller"].Value<string>()}; Exception: {e}; Loading default class");
                dataController = new DataControllers.SimpleDataController(config);
            }
            webSocketServer = new WebSocketServer();
            webSocketServer.Setup(config["wsport"].Value<int>());
            webSocketServer.NewSessionConnected += WebSocketServer_NewSessionConnected;
            webSocketServer.SessionClosed += WebSocketServer_SessionClosed;
            webSocketServer.NewMessageReceived += WebSocketServer_NewMessageReceived;
            mainServer = new WebSocketServer();
            mainServer.Setup(config["mainport"].Value<int>());
            mainServer.NewSessionConnected += MainServer_NewSessionConnected;
            mainServer.SessionClosed += MainServer_SessionClosed;
            mainServer.NewDataReceived += MainServer_NewDataReceived;
        }

        private void WebSocketServer_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value)
        {
            PermissionLevel level;
            while (!clients.TryRemove(session.SessionID, out level)) { Thread.Sleep(1); }
        }

        private void WebSocketServer_NewMessageReceived(WebSocketSession session, string value)
        {
            try
            {
                if (value.Length > 16384)
                    return;
                WebSocketPackets.CPanelPacket packet = new WebSocketPackets.CPanelPacket();
                packet.DeserializeJson(value);
                switch (packet.PacketType)
                {
                    case WebSocketPackets.CPanelPacketType.AuthRequest:
                        WebSocketPackets.CPanelAuthPacket authPacket = new WebSocketPackets.CPanelAuthPacket();
                        authPacket.DeserializeJson(packet.JsonData);
                        byte[] data = rsa.DecryptValue(authPacket.PasswordData);
                        PermissionLevel level, newLevel;
                        newLevel = dataController.GetPermissionByCode(Encoding.UTF8.GetString(data, 8, data.Length - 8));
                        if (newLevel == PermissionLevel.WrongPassword)
                        {
                            session.Send(new WebSocketPackets.CPanelInfoPacket()
                            {
                                Level = newLevel
                            }.SerializeJson());
                        }
                        else
                        {
                            while (!clients.TryRemove(session.SessionID, out level)) { Thread.Sleep(1); }
                            while (!clients.TryAdd(session.SessionID, newLevel)) { Thread.Sleep(1); }
                            session.Send(new WebSocketPackets.CPanelInfoPacket()
                            {
                                Level = newLevel
                            }.SerializeJson());
                            if (newLevel >= PermissionLevel.Workers)
                            {
                                session.Send(new WebSocketPackets.GeneralInfoPacket()
                                {
                                    TasksCount = dataController.TaskQueueGetCount(),
                                    WorkerCount = workers.Count
                                }.SerializeJson());
                                session.Send(new WebSocketPackets.StartCPanelPacket()
                                {
                                    Workers = workers.Values.Select(x => new WebSocketPackets.WorkerInfo()
                                    {
                                        WorkerProgress = x.LastProgress,
                                        WorkerId = x.GetId(),
                                        WorkerStatus = x.Status,
                                        WorkerSystem = x.System,
                                        CurrentTask = (newLevel < PermissionLevel.WorkersTasks ? null : (dataController.CurrentTasksGetByKey(x.GetId()) != null ? new WebSocketPackets.TaskInfo(dataController.CurrentTasksGetByKey(x.GetId()), WebSocketPackets.TaskStatus.Processing) : null))
                                    }).ToArray(),
                                    ReadyTasks = dataController.ReadyTasksGetAll().Select(x => new WebSocketPackets.TaskInfo(x, WebSocketPackets.TaskStatus.Ready)).ToArray(),
                                    InQueueTasks = dataController.TaskQueueGetAll().Select(x => new WebSocketPackets.TaskInfo(x, WebSocketPackets.TaskStatus.InQueue)).ToArray()
                                }.SerializeJson());
                            }
                        }
                        break;
                    case WebSocketPackets.CPanelPacketType.NewTask:
                        if (clients[session.SessionID] >= PermissionLevel.CreateTasks)
                        {
                            WebSocketPackets.CPanelTaskInputPacket inputPacket = new WebSocketPackets.CPanelTaskInputPacket();
                            inputPacket.DeserializeJson(packet.JsonData);
                            if (inputPacket.Data.Length > 65536)
                                break;
                            SendTask(inputPacket.Data);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Debug($"Exception on WS packet: {e}");
            }
        }

        private void WebSocketServer_NewSessionConnected(WebSocketSession session)
        {
            PermissionLevel defaultPermission = (PermissionLevel)config["defaultpermission"].Value<int>();
            while (!clients.TryAdd(session.SessionID, (PermissionLevel)config["defaultpermission"].Value<int>())) { Thread.Sleep(1); }
            session.Send(new WebSocketPackets.CPanelRSAKPInfo()
            {
                Exponent = rsa.ExportParameters(false).Exponent,
                Modulus = rsa.ExportParameters(false).Modulus
            }.SerializeJson());
            session.Send(new WebSocketPackets.CPanelInfoPacket()
            {
                Level = defaultPermission
            }.SerializeJson());
            if (defaultPermission >= PermissionLevel.Workers)
            {
                session.Send(new WebSocketPackets.GeneralInfoPacket()
                {
                    TasksCount = dataController.TaskQueueGetCount(),
                    WorkerCount = workers.Count
                }.SerializeJson());
                session.Send(new WebSocketPackets.StartCPanelPacket()
                {
                    Workers = workers.Values.Select(x => new WebSocketPackets.WorkerInfo()
                    {
                        WorkerProgress = x.LastProgress,
                        WorkerId = x.GetId(),
                        WorkerStatus = x.Status,
                        WorkerSystem = x.System,
                        CurrentTask = (defaultPermission < PermissionLevel.WorkersTasks ? null : (dataController.CurrentTasksGetByKey(x.GetId()) != null ? new WebSocketPackets.TaskInfo(dataController.CurrentTasksGetByKey(x.GetId()), WebSocketPackets.TaskStatus.Processing) : null))
                    }).ToArray(),
                    ReadyTasks = dataController.ReadyTasksGetAll().Select(x => new WebSocketPackets.TaskInfo(x, WebSocketPackets.TaskStatus.Ready)).ToArray(),
                    InQueueTasks = dataController.TaskQueueGetAll().Select(x => new WebSocketPackets.TaskInfo(x, WebSocketPackets.TaskStatus.InQueue)).ToArray()
                }.SerializeJson());
            }
        }

        private void MainServer_NewDataReceived(WebSocketSession session, byte[] value)
        {
            try
            {
                if (workers.ContainsKey(session.SessionID) && value.Length <= 65536)
                    workers[session.SessionID].OnMessage(value);
            }
            catch (Exception e)
            {
                Logger.Debug($"Exception on packet: {e}");
            }
        }

        private void MainServer_NewSessionConnected(WebSocketSession session)
        {
            var client = new MultiServerClient(this, new ClientInfo()
            {
                Id = session.SessionID,
                Config = config
            }, session);
            while (!workers.TryAdd(session.SessionID, client)) { Thread.Sleep(1); }
            SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
            {
                EventType = WebSocketPackets.EventType.WorkerConnected,
                WorkerId = session.SessionID,
            });
            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
            {
                TasksCount = dataController.TaskQueueGetCount(),
                WorkerCount = workers.Count
            });
            Packets.WorkerAuthRequest request = new Packets.WorkerAuthRequest();
            client.RandomBytes = request.RandomBytes;
            client.SendPacket(request.GetPacket());
            Logger.Debug($"Client {session.SessionID} connected from IP:Port: {session.RemoteEndPoint}");
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
            taskId++;
            Task task = new Task()
            {
                Id = taskId,
                InputData = data
            };
            SendToWebSocket(new WebSocketPackets.CPanelTaskChanged()
            {
                Task = new WebSocketPackets.TaskInfo(task, WebSocketPackets.TaskStatus.InQueue),
                TaskEventType = WebSocketPackets.TaskEventType.AddedToEnd
            });
            dataController.TaskQueueAddBack(task);
            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
            {
                TasksCount = dataController.TaskQueueGetCount(),
                WorkerCount = workers.Count
            });
            return taskId;
        }

        private void mainSelectorThreadVoid()
        {
            while (working)
            {
                Thread.Sleep(100);
                if (dataController.TaskQueueGetCount() > 0)
                {
                    string workerId = null;
                    foreach (var worker in workers)
                    {
                        if (worker.Value.Status == ClientWorkerStatus.None)
                        {
                            workerId = worker.Key;
                            break;
                        }
                    }
                    if (workerId == null)
                        continue;
                    Task task = dataController.TaskQueuePopFront();
                    task.WorkerId = workerId;
                    dataController.CurrentTasksSetByKey(workerId, task);
                    SendToWebSocket(new WebSocketPackets.CPanelTaskChanged()
                    {
                        Task = new WebSocketPackets.TaskInfo(task, WebSocketPackets.TaskStatus.Processing),
                        TaskEventType = WebSocketPackets.TaskEventType.WorkerSet
                    });
                    SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                    {
                        EventType = WebSocketPackets.EventType.GeneralInfo,
                        TasksCount = dataController.TaskQueueGetCount(),
                        WorkerCount = workers.Count
                    });
                    if (!workers[workerId].StartWork(dataController.CurrentTasksGetByKey(workerId).InputData, (x) =>
                    {
                        Task t;
                        t = dataController.CurrentTasksGetByKey(workerId);
                        t.OutputData = x;
                        dataController.ReadyTasksAdd(t);
                        dataController.CurrentTasksSetByKey(workerId, null);
                        SendToWebSocket(new WebSocketPackets.CPanelTaskChanged()
                        {
                            Task = new WebSocketPackets.TaskInfo(t, WebSocketPackets.TaskStatus.Ready),
                            TaskEventType = WebSocketPackets.TaskEventType.TaskReady
                        });
                        workers[workerId].Status = ClientWorkerStatus.None;
                    }, (x) =>
                    {
                        SendToWebSocket(new WebSocketPackets.CPanelTaskChanged()
                        {
                            Task = new WebSocketPackets.TaskInfo(dataController.CurrentTasksGetByKey(workerId), WebSocketPackets.TaskStatus.InQueue),
                            TaskEventType = WebSocketPackets.TaskEventType.AddedToStart
                        });
                        dataController.TaskQueueAddFront(dataController.CurrentTasksGetByKey(workerId));
                        dataController.CurrentTasksSetByKey(workerId, null);
                        SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                        {
                            TasksCount = dataController.TaskQueueGetCount(),
                            WorkerCount = workers.Count
                        });
                    }, (x) =>
                    {
                        SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
                        {
                            EventType = WebSocketPackets.EventType.WorkerStatusChange,
                            WorkerId = workerId,
                            WorkerProgress = x,
                            WorkerStatus = workers[workerId].Status,
                            WorkerSystem = workers[workerId].System
                        });
                    }))
                    {
                        dataController.TaskQueueAddFront(dataController.CurrentTasksGetByKey(workerId));
                        SendToWebSocket(new WebSocketPackets.CPanelTaskChanged()
                        {
                            Task = new WebSocketPackets.TaskInfo(dataController.CurrentTasksGetByKey(workerId), WebSocketPackets.TaskStatus.InQueue),
                            TaskEventType = WebSocketPackets.TaskEventType.AddedToStart
                        });
                        dataController.CurrentTasksSetByKey(workerId, null);
                        SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
                        {
                            TasksCount = dataController.TaskQueueGetCount(),
                            WorkerCount = workers.Count
                        });
                    }
                }
            }
        }

        public void RemoveClient(string workerId)
        {
            if (!workers.ContainsKey(workerId))
                return;
            SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
            {
                EventType = WebSocketPackets.EventType.WorkerDisconnected,
                WorkerId = workerId
            });
            MultiServerClient tc;
            while (!workers.TryRemove(workerId, out tc)) { Thread.Sleep(1); }
            if (dataController.CurrentTasksGetByKey(workerId) != null)
            {
                dataController.TaskQueueAddFront(dataController.CurrentTasksGetByKey(workerId));
                SendToWebSocket(new WebSocketPackets.CPanelTaskChanged()
                {
                    Task = new WebSocketPackets.TaskInfo(dataController.CurrentTasksGetByKey(workerId), WebSocketPackets.TaskStatus.InQueue),
                    TaskEventType = WebSocketPackets.TaskEventType.AddedToStart
                });
                dataController.CurrentTasksSetByKey(workerId, null);
            }
            SendToWebSocket(new WebSocketPackets.GeneralInfoPacket()
            {
                TasksCount = dataController.TaskQueueGetCount(),
                WorkerCount = workers.Count
            });
            Logger.Debug($"Client {workerId} removed successfully");
        }

        public void SendToWebSocket(IWebSocketPacket packet)
        {
            lock (webSocketServer)
                foreach (var session in webSocketServer.GetAllSessions())
                   session.Send(packet.SerializeJson());
        }
    }
}
