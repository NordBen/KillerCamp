using UnityEngine;
using Unity.Netcode;
using SerializeReferenceEditor;
using TMPro;

namespace KillerCamp.TaskSystem
{
    public class TaskObject : NetworkBehaviour, IInteract
    {
        [SerializeReference, SR] private BaseTask task;
        
        [SerializeReference, SR] private BaseTask alternativeTask;
        
        [SerializeField] private GameObject taskUI;
        private TMP_Text taskUIText;

        public BaseTask TaskType { get { return task; } }

        private bool inInteraction = false;

        private GameObject interactedObj;
        private ulong interactingObjId;

        public override void OnNetworkSpawn()
        {
            int sabotageTime = 3;
            if (task is TimedTask timedTask) sabotageTime = timedTask.Duration;
            
            alternativeTask = new KillerTask(sabotageTime);
            
            var killerTask = alternativeTask as KillerTask;
            killerTask.TaskToSabotage = this;
            
            TaskType.OnComplete += OnTaskComplete;
            
            taskUIText = taskUI.GetComponentInChildren<TMP_Text>();
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            TaskType.OnComplete -= OnTaskComplete;
            base.OnNetworkDespawn();
        }

        private void OnTaskComplete()
        {
            TaskManager.Instance.ServerCompleteTaskRpc(interactingObjId, NetworkObjectId);
        }

        public bool CanInteract()
        {
            return task.CurrentState != TaskState.Unassigned;
        }

        public void Interact()
        {
            Debug.Log("Camper is interacting with: " + this);
            TaskManager.Instance.ServerTryInteractTaskRpc(NetworkObjectId, interactingObjId);
        }

        public bool IsInteracting()
        {
            return inInteraction;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            var networkObj = other.GetComponent<NetworkObject>();
            if (networkObj == null) return;

            interactingObjId = networkObj.OwnerClientId;

            interactedObj = other.gameObject;
            Debug.Log($"Collided with {other.gameObject.name}");
            
            inInteraction = true;
            var interactionHandler = other.GetComponent<InteractionHandler>();
            if (interactionHandler == null) return;
            interactionHandler.SetInteract(NetworkObject);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            inInteraction = false;
            interactingObjId = 0;
            interactedObj = null;
            var interactionHandler = other.GetComponent<InteractionHandler>();
            if (interactionHandler == null) return;
            interactionHandler.SetInteract(null);
        }

        public void ExecuteTask(ulong playerClientId, bool isKiller = false)
        {
            if (!IsServer) return;
            
            BaseTask taskToExecute;
            if (isKiller) taskToExecute = alternativeTask;
            else taskToExecute = task;
            
            int taskDuration = 3;
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                taskDuration = timedTaskToExecute.Duration;
                timedTaskToExecute.OnTickedEvent += (elapsedTime) => UpdateTimedTaskUIClientRpc(elapsedTime, taskDuration, isKiller);
                timedTaskToExecute.OnComplete += () => UnsubscribleTickUIClientRpc(isKiller);
            }
            ShowTaskUIClientRpc(playerClientId, taskDuration);
            taskToExecute.OnComplete += () => DisableTaskUIClientRpc(playerClientId);
            
            taskToExecute.Execute(this);
        }
        
        [ClientRpc]
        public void ShowTaskUIClientRpc(ulong playerClientId, int startTime = 3)
        {
            if (NetworkManager.Singleton.LocalClientId != playerClientId) return;
            taskUI.SetActive(true);
            UpdateTimedTaskUIClientRpc(0, startTime > 3 ? startTime : 0);
        }

        [ClientRpc]
        private void UpdateTimedTaskUIClientRpc(int elapsedTime, int startTime, bool isKiller = false)
        {
            if (taskUIText == null) return;
            int displayedTime = startTime - elapsedTime;
            if (displayedTime >= 0) taskUIText.text = $"Time to complete: {displayedTime}";
        }

        [ClientRpc]
        private void UnsubscribleTickUIClientRpc(bool isKiller = false)
        {
            ServerUnsubTickUIRpc(isKiller);
        }

        [Rpc(SendTo.Server)]
        private void ServerUnsubTickUIRpc(bool iskiller = false)
        {
            BaseTask taskToUnsubscribeFrom = null;
            if (iskiller && alternativeTask is KillerTask killerTask) taskToUnsubscribeFrom = killerTask;
            else if (task is TimedTask timedTask) taskToUnsubscribeFrom = timedTask;

            if (taskToUnsubscribeFrom != null && taskToUnsubscribeFrom is TimedTask timedTaskToUnsubscribeFrom)
            {
                timedTaskToUnsubscribeFrom.OnTickedEvent -= (time) => UpdateTimedTaskUIClientRpc(time, timedTaskToUnsubscribeFrom.Duration, iskiller);
            }
        }

        [ClientRpc]
        private void DisableTaskUIClientRpc(ulong clientId, ClientRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            taskUI.SetActive(false);
        }

        public void SetState(TaskState newState)
        {
            if (task.CurrentState == newState) return;
            
            task.SetState(newState);
            Debug.Log("Task state changed to: " + newState);
        }
    }
}