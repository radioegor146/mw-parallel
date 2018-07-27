using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using Nito.Collections;
using System.Collections.Concurrent;

namespace Master.DataControllers
{
    class SimpleDataController : IDataController
    {
        private JObject config;

        private Deque<Task> tasksInQueue = new Deque<Task>();

        private ConcurrentDictionary<string, Task> currentTasks = new ConcurrentDictionary<string, Task>();

        private ConcurrentQueue<Task> readyTasks = new ConcurrentQueue<Task>();

        private Dictionary<string, PermissionLevel> perms = new Dictionary<string, PermissionLevel>();

        public SimpleDataController(JObject config)
        {
            this.config = config;
            foreach (var kp in config["accounts"])
                perms.Add(kp["password"].Value<string>(), (PermissionLevel)kp["permissions"].Value<int>());
        }

        public Task CurrentTasksGetByKey(string key)
        {
            return (currentTasks.ContainsKey(key) ? currentTasks[key] : null);
        }

        public void CurrentTasksSetByKey(string key, Task newTask)
        {
            if (currentTasks.ContainsKey(key))
            {
                Task t;
                while (!currentTasks.TryRemove(key, out t)) { Thread.Sleep(1); }
            }
            while (!currentTasks.TryAdd(key, newTask)) { Thread.Sleep(1); }
        }

        public void ReadyTasksAdd(Task task)
        {
            readyTasks.Enqueue(task);
        }

        public Task[] ReadyTasksGetAll()
        {
            return readyTasks.ToArray();
        }

        public void TaskQueueAddBack(Task task)
        {
            lock (tasksInQueue)
                tasksInQueue.AddToBack(task);
        }

        public void TaskQueueAddFront(Task task)
        {
            lock (tasksInQueue)
                tasksInQueue.AddToFront(task);
        }

        public Task[] TaskQueueGetAll()
        {
            lock (tasksInQueue)
                return tasksInQueue.ToArray();
        }

        public int TaskQueueGetCount()
        {
            lock (tasksInQueue)
                return tasksInQueue.Count;
        }

        public Task TaskQueuePopFront()
        {
            lock (tasksInQueue)
                return tasksInQueue.RemoveFromFront();
        }

        public PermissionLevel GetPermissionByCode(string code)
        {
            lock (perms)
                if (perms.ContainsKey(code))
                    return perms[code];
                else
                    return PermissionLevel.None;
        }
    }
}
