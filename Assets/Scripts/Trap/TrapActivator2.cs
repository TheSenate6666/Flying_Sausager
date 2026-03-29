using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapActivator2 : MonoBehaviour
{
    public TrapController trapController;
    public ParticleSystem clickParticles;
    public GameObject bubbleObject;
    public string bubbleIdleAnimationTrigger = "BubbleIdle";
    public string bubbleFlyAnimationTrigger = "BubbleFly";

    private Animator bubbleAnimator;

    private void Start()
    {
        bubbleAnimator = bubbleObject.GetComponent<Animator>();
        bubbleAnimator.SetTrigger(bubbleIdleAnimationTrigger);
    }

    void OnMouseDown()
{
    if (trapController != null)
    {
        // Play particle effect
        if (clickParticles != null)
        {
            clickParticles.Play(); // Play the particle system
        }

        // Destroy the bubble object
        Destroy(bubbleObject);

        // Deactivate the trap
        trapController.SaveJerry();
    }
}

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Jerry") && bubbleObject != null)
        {
            // Play bubble fly animation
            bubbleAnimator.SetTrigger(bubbleFlyAnimationTrigger);
        }
    }
}