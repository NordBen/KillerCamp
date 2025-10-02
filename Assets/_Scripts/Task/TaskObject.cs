using System;
using UnityEngine;
using Unity.Netcode;
using SerializeReferenceEditor;

namespace KillerCamp.TaskSystem
{
    public class TaskObject : NetworkBehaviour, IInteract
    {
        [SerializeReference, SR] private BaseTask task;

        public ITask TaskType { get { return task; } }

        private bool inInteraction = false;

        private GameObject interactedObj;

        public bool CanInteract()
        {
            return task.CurrentState != TaskState.Finished;
        }

        public void Interact()
        {
            TaskManager.instance.TryInteractWithTaskRpc(interactedObj.GetComponent<NetworkObject>().NetworkObjectId);
            //task.Execute(this);
        }

        public bool IsInteracting()
        {
            return inInteraction;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"Collided with {other.gameObject.name}");
            if (other.CompareTag("Player"))
            {
                Debug.Log("Player entered trigger");
                inInteraction = true;
                other.GetComponent<InteractionHandler>().SetInteract(this);
                interactedObj = other.gameObject;
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

    [System.Serializable]
    public struct AbstractTask : IEquatable<AbstractTask>
    {
        public bool Started => State == TaskState.Started;
        
        public TaskState State => Task.CurrentState;
        
        public BaseTask Task { get; set; }
        
        public ulong Player { get; set; }
        
        public bool Equals(AbstractTask other)
        {
            return this.Started == other.Started && this.State == other.State && this.Task == other.Task && this.Player == other.Player;
        }
    }
}