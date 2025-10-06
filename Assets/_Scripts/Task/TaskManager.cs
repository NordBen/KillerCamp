using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace KillerCamp.TaskSystem
{
    public class TaskManager : NetworkBehaviour
    {
        [SerializeField] public List<TaskObject> tasks = new();
        
        [SerializeField] private GameObject wrongTask;

        private Dictionary<ulong, int> playerTaskMap = new();

        [SerializeField] private bool startWithTask;
        [SerializeField] private bool continousTasks;

        public static TaskManager Instance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void InitializeTasks()
        {
            foreach (var taskObj in FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID))
            {
                tasks.Add(taskObj);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeTasks();
                GameManager.Instance.OnGameStarted += OnGameStarted_Implementation;
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            GameManager.Instance.OnGameStarted -= OnGameStarted_Implementation;
            base.OnNetworkDespawn();
        }

        private void OnGameStarted_Implementation()
        {
            AssignInitialTasks();
        }

        private void AssignInitialTasks()
        {
            if (!IsServer) return;
            
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                AssignTaskToPlayer(client.Key);
            }
        }

        private void AssignTaskToPlayer(ulong clientId)
        {
            int taskId = GetUnassignedTaskId(clientId);
            if (taskId == -1) return;
            
            playerTaskMap[clientId] = taskId;
            tasks[taskId].SetState(TaskState.Assigned);
            
            UpdateClientTaskClientRpc(clientId, taskId);
        }

        [ClientRpc]
        private void UpdateClientTaskClientRpc(ulong clientId, int taskId, ClientRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            
            var assignedTask = tasks[taskId];
            
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            var taskComponent = player.GetComponent<TaskComponent>();
            if (taskComponent == null) return;
            
            taskComponent.SetTask(assignedTask);

            var line = assignedTask.gameObject.AddComponent<LineRenderer>();
            if (line == null) return;
            
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.sortingOrder = 10;
            
            line.positionCount = 2;
            line.SetPosition(0, player.transform.position);
            line.SetPosition(1, assignedTask.transform.position);
        }

        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc(ulong playerClientId)
        {
            if (!playerTaskMap.TryGetValue(playerClientId, out int completedTaskIndex)) return;

            var completedTask = tasks[completedTaskIndex];
            completedTask.SetState(TaskState.Finished);
            
            var line = completedTask.gameObject.GetComponent<LineRenderer>();
            if (line != null) Destroy(line);
            
            completedTask.gameObject.SetActive(false);
            
            AssignTaskToPlayer(playerClientId);
        }
        
        [Rpc(SendTo.Server)]
        public void ServerTryInteractTaskRpc(ulong taskNetworkObjectId, ulong playerClientId)
        {
            if (!playerTaskMap.TryGetValue(playerClientId, out int assignedTaskId))
            {
                Debug.Log("Player not assigned to task");
                return;
            }

            var taskObj = tasks.Find(t => t.NetworkObjectId == taskNetworkObjectId);
            if (taskObj == null)
            {
                Debug.Log("TaskObj not found");
                return;
            }
            
            var player = NetworkManager.Singleton.ConnectedClients[playerClientId].PlayerObject;
            if (player == null) return;
            
            var roleComponent = player.GetComponent<RoleComponent>();
            if (roleComponent == null) return;

            if (roleComponent.Role == CamperRole.Killer)
            {
                taskObj.ExecuteTask(true);
            }
            else if (roleComponent.Role == CamperRole.Camper && tasks[assignedTaskId] == taskObj)
            {
                taskObj.ExecuteTask();
            }
            else
            {
                WrongTaskClientRpc();
            }
        }
        
        [ClientRpc]
        public void WrongTaskClientRpc(ClientRpcParams rpcParams = default)
        {
            wrongTask.SetActive(true);
            Invoke(nameof(DeactivateWrongTask), 2f);
        }
        
        void DeactivateWrongTask()
        {
            wrongTask.SetActive(false);
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
                    /*
                    bool isAssigned = false;
                    foreach (var kvp in playerTaskMap)
                    {
                        if (kvp.Value == 1)
                        {
                            isAssigned = true;
                            break;
                        }
                    }
                    
                    if (!isAssigned) return i;*/
                    return i;
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