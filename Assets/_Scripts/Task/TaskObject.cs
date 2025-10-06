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

        public override void OnNetworkSpawn()
        {
            float sabotageTime = 3f;
            if (task is TimedTask timedTask) sabotageTime = timedTask.Duration;
            
            alternativeTask = new KillerTask(sabotageTime);
            
            var killerTask = alternativeTask as KillerTask;
            killerTask.TaskToSabotage = this;
            
            TaskType.OnComplete += TaskTypeOnOnComplete;
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
            Debug.Log($"[null test] alter: {alternativeTask == null} - task: {task == null}");
            Debug.Log($"[timed task test] alter is {alternativeTask is TimedTask} - task is {task is TimedTask}");
            taskToExecute = isKiller ? alternativeTask : task;
            taskToExecute.Execute(this);
            ShowTaskUIClientRpc(playerClientId, taskToExecute);
            
            Debug.Log($"taskToExecute type: {taskToExecute.GetType().FullName}");
            
            Debug.Log($"[timedtask + null check] tasktoexec: {taskToExecute != null && taskToExecute is TimedTask}");
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                Debug.Log("IT IS TIMED TASK");
                timedTaskToExecute.OnTickedEvent += (time) => UpdateTimedTaskUIClientRpc(time);
                timedTaskToExecute.OnComplete += () =>  UnsubscribleTickUIClientRpc();
            }
            else
            {
                Debug.Log("[ExecuteTask] no timed task to execute");
            }
            
            taskToExecute.OnComplete += () => DisableTaskUIClientRpc();
        }
        
        [ClientRpc]
        public void ShowTaskUIClientRpc(ulong playerClientId, BaseTask taskToExecuteV)
        {
            Debug.Log($"[ShowTaskUIClientRpc] Showing task first {taskToExecute}");
            this.taskToExecute  = taskToExecuteV;
            if (NetworkManager.Singleton.LocalClientId != playerClientId) return;
            taskUI.SetActive(true);
            Debug.Log($"[ShowTaskUIClientRpc] Showing task end {taskToExecute}");
        }

        [ClientRpc]
        private void UpdateTimedTaskUIClientRpc(int elapsedTime)
        {
            Debug.Log($"trying to Update timedtask elapsedTime: {elapsedTime}");
            float displayedTime = 0;
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                displayedTime = timedTaskToExecute.Duration - elapsedTime;
            }

            var uiText = taskUI.GetComponentInChildren<TMP_Text>();
            if (uiText == null) return;

            string timeString = $"Time to complete: {displayedTime}";//displayedTime.ToString("0.0");
            
            uiText.text = timeString;
            Debug.Log($"[UpdateTimedTaskUIClientRpc] : {timeString}");
        }

        [ClientRpc]
        private void UnsubscribleTickUIClientRpc()
        {
            Debug.Log($"Unsubscribing tick UI client");
            ServerUnsubTickUIRpc();
        }

        [Rpc(SendTo.Server)]
        private void ServerUnsubTickUIRpc()
        {
            Debug.Log("[ServerUnsubTickUIRpc] Unsubsricbing frp, TickUI Server ");
            if (taskToExecute is TimedTask timedTaskToExecute)
            {
                Debug.Log("[ServerUnsubTickUIRpc] task to execute timed");
                timedTaskToExecute.OnTickedEvent -= UpdateTimedTaskUIClientRpc;
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