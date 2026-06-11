using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("Basic Info")]
    public string itemName = "Item";

    [Header("Pickup Settings")]
    public bool isPickupable = true;

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Interact()
    {
        if (!isPickupable)
        {
            Debug.Log($"You interacted with {itemName}!");
        }
        // If pickupable, PlayerInteraction handles pickup.
    }

    public void OnPickup(Transform holdPoint)
    {
        if (rb != null) rb.isKinematic = true;

        // You can disable collider OR collisions. Disabling collider is simplest.
        if (col != null) col.enabled = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Debug.Log($"Picked up {itemName}");
    }

    public void OnDrop()
    {
        transform.SetParent(null);

        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;

        Debug.Log($"Dropped {itemName}");
    }
}
