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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using SuperWebSocket;

namespace Master
{
    class MultiServerClient
    {
        private ClientInfo info;
        private MasterHandler handler;
        private WebSocketSession session;
        private Timer timeoutTimer;
        private Timer keepAliveTimer;
        private Stopwatch keepAliveSw;

        public byte[] RandomBytes;

        public MultiServerClient(MasterHandler handler, ClientInfo info, WebSocketSession session)
        {
            this.handler = handler;
            this.info = info;
            this.session = session;
            timeoutTimer = new Timer(x =>
            {
                if (status == ClientWorkerStatus.Connecting)
                    session.Close(SuperSocket.SocketBase.CloseReason.TimeOut);
            }, null, 2000, Timeout.Infinite);
            keepAliveSw = new Stopwatch();
            keepAliveTimer = new Timer(x =>
            {
                if (!session.Connected)
                    return;
                if (!keepAliveSw.IsRunning)
                    keepAliveSw.Start();
                if (keepAliveSw.ElapsedMilliseconds > 500)
                { 
                    handler.Logger.Log($"Client {session.SessionID} is disconnected with idle of {keepAliveSw.ElapsedMilliseconds}");
                    keepAliveSw.Stop();
                    handler.RemoveClient(session.SessionID);
                    session.Close();
                    status = ClientWorkerStatus.None;
                    keepAliveTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    return;
                }
                SendPacket(new SimplePacket()
                {
                    Data = new byte[0],
                    Type = PacketType.Nop
                });
            }, null, 0, 250);
        }

        public void OnMessage(byte[] data)
        {
            SimplePacket packet = StreamUtils.ReadPacket(data);
            switch (packet.Type)
            {
                case PacketType.WorkerInfo:
                    if (Status == ClientWorkerStatus.Connecting)
                        return;
                    Packets.WorkerInfo info = new Packets.WorkerInfo(packet);
                    System = info.System;
                    switch (info.Status) 
                    {
                        case Packets.WorkerStatus.Error:
                            LastProgress = 0;
                            errorCallback?.Invoke(ErrorType.WorkerError);
                            errorCallback = null;
                            progressChangeCallback = null;
                            workCallback = null;
                            break;
                        case Packets.WorkerStatus.Working:
                            LastProgress = info.OkPart;
                            status = ClientWorkerStatus.Working;
                            progressChangeCallback?.Invoke(info.OkPart);
                            break;
                    }
                    break;

                case PacketType.WorkOutput:
                    if (status != ClientWorkerStatus.Working)
                        return;
                    LastProgress = 0;
                    workCallback?.Invoke(packet.Data);
                    errorCallback = null;
                    progressChangeCallback = null;
                    workCallback = null;
                    break;

                case PacketType.WorkerAuthResponse:
                    if (status != ClientWorkerStatus.Connecting)
                        return;
                    Packets.WorkerAuthResponse response = new Packets.WorkerAuthResponse(packet);
                    if (!response.CheckPassword(RandomBytes, this.info.Config["password"].Value<string>()))
                    {
                        handler.RemoveClient(session.SessionID);
                        session.Close();
                        return;
                    }
                    status = ClientWorkerStatus.None;
                    break;

                case PacketType.Nop:
                    keepAliveSw.Reset();
                    break;
            }
        }

        private ClientWorkerStatus status = ClientWorkerStatus.Connecting;

        public string GetId()
        {
            return session.SessionID;
        }

        public double LastProgress;
        public ClientWorkerStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                if (status == value)
                    return;
                status = value;
                handler.SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
                {
                    EventType = WebSocketPackets.EventType.WorkerStatusChange,
                    WorkerId = info.Id,
                    WorkerProgress = LastProgress,
                    WorkerStatus = status,
                    WorkerSystem = system
                });
            }
        }


        private string system = "";
        public string System
        {
            get
            {
                return system;
            }

            set
            {
                if (system == value)
                    return;
                system = value;
                handler.SendToWebSocket(new WebSocketPackets.WorkerChangedPacket()
                {
                    EventType = WebSocketPackets.EventType.WorkerStatusChange,
                    WorkerId = info.Id,
                    WorkerProgress = LastProgress,
                    WorkerStatus = status,
                    WorkerSystem = system
                });
            }
        }

        Action<byte[]> workCallback;
        Action<ErrorType> errorCallback;
        Action<double> progressChangeCallback;

        public bool StartWork(byte[] work, Action<byte[]> workCallback, Action<ErrorType> errorCallback, Action<double> progressChangeCallback)
        {
            if (Status != ClientWorkerStatus.None)
                return false;
            Status = ClientWorkerStatus.WaitingForSend;
            this.workCallback = workCallback;
            this.errorCallback = errorCallback;
            this.progressChangeCallback = progressChangeCallback;
            try
            {
                SendPacket(new Packets.WorkInput(work).ToPacket());
            }
            catch
            {
                this.workCallback = null;   
                this.errorCallback = null;
                this.progressChangeCallback = null;
                Stop();
                return false;
            }
            return true;
        }

        public void SendPacket(SimplePacket packet)
        {
            byte[] data = packet.GetBytes();
            session.Send(data, 0, data.Length);
        }

        public void Stop()
        {
            Status = ClientWorkerStatus.Stopped;
        }
    }

    enum ClientWorkerStatus
    {
        Connecting,
        None,
        WaitingForSend,
        Working,
        Stopped
    }

    enum ErrorType
    {
        SendError,
        WorkerDisconnectedError,
        WorkerError
    }
}
