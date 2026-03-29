using UnityEngine;
using System.Collections.Generic;

public class TrapController : MonoBehaviour
{
    public Animator jerryAnimator; 
    public string deathAnimationTrigger = "DeathElectroShock"; 
    public bool isTrapActive = true; 
    public Movement movementScript; 
    private bool jerrySaved = false; 

    public List<AdditionalAnimationObject> additionalObjects = new List<AdditionalAnimationObject>();

    [System.Serializable]
    public class AdditionalAnimationObject
    {
        public Animator animator;
        public string animationTrigger = "Activate";
    }

    public bool IsTrapActive()
    {
        return isTrapActive;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Jerry") && isTrapActive)
        {
            // Trigger Jerry's death animation instantly
            jerryAnimator.Play(deathAnimationTrigger, 0, 0); // Play the death animation from the start without blending
            
            // Stop movement
            movementScript.StopMovement();
            
            // Mark Jerry as saved
            if (!jerrySaved)
            {
                GameOver();
            }

            // Deactivate the trap
            SaveJerry();

            // Trigger additional animations
            foreach (AdditionalAnimationObject obj in additionalObjects)
            {
                if (obj.animator != null)
                {
                    obj.animator.SetTrigger(obj.animationTrigger); // Trigger the animation
                }
            }
        }
    }

    public void SaveJerry()
    {
        if (isTrapActive)
        {
            isTrapActive = false;
            jerrySaved = true;
        }
    }

    void GameOver()
    {
        // Game over logic
    }
}
