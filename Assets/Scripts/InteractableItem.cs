using UnityEngine;

public class InteractableItem : MonoBehaviour, IInteractable
{
    public string itemName = "Item";

    public void Interact()
    {
        Debug.Log($"You interacted with {itemName}!");
        Destroy(gameObject); // Or any action like adding to inventory
    }
}

