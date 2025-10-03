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
            foreach (var kvp in playerTaskMap)
            {
                if (kvp.Key == 999) return kvp.Value;
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
        public void TryInteractWithTaskRpc(ulong player)
        {
            var task = tasks.Find(t => t.TaskType.CurrentState == TaskState.Unassigned);
            if (task.TaskType != null)
            {
                task.TaskType.Execute(task);
            }
        }

        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc()
        {
            
        }
    }
}