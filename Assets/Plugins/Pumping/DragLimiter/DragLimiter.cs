using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// @kurtdekker
//
// Found from here: a resisting vibrating pressure pump handle
//
// https://forum.unity.com/threads/pump-mechanic.1606821/#post-9908826
//

public class DragLimiter : MonoBehaviour
{
	// this gets "picked up" at Start
	Vector2 Origin;

	[Header( "No fallback... you get what you get.")]
	public float AllowableDistance = 4.0f;

	[Header( "You get more and more fallback during this.")]
	public float ResistanceDistance = 4.0f;

	[Header( "Collider we're grabbing.")]
	public Collider GrabHandle;

	[Header( "How much it lags behind desired position...")]
	public AnimationCurve FallbackCurve;

	[Header( "How much shakes / vibrates")]
	public AnimationCurve VibrateCurve;

	public float ShakingFactor = 0.1f;

	Camera cam;

	void Start ()
	{
		// we're assumed to be at rest at our "zero"
		Origin = transform.position;

		neutralPosition = Origin;

		cam = Camera.main;	
	}

	GameObject grabbedThing;
	Vector2 gripOffset;

	// if we were within the allowable range, we just rest here when let go
	// if we were far away, this gets jerked back inside the radius
	Vector2 neutralPosition;

	void Update ()
	{
		// 2D only!!
		Vector2 pos = Input.mousePosition;
		Vector2 world = cam.ScreenToWorldPoint( pos);

		bool bHeld = Input.GetMouseButton(0);
		bool bUp = Input.GetMouseButtonUp(0);
		bool bDown = Input.GetMouseButtonDown(0);

		Ray ray = cam.ScreenPointToRay( pos);

		RaycastHit rch = new RaycastHit();

		if (!grabbedThing)
		{
			if (bDown)
			{
				if (GrabHandle.Raycast( ray, out rch, 100))
				{
					if (rch.collider == GrabHandle)
					{
						grabbedThing = GrabHandle.gameObject;		// grip

						gripOffset = world -(Vector2)grabbedThing.transform.position;
					}
				}
			}
		}

		if (!grabbedThing)
		{
			const float Snappiness = 7.0f;

			// move the handle
			{
				Vector2 position = GrabHandle.transform.position;
				position = Vector2.Lerp( position, neutralPosition, Snappiness * Time.deltaTime);
				GrabHandle.transform.position = position;
			}

			// move us
			{
				Vector2 position = transform.position;
				position = Vector2.Lerp( position, neutralPosition, Snappiness * Time.deltaTime);
				transform.position = position;
			}
		}

		if (grabbedThing)
		{
			Vector2 rawPosition = world - gripOffset;
			Vector2 proposedPosition = rawPosition;

			Vector2 delta = proposedPosition - Origin;

			float distance = delta.magnitude;

			Vector2 shakingOffset = Vector2.zero;

			neutralPosition = proposedPosition;

			if (distance < AllowableDistance)
			{
			}
			else
			{
				// restore neutral
				neutralPosition = Origin + delta.normalized * AllowableDistance;

				// compute shakiness / pullback
				float overage = distance - AllowableDistance;

				// scale
				overage /= ResistanceDistance;

				// fallback:
				float fallback = FallbackCurve.Evaluate( overage);

				proposedPosition -= delta.normalized * fallback;

				// shake
				float shakeMagnitude = VibrateCurve.Evaluate( overage);

				shakeMagnitude *= ShakingFactor;

				shakingOffset = Random.insideUnitCircle * shakeMagnitude;
			}

			// final computation
			proposedPosition += shakingOffset;

			transform.position = proposedPosition;

			// the dragger is unaffected
			grabbedThing.transform.position = rawPosition;

			if (bUp)
			{
				grabbedThing = null;			// let go
			}
		}
	}
}
