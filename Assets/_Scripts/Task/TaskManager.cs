using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace KillerCamp.TaskSystem
{
    public class TaskManager : NetworkBehaviour
    {
        [SerializeField] public List<TaskObjListEntry> tasks = new();
        
        [SerializeField] public List<TaskObjEntry> tasksObj = new();
        
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
        
        [Serializable]
        public struct TaskObjEntry : INetworkSerializable
        {
            public ulong Player;
            public int TaskId;

            public TaskObjEntry(ulong inPlayer, int inTaskId)
            {
                Player = inPlayer;
                TaskId = inTaskId;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Player);
                serializer.SerializeValue(ref TaskId);
            }
        }

        [SerializeField] private bool startWithTask;
        [SerializeField] private bool continousTasks;
        
        public static TaskManager instance;

        private void Awake()
        {
            if (instance == null) instance = this;
            
            InitializeTasks();
        }

        private void OnEnable()
        {
            GameManager.instance.OnGameStarted += OnGameStarted_Implementation;
        }

        private void OnDisable()
        {
            GameManager.instance.OnGameStarted -= OnGameStarted_Implementation;
        }

        void InitializeTasks()
        {
            foreach (var taskObj in FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID))
            {
                tasks.Add(new TaskObjListEntry(999, taskObj));
            }
            
            for (int i = 0; i < tasks.Count; i++)
            {
                tasksObj.Add(new TaskObjEntry(999, i));
            }
        }

        private void OnGameStarted_Implementation()
        {
            foreach (var player in NetworkManager.Singleton.ConnectedClients)
            {
                var playerId = NetworkManager.Singleton.ConnectedClients[player.Key].PlayerObject.NetworkObjectId;
                ServerGiveNewTaskObjRpc(playerId);
            }
        }

        private int GetTaskId(ulong clientId)
        {
            foreach (var taskObj in tasksObj)
            {
                if (taskObj.Player == 999)
                    return taskObj.TaskId;
            }
            return -1;
        }

        private TaskObjListEntry GetTaskObjById(int taskId)
        {
            if (taskId == -1)
            {
                Debug.Log("No task id found");
                return new TaskObjListEntry();
            }
            var task = tasks[taskId];
            if (task.Task != null)
            {
                return task;
            }
            return new TaskObjListEntry();
        }

        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                Debug.Log("[OnNetworkSpawn TaskManager] ClientId: " + clientId);
                ServerGiveNewTaskObjRpc(clientId);
            }
            
            base.OnNetworkSpawn();
        }
        
        [Rpc(SendTo.Server)]
        public void TryInteractWithTaskRpc(ulong player)
        {
            if (HasAuthority)
            {
                var task = tasks.Find(t => t.Player == player && t.Task != null);
                if (task.Task.TaskType != null)
                {
                    task.Task.TaskType.Execute(task.Task);
                }
            }
        }
        
        private bool AnyTasksLeft() => tasks.Exists(task => !task.Task.TaskType.HasStarted || !task.Task.TaskType.HasFinished && task.Player == 0);
        
        [Rpc(SendTo.Server)]
        public void ServerGiveNewTaskObjRpc(ulong player)
        {
            if (HasAuthority)
            {
                TaskObjEntry taskToChange;
                foreach (var task in tasksObj)
                {
                    if (task.Player == 999)
                    {
                        Debug.Log("[GiveTask] Found task for Player: " + player);
                        int taskId = GetTaskId(player);
                        taskToChange.Player = player;
                        var taskObjListEntry = GetTaskObjById(taskId);
                        taskObjListEntry.Player = player;
                        ClientUpdateTaskRpc(taskId);
                        return;
                    }
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ClientUpdateTaskRpc(int taskId, RpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                var task = GetTaskObjById(taskId);
                if (task.Task != null)
                {
                    GameObject player = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject.gameObject;
                    Debug.Log("[Client Upd Task]: " + NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject);
                    player.GetComponent<TaskComponent>().SetTask(task.Task);
                }
            }
        }
        
        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc()
        {
            
        }
    }
}