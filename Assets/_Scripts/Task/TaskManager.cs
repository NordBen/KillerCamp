using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace KillerCamp.TaskSystem
{
    public class TaskManager : NetworkBehaviour
    {
        public NetworkVariable<bool> HasTask;

        [SerializeField] private List<AbstractTask> taskList = new();
        
        [SerializeField] public List<TaskObjListEntry> tasks = new();
        
        [Serializable]
        public struct TaskObjListEntry
        {
            public ulong Player;
            public TaskObject Task;

            public TaskObjListEntry(ulong player, TaskObject task)
            {
                Player = player;
                Task = task;
            }
        }

        [SerializeField] private bool startWithTask;
        [SerializeField] private bool continousTasks;
        
        public static TaskManager instance;

        private void Awake()
        {
            if (instance == null) instance = this;
            
            foreach (var taskObj in FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID))
            {
                tasks.Add(new TaskObjListEntry(0, taskObj));
            }
            
            //GiveNewTaskRpc();
        }

        public override void OnNetworkSpawn()
        {
            if (HasAuthority && startWithTask)
            {
                HasTask.Value = true;
            }

            base.OnNetworkSpawn();
        }

        [Rpc(SendTo.Server)]
        public void CompleteTaskRpc()
        {
            if (HasAuthority)
            {
                HasTask.Value = false;
            }
        }
        
        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc(BaseTask inTaskToComplete, RpcParams rpcParams = default)
        {
            if (HasAuthority)
            {
                foreach (var task in taskList)
                {
                    if (task.Task == inTaskToComplete && task.Task.CurrentState == TaskState.Finished)
                    {
                        HasTask.Value = false;
                        if (AnyTasksLeft() && continousTasks) ServerGiveNewTaskRpc(rpcParams);
                    }
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void GiveNewTaskRpc(ulong player, RpcParams rpcParams = default)
        {
            if (HasAuthority)
            {
                var newTask = tasks.Find(t => t.Player == 0 && t.Task != null);
                newTask.Player = player;
                //UpdateTaskUIRpc(newTask.Task, player);
            }
        }
/*
        [Rpc(SendTo.ClientsAndHost)]
        private void UpdateTaskUIRpc(TaskObject newTask, ulong player)
        {
            
        }*/
        
        private bool AnyTasksLeft() => taskList.Exists(task => task.Started == false && task.Player == null);
        
        [Rpc(SendTo.Server)]
        public void ServerGiveNewTaskRpc(RpcParams rpcParams)
        {
            if (HasAuthority)
            {
                AbstractTask taskToChange;
                foreach (var task in taskList)
                {
                    if (task.Started == false && task.Player == 0)
                    {
                        taskToChange = task;
                        ulong clientId = rpcParams.Receive.SenderClientId;
                        taskToChange.Player = clientId;
                        taskToChange.Task.Execute(taskToChange.Player);
                        HasTask.Value = true;
                        return;
                    }
                }
            }
        }
    }
}