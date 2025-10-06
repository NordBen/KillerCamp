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

        public BaseTask TaskType { get { return task; } }

        private bool inInteraction = false;
        private BaseTask taskToExecute;

        private GameObject interactedObj;

        private void Start()
        {
            float sabotageTime = 3f;
            if (task is TimedTask timedTask) sabotageTime = timedTask.Duration;
            
            alternativeTask = new KillerTask(sabotageTime);
            
            var killerTask = alternativeTask as KillerTask;
            killerTask.TaskToSabotage = this;
        }

        private void OnEnable()
        {
            TaskType.OnComplete += TaskTypeOnOnComplete;
        }

        private void TaskTypeOnOnComplete(ITask obj)
        {
            var player = NetworkManager.Singleton.LocalClient.PlayerObject;
            TaskManager.Instance.ServerCompleteTaskRpc(player.GetComponent<NetworkObject>().OwnerClientId);//.ServerCompleteTaskRpc(GetComponent<NetworkObject>().NetworkObjectId);
        }

        public bool CanInteract()
        {
            return task.CurrentState != TaskState.Unassigned;
        }

        public void Interact()
        {
            var playerRole = interactedObj.GetComponent<RoleComponent>().Role;
            Debug.Log("Interacting player has role: " + playerRole);
            if (playerRole == CamperRole.Camper)
            {
                Debug.Log("Camper is interacting with: " + this);
                //TaskManager.Instance.TryInteractWithTaskRpc(interactedObj.GetComponent<NetworkObject>().NetworkObjectId);
                var player = NetworkManager.Singleton.LocalClient.PlayerObject;
                TaskManager.Instance.ServerTryInteractTaskRpc(NetworkObjectId, player.GetComponent<NetworkObject>().OwnerClientId);
            }
            else if (playerRole == CamperRole.Killer)
            {/*
                Debug.Log("Killer is interacting with " + this);
                alternativeTask.Execute(this);*/
            }
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

        public void ExecuteTask(bool isKiller = false)
        {
            taskToExecute = isKiller ? alternativeTask : task;
            taskToExecute.Execute(this);
            
            taskUI.SetActive(true);

            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                timedTaskToExecute.OnTickedEvent += UpdateTimedTaskUIClientRpc;
                timedTaskToExecute.OnComplete += UnsubscribleTickUI;
            }

            taskToExecute.OnComplete += DisableTaskUI;
        }

        [ClientRpc]
        private void UpdateTimedTaskUIClientRpc(int elapsedTime)
        {
            float displayedTime = 0;
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                displayedTime = timedTaskToExecute.Duration - elapsedTime;
            }

            var uiText = taskUI.GetComponentInChildren<TMP_Text>();
            if (uiText == null) return;

            string timeString = $"Time to complete: {displayedTime}";//displayedTime.ToString("0.0");
            
            uiText.text = timeString;
            Debug.Log(timeString);
        }

        private void UnsubscribleTickUI(ITask taskToUnsubscribe)
        {
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                timedTaskToExecute.OnTickedEvent -= UpdateTimedTaskUIClientRpc;
            }
        }

        private void DisableTaskUI(ITask taskToDisable)
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