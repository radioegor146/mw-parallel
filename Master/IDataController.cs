using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master
{
    interface IDataController
    {
        int TaskQueueGetCount();
        Task[] TaskQueueGetAll();
        void TaskQueueAddBack(Task task);
        void TaskQueueAddFront(Task task);
        Task TaskQueuePopFront();

        Task CurrentTasksGetByKey(string key);
        void CurrentTasksSetByKey(string key, Task newTask);

        Task[] ReadyTasksGetAll();
        void ReadyTasksAdd(Task task);

        PermissionLevel GetPermissionByCode(string code);

    }
}
