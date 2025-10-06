using System;
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

        public override void OnNetworkSpawn()
        {
            int sabotageTime = 3;
            if (task is TimedTask timedTask) sabotageTime = timedTask.Duration;
            
            alternativeTask = new KillerTask(sabotageTime);
            
            var killerTask = alternativeTask as KillerTask;
            killerTask.TaskToSabotage = this;
            
            TaskType.OnComplete += TaskTypeOnOnComplete;
            
            taskUIText = taskUI.GetComponentInChildren<TMP_Text>();
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            TaskType.OnComplete -= TaskTypeOnOnComplete;
            base.OnNetworkDespawn();
        }

        private void TaskTypeOnOnComplete()
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            TaskManager.Instance.ServerCompleteTaskRpc(player.GetComponent<NetworkObject>().OwnerClientId);
        }

        public bool CanInteract()
        {
            return task.CurrentState != TaskState.Unassigned;
        }

        public void Interact()
        {
            Debug.Log("Camper is interacting with: " + this);
            //TaskManager.Instance.TryInteractWithTaskRpc(interactedObj.GetComponent<NetworkObject>().NetworkObjectId);
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            TaskManager.Instance.ServerTryInteractTaskRpc(NetworkObjectId, player.GetComponent<NetworkObject>().OwnerClientId);
        }

        public bool IsInteracting()
        {
            return inInteraction;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            interactedObj = other.gameObject;
            Debug.Log($"Collided with {other.gameObject.name}");
            if (other.CompareTag("Player"))
            {
                Debug.Log("Player entered trigger");
                inInteraction = true;
                interactedObj.GetComponent<InteractionHandler>().SetInteract(this, gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                inInteraction = false;
            }
        }

        public void ExecuteTask(ulong playerClientId, bool isKiller = false)
        {
            BaseTask taskToExecute;
            if (isKiller) taskToExecute = alternativeTask;
            else taskToExecute = task;
            
            taskToExecute.Execute(this);
            ShowTaskUIClientRpc(playerClientId);
            
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                timedTaskToExecute.OnTickedEvent += (elapsedTime) => UpdateTimedTaskUIClientRpc(elapsedTime, isKiller);
                timedTaskToExecute.OnComplete += () => UnsubscribleTickUIClientRpc(isKiller);
            }
            
            taskToExecute.OnComplete += () => DisableTaskUIClientRpc();
        }
        
        [ClientRpc]
        public void ShowTaskUIClientRpc(ulong playerClientId)
        {
            if (NetworkManager.Singleton.LocalClientId != playerClientId) return;
            taskUI.SetActive(true);
        }

        [ClientRpc]
        private void UpdateTimedTaskUIClientRpc(int elapsedTime, bool isKiller = false)
        {
            if (taskUIText == null) return;
            
            int startDuration = 3;
            if (isKiller && alternativeTask is KillerTask killerTask) startDuration = killerTask.Duration;
            else if (task is TimedTask timedTask) startDuration = timedTask.Duration;
            
            int displayedTime = startDuration - elapsedTime;
            string timeString = $"Time to complete: {displayedTime}";
            taskUIText.text = timeString;
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
                timedTaskToUnsubscribeFrom.OnTickedEvent -= (time) => UpdateTimedTaskUIClientRpc(time, iskiller);
            }
        }

        [ClientRpc]
        private void DisableTaskUIClientRpc()
        {
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