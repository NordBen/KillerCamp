using UnityEngine;
using SerializeReferenceEditor;

namespace KillerCamp.TaskSystem
{
    public class TaskObject : MonoBehaviour, IInteract
    {
        [SerializeReference, SR] private ITask taskType;

        public ITask TaskType { get { return taskType; } }

        public bool CanInteract()
        {
            return true;
        }

        public void Interact()
        {
            taskType.Execute(this);
        }

        public bool IsInteracting()
        {
            return false;
        }
    }
}