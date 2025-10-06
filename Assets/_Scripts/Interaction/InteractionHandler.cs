using KillerCamp.TaskSystem;
using UnityEngine;

namespace KillerCamp
{
    public class InteractionHandler : MonoBehaviour
    {
        [SerializeField] private LayerMask InteractionLayer;

        [SerializeField] private GameObject hitObj;

        private IInteract interactable;
        private bool interacting;
        
        public bool Interacting { get => interacting; set => interacting = value; }
        
        public IInteract Interactable { get => interactable; set => interactable = value; }

        public void SetInteract(IInteract inInteractable, GameObject inHitObj = null)
        {
            Interacting = true;
            interactable = inInteractable;
            hitObj = inHitObj;
        }

        void Update()
        {
            if (interacting && HasInteractable() && Input.GetKeyDown(KeyCode.E))
            {
                interactable.Interact();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D collision)
        {/*
            Debug.Log($"Entered Trigger of {collision}");
            hitObj = collision.gameObject;

            if (hitObj.TryGetComponent<IInteract>(out IInteract interactable))
            {
                Debug.Log($"Interacted with {interactable}");
                interactable.Interact();
            }*/
        }

        private bool HasInteractable() => interactable != null;
    }
}
