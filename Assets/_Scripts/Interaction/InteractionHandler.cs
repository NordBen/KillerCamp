using UnityEditor;
using UnityEngine;

namespace KillerCamp
{
    public class InteractionHandler : MonoBehaviour
    {
        [SerializeField] private LayerMask InteractionLayer;
        public bool DebugRay;

        [SerializeField] private GameObject hitObj;

        void Start()
        {

        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log($"Entered Trigger of {collision}");
            hitObj = collision.gameObject;

            if (hitObj.TryGetComponent<IInteract>(out IInteract interactable))
            {
                Debug.Log($"Interacted with {interactable}");
                interactable.Interact();
            }
        }

        void Update()
        {/*
        if (Physics.Raycast(new Ray(transform.position, transform.forward), out RaycastHit HitResult, 400f))
        {
            if (HitResult.transform is IInteract HitInteractable)
            {
                Debug.Log(HitResult.transform);
                HitInteractable.Interact();
            }
        }*/
        }
        /*
        private void OnDrawGizmos()
        {
            if (DebugRay)
            {
                Debug.DrawLine(transform.position, transform.forward, Color.green, 400f);
            }
        }*/
    }
}
