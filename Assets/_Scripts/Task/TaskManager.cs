using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace KillerCamp.TaskSystem
{
    public class TaskManager : NetworkBehaviour
    {
        [SerializeField] public List<TaskObject> tasks = new();

        private Dictionary<ulong, int> playerTaskMap = new();

        [SerializeField] private bool startWithTask;
        [SerializeField] private bool continousTasks;

        public static TaskManager instance;

        private void Awake()
        {
            if (instance == null) instance = this;
            if (startWithTask) InitializeTasks();
        }

        private void Start()
        {
            GameManager.Instance.OnGameStarted += OnGameStarted_Implementation;
        }

        private void OnDisable()
        {
            GameManager.Instance.OnGameStarted -= OnGameStarted_Implementation;
        }

        void InitializeTasks()
        {
            if (!IsServer) return;
            
            foreach (var taskObj in FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID))
            {
                tasks.Add(taskObj);
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
            if (!IsServer) return;
            
            int taskIndex = 0;
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var clientId = kvp.Key;
                playerTaskMap[clientId] = taskIndex;
                
                UpdateTaskForAllClientsClientRpc(clientId, taskIndex);
                
                /*
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new [] { clientId } }
                };
                
                ClientUpdateTaskClientRpc(taskIndex, clientRpcParams);*/
                taskIndex++;
            }
        }
        
        [ClientRpc]
        public void UpdateTaskForAllClientsClientRpc(ulong clientId, int taskId, ClientRpcParams rpcParams = default)
        {
            var assignedTask = tasks[taskId];
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                var player = NetworkManager.Singleton.LocalClient.PlayerObject;
                var taskComponent = player.GetComponent<TaskComponent>();
                if (taskComponent == null) return;
                
                taskComponent.SetTask(assignedTask);
            }

            //tasks[taskId].SetTaskVisual(true);
        }

        private int GetUnassignedTaskId(ulong clientId)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].TaskType.CurrentState == TaskState.Unassigned)
                {
                    bool isAssigned = false;
                    foreach (var kvp in playerTaskMap)
                    {
                        if (kvp.Value == 1)
                        {
                            isAssigned = true;
                            break;
                        }
                    }
                    
                    if (!isAssigned) return i;
                }
            }
            return -1;
        }
        
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
        public void TryInteractWithTaskRpc(ulong taskNetworkObjectId, RpcParams rpcParams = default)
        {
            ulong playerClientId = rpcParams.Receive.SenderClientId;

            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(playerClientId, out var client)) return;

            var playerObj = client.PlayerObject;
            var taskComponent = playerObj.GetComponent<TaskComponent>();
            if (taskComponent == null) return;

            TaskObject interactedTask = null;
            foreach (var ftask in tasks)
            {
                if (ftask.GetComponent<NetworkObject>().NetworkObjectId == taskNetworkObjectId)
                {
                    interactedTask = ftask;
                }
            }
            
            if (interactedTask == null) return;
            
            if (taskComponent.CurrentTask != null) return;
            
            if (interactedTask.TaskType.CurrentState == TaskState.Finished) return;
            
            interactedTask.TaskType.Execute(interactedTask);
            /*
            var task = tasks.Find(t => t.TaskType.CurrentState == TaskState.Unassigned);
            if (task.TaskType != null)
            {
                task.TaskType.Execute(task);
            }*/
        }

        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc(ulong taskNetworkObjectId, RpcParams rpcParams = default)
        {
            ulong playerClientId = rpcParams.Receive.SenderClientId;
            
            TaskObject completedTask = null;
            int completedTaskId = -1;

            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].GetComponent<NetworkObject>().NetworkObjectId == taskNetworkObjectId)
                {
                    completedTask = tasks[i];
                    completedTaskId = i;
                    break;
                }
            }
            
            if (completedTask == null) return;

            // finish task
            //completedTask.TaskType.CurrentState = TaskState.Finished;
            
            CompleteTaskForAllClientsClientRpc(completedTaskId);
            
            int newTaskId = GetUnassignedTaskId(playerClientId);
            
            if (newTaskId != -1)
            {
                playerTaskMap[playerClientId] = newTaskId;
                // update task state to started
                UpdateTaskForAllClientsClientRpc(playerClientId, newTaskId);
            }
        }
        
        [ClientRpc]
        private void CompleteTaskForAllClientsClientRpc(int taskId)
        {
            if (taskId < 0 || taskId >= tasks.Count) return;
            
            // finish task
            //tasks[taskId].TaskType.HasFinished = true;
            
            //Set visual of task
        }
    }
}