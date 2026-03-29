using UnityEngine;

public class DragAndDropActivator : MonoBehaviour
{
    public TrapController trapController;
    public ParticleSystem successParticles;
    public Animator treeAnimator;
    public string successAnimationTrigger = "NestCorkInserted";
    public string tagToCheck = "Cork";
    public ParticleSystem beesParticles; // Reference to the particle system for bees
    public AdditionalAnimationObject[] additionalObjects;

    [System.Serializable]
    public class AdditionalAnimationObject
    {
        public Animator animator;
        public string animationTrigger = "Activate";
    }

    private Vector3 mouseOffset;
    private bool isDragging = false;
    private bool isWithinTrapCollider = false;

    private void OnMouseDown()
    {
        mouseOffset = transform.position - GetMouseWorldPosition();
        isDragging = true;
    }

    private void OnMouseUp()
    {
        isDragging = false;

        if (isWithinTrapCollider && CompareTag(tagToCheck))
        {
            // Deactivate the trap, play the success animation on the tree, and destroy the draggable object
            trapController.SaveJerry();
            if (successParticles != null)
            {
                successParticles.Play();
            }
            if (treeAnimator != null)
            {
                treeAnimator.SetTrigger(successAnimationTrigger);
            }

            // Stop and destroy the bees particle system
            if (beesParticles != null)
            {
                beesParticles.Stop();
                Destroy(beesParticles.gameObject);
            }

            // Trigger additional animations
            foreach (AdditionalAnimationObject obj in additionalObjects)
            {
                if (obj.animator != null)
                {
                    obj.animator.CrossFade(obj.animationTrigger, 0);

                }
            }

            Destroy(gameObject);
        }

        isWithinTrapCollider = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Jerry"))
        {
            // Trigger the success animation instantly if the trap is not active
            if (!trapController.IsTrapActive() && treeAnimator != null)
            {
                treeAnimator.Play(successAnimationTrigger, 0, 0); // Play the success animation from the start without blending

                // Stop and destroy the bees particle system
                if (beesParticles != null)
                {
                    beesParticles.Stop();
                    Destroy(beesParticles.gameObject);
                }

                // Trigger additional animations
                foreach (AdditionalAnimationObject obj in additionalObjects)
                {
                    if (obj.animator != null)
                    {
                        obj.animator.CrossFade(obj.animationTrigger, 0);

                    }
                }

                // Destroy the draggable object
                Destroy(gameObject);
            }
        }

        if (other.CompareTag("Collider"))
        {
            isWithinTrapCollider = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Collider"))
        {
            isWithinTrapCollider = false;
        }
    }

    private void Update()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPosition() + mouseOffset;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.transform.position.y;
        return Camera.main.ScreenPointToRay(mousePos).origin;
    }
}
