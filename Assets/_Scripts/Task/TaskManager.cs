using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace KillerCamp.TaskSystem
{
    public class TaskManager : NetworkBehaviour
    {
        [SerializeField] public List<TaskObject> tasks = new();

        [SerializeField] public List<TaskObjEntry> tasksObj = new();

        private Dictionary<ulong, int> playerTaskMap = new();
        
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
                tasks.Add(taskObj);
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                tasksObj.Add(new TaskObjEntry(999, i));
            }
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                ulong clientId = NetworkManager.Singleton.LocalClientId;
                Debug.Log("[OnNetworkSpawn TaskManager] ClientId: " + clientId);
            }

            base.OnNetworkSpawn();
        }
        
        private void OnGameStarted_Implementation()
        {
            GivePlayersTask();
        }
        
        public void GivePlayersTask()
        {
            int taskIndex = 0;
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var clientId = kvp.Key;
                playerTaskMap[clientId] = taskIndex;

                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new [] { clientId } }
                };
                
                ClientUpdateTaskClientRpc(taskIndex, clientRpcParams);
                taskIndex++;
            }
        }

        private int GetUnassignedTaskId(ulong clientId)
        {
            foreach (var taskObj in tasksObj)
            {
                if (taskObj.Player == 999)
                    return taskObj.TaskId;
            }
            return -1;
        }
/*
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
        }*/

        [Rpc(SendTo.Server)]
        public void TryInteractWithTaskRpc(ulong player)
        {
            var task = tasks.Find(t => t.TaskType.CurrentState == TaskState.Unassigned);
            if (task.TaskType != null)
            {
                task.TaskType.Execute(task);
            }
        }

        private bool AnyTasksLeft() => tasks.Exists(task =>
            !task.TaskType.HasStarted || !task.TaskType.HasFinished);

        [Rpc(SendTo.Server)]
        public void ServerGiveNewTaskObjRpc(ulong player)
        {
            Debug.Log("[GiveTask] Found task for Player: " + player);
            int taskId = GetUnassignedTaskId(player);
            playerTaskMap[player] = taskId;
            ClientUpdateTaskClientRpc(taskId);
        }

        [ClientRpc]
        public void ClientUpdateTaskClientRpc(int taskId, ClientRpcParams rpcParams = default)
        {
            var assignedTask = tasks[taskId];

            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            var taskComponent = player.GetComponent<TaskComponent>();

            if (assignedTask != null)
            {
                taskComponent.SetTask(assignedTask);
            }
        }

        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc()
        {
        }
    }
}