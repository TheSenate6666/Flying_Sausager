using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapCollider : MonoBehaviour
{
    public TrapController trapController;
    private GameObject corkedObject = null;

    private void OnTriggerEnter(Collider other)
    {
        // Check if a draggable object entered the collider
        if (other.CompareTag("drag"))
        {
            corkedObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the draggable object exited the collider
        if (other.gameObject == corkedObject)
        {
            // Deactivate the trap and destroy the draggable object
            trapController.SaveJerry();
            Destroy(corkedObject);
            corkedObject = null;
        }
    }
}