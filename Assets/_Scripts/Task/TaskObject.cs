using UnityEngine;
using Unity.Netcode;
using SerializeReferenceEditor;

namespace KillerCamp.TaskSystem
{
    public class TaskObject : NetworkBehaviour, IInteract
    {
        [SerializeReference, SR] private BaseTask task;
        
        [SerializeReference, SR] private BaseTask alternativeTask;

        public BaseTask TaskType { get { return task; } }

        private bool inInteraction = false;

        private GameObject interactedObj;

        private void Start()
        {
            float sabotageTime = 3f;
            if (task is TimedTask timedTask) sabotageTime = timedTask.Duration;
            
            alternativeTask = new KillerTask(sabotageTime);
            
            var killerTask = alternativeTask as KillerTask;
            killerTask.TaskToSabotage = this;
        }

        public bool CanInteract()
        {
            return task.CurrentState != TaskState.Finished;
        }

        public void Interact()
        {
            var playerRole = interactedObj.GetComponent<RoleComponent>().Role;
            Debug.Log("Interacting player has role: " + playerRole);
            if (playerRole == CamperRole.Camper)
            {
                Debug.Log("Camper is interacting with: " + this);
                TaskManager.instance.TryInteractWithTaskRpc(interactedObj.GetComponent<NetworkObject>().NetworkObjectId);
            }
            else if (playerRole == CamperRole.Killer)
            {
                Debug.Log("Killer is interacting with " + this);
                alternativeTask.Execute(this);
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
                interactedObj.GetComponent<InteractionHandler>().SetInteract(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                inInteraction = false;
            }
        }
    }
}