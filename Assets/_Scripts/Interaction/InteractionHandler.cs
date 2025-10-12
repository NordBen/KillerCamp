using Unity.Netcode;
using UnityEngine;

namespace KillerCamp
{
    public class InteractionHandler : MonoBehaviour
    {
        [SerializeField] private LayerMask InteractionLayer;

        private NetworkObject interactingNetworkObject;
        private IInteract interactable;
        private bool interacting;
        
        public bool Interacting { get => interacting; set => interacting = value; }
        
        public IInteract Interactable { get => interactable; set => interactable = value; }

        public void SetInteract(NetworkObject inNetworkObject)
        {
            Interacting = inNetworkObject != null;
            interactingNetworkObject = inNetworkObject;
            if (interactingNetworkObject == null) 
            {
                interactable = null;
                return;
            }
            interactable = interactingNetworkObject.GetComponent<IInteract>();
        }

        void Update()
        {
            if (interacting && HasInteractable() && Input.GetKeyDown(KeyCode.E))
            {
                interactable.Interact();
            }
        }

        private bool HasInteractable() => interactable != null;
    }
}
