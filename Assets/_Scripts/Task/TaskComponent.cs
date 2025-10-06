using UnityEngine;
using Unity.Netcode;

namespace KillerCamp.TaskSystem
{
    public class TaskComponent : NetworkBehaviour
    {
        [SerializeField] private TaskObject currentTask;
        public TaskObject CurrentTask => currentTask;

        public void SetTask(TaskObject inTask)
        {
            currentTask = inTask;
            UpdateTask();
        }

        private void UpdateTask()
        {
            
        }
    }
}