using UnityEngine;

public class TrapActivator : MonoBehaviour
{
    public TrapController trapController;
    public ParticleSystem clickParticles;
    public GameObject buttonAnimationObject;

    void OnMouseDown()
    {
        if (trapController != null)
        {
            // Deactivate the trap
            trapController.SaveJerry();

            // Play animation on child object
            if (buttonAnimationObject != null)
            {
                Animator buttonAnimator = buttonAnimationObject.GetComponent<Animator>();
                if (buttonAnimator != null)
                {
                    buttonAnimator.SetTrigger("Activate");
                }
                else
                {
                    Debug.LogWarning("Button animation object does not have an Animator component.");
                }
            }

            // Particle effect
            if (clickParticles != null)
            {
                clickParticles.Play();
            }
        }
    }
}