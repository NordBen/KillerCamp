using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

namespace KillerCamp.TaskSystem
{
    public class TaskManager : NetworkBehaviour
    {
        [SerializeField] private GameObject wrongTask;

        private Dictionary<ulong, int> playerTaskMap = new();

        [SerializeField] private bool startWithTask;
        [SerializeField] private bool continousTasks;
        
        [SerializeField] public List<TaskObject> tasks = new();

        public static TaskManager Instance;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                foreach (var taskObj in FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID))
                {
                    tasks.Add(taskObj);
                }
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
                var player = client.Value.PlayerObject;
                if (player == null) continue;
                
                if (player.GetComponent<PlayerState>().playerData.Value.Role == CamperRole.Killer) continue;
                AssignTaskToPlayerRpc(client.Key);
            }
        }

        [Rpc(SendTo.Server)]
        private void AssignTaskToPlayerRpc(ulong clientId)
        {
            var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var playerState = player.GetComponent<PlayerState>();
            if (playerState == null) return;
            if (playerState.playerData.Value.Role == CamperRole.Killer) return;
            
            ulong taskId = GetUnassignedTaskId(clientId);
            if (taskId == 0) return;
            
            var task = tasks.FirstOrDefault(t => t.NetworkObjectId == taskId);
            if (task == null) return;
            
            task.SetState(TaskState.Assigned);
            
            PlayerData playerData = playerState.playerData.Value;
            playerData.CurrentTask = taskId;
            playerState.playerData.Value = playerData;
            //playerTaskMap[clientId] = tasks.IndexOf(task);
            
            UpdateClientTaskClientRpc(clientId, taskId);
        }
        
        private ulong GetUnassignedTaskId(ulong clientId)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].TaskType.CurrentState == TaskState.Unassigned)
                {
                    return tasks[i].NetworkObjectId;
                }
            }
            return 0;
        }

        [ClientRpc]
        private void UpdateClientTaskClientRpc(ulong clientId, ulong taskId, ClientRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;

            var assignedTask = FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID)
                .First(task => task.NetworkObjectId == taskId);
            
            if (assignedTask == null) return;

            var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            var taskComponent = player.GetComponent<TaskComponent>();
            if (taskComponent == null) return;
            
            taskComponent.SetTask(assignedTask);

            if (assignedTask.TryGetComponent(out LineRenderer lineRenderer)) Destroy(lineRenderer);
            
            var line = assignedTask.gameObject.AddComponent<LineRenderer>();
            if (line == null) return;
            
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.sortingOrder = 10;
            
            line.positionCount = 2;
            line.SetPosition(0, player.transform.position);
            line.SetPosition(1, assignedTask.transform.position);
            
            assignedTask.transform.GetChild(0).gameObject.SetActive(true);
        }

        [Rpc(SendTo.Server)]
        public void ServerCompleteTaskRpc(ulong playerClientId, ulong taskObjId)
        {
            //if (!playerTaskMap.TryGetValue(playerClientId, out int completedTaskIndex)) return;

            var playerObj = NetworkManager.Singleton.ConnectedClients[playerClientId].PlayerObject;
            if (playerObj == null) return;
            
            var player = playerObj.GetComponent<PlayerState>();
            if (player == null) return;
            
            var completedTask = tasks.FirstOrDefault(task => task.NetworkObjectId == taskObjId);//var completedTask = tasks[completedTaskIndex];
            if (completedTask == null) return;
            
            completedTask.SetState(TaskState.Finished);
            
            PlayerData playerData = player.playerData.Value;
            playerData.CurrentTask = 0;
            player.playerData.Value = playerData;
            
            CompleteTaskClientRpc(taskObjId);

            var fireReward = completedTask.GetComponent<ITask>().FireReward;
            GameManager.Instance.ServerUpdateFireValueRpc(fireReward);
            
            if (continousTasks) AssignTaskToPlayerRpc(playerClientId);
        }

        [ClientRpc]
        private void CompleteTaskClientRpc(ulong taskId, ClientRpcParams rpcParams = default)
        {
            var completedTask = FindObjectsByType<TaskObject>(FindObjectsSortMode.InstanceID)
                .First(task => task.NetworkObjectId == taskId);
            
            if (completedTask == null) return;
            
            var line = completedTask.gameObject.GetComponent<LineRenderer>();
            if (line != null) Destroy(line);
            
            completedTask.gameObject.SetActive(false);
        }
        
        [Rpc(SendTo.Server)]
        public void ServerTryInteractTaskRpc(ulong taskNetworkObjectId, ulong playerClientId)
        {
            var playerObj = NetworkManager.Singleton.ConnectedClients[playerClientId].PlayerObject;
            if (playerObj == null) return;
            
            var player = playerObj.GetComponent<PlayerState>();
            if (player == null) return;

            var taskObj = tasks.Find(t => t.NetworkObjectId == taskNetworkObjectId);
            if (taskObj == null)
            {
                Debug.Log("TaskObj not found");
                return;
            }

            if (player.playerData.Value.Role == CamperRole.Killer)
            {
                taskObj.ExecuteTask(playerClientId, true);
            }
            else if (player.playerData.Value.Role == CamperRole.Camper)
            {/*
                if (!playerTaskMap.TryGetValue(playerClientId, out int assignedTaskId))
                {
                    Debug.Log("Player not assigned to task");
                    WrongTaskClientRpc();
                    return;
                }*/
                
                //if (tasks[assignedTaskId] != taskObj) return;
                if (player.playerData.Value.CurrentTask != taskNetworkObjectId)
                {
                    Debug.Log("Player not assigned to task");
                    WrongTaskClientRpc(new ClientRpcParams
                    {
                            Send = new ClientRpcSendParams { TargetClientIds = new[] { playerClientId }}
                    });
                    return;
                }
                
                taskObj.ExecuteTask(playerClientId);
                Debug.Log("Camper executed Task");
            }
        }
        
        [ClientRpc]
        public void WrongTaskClientRpc(ClientRpcParams rpcParams = default)
        {
            if (wrongTask == null) return;
            wrongTask.SetActive(true);
            Invoke(nameof(DeactivateWrongTask), 2f);
        }
        
        void DeactivateWrongTask()
        {
            wrongTask.SetActive(false);
        }
    }
}