using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SimpleInputNamespace
{
    public class SteeringWheel : MonoBehaviour, ISimpleInputDraggable
    {
        public SimpleInput.AxisInput axis = new SimpleInput.AxisInput("Horizontal");

        public static SteeringWheel Instance { get; private set; }

        private Graphic wheel;
        private RectTransform wheelTR;
        private Vector2 centerPoint;

        // ── Steering range ────────────────────────────────────────────────────
        [Header("Steering Range")]
        [Tooltip("Maximum physical angle the wheel can turn in either direction.")]
        public float maximumSteeringAngle = 200f;

        // ── Turning feel ──────────────────────────────────────────────────────
        [Header("Turning Feel")]
        [Tooltip("Scales the final output value sent to the plane. " +
                 "Lower = more wheel rotation needed for the same turn. Try 0.4–0.7.")]
        public float valueMultiplier = 0.5f;

        [Tooltip("Power curve applied to the output value before it reaches the plane. " +
                 "Values above 1.0 mean small inputs do almost nothing but large inputs " +
                 "hit hard. 1.0 = linear. 1.5–2.5 = heavy/clunky feel. Try 2.0.")]
        public float steeringCurve = 2.0f;

        // ── Hold position on release ──────────────────────────────────────────
        [Header("Hold On Release")]
        [Tooltip("If true, the wheel stays wherever you leave it when you let go. " +
                 "If false, it snaps back to center (original behaviour).")]
        public bool holdPositionOnRelease = true;

        [Tooltip("Only used when holdPositionOnRelease is FALSE. " +
                 "How fast the wheel returns to center in degrees per second.")]
        public float wheelReleasedSpeed = 350f;

        // ── Resistance ────────────────────────────────────────────────────────
        [Header("Resistance")]
        [Tooltip("The wheel angle (degrees) at which resistance starts building. " +
                 "Below this threshold the wheel turns freely. Try 30–60.")]
        public float resistanceStartAngle = 40f;

        [Tooltip("How strongly the resistance fights the player past the threshold. " +
                 "This is degrees-per-second of counter-rotation applied while held. " +
                 "Try 20–80. Higher = heavier helm.")]
        public float resistanceStrength = 40f;

        [Tooltip("How much the resistance grows as the wheel approaches maximum angle. " +
                 "1.0 = linear resistance. 2.0 = resistance accelerates toward the limit. " +
                 "Try 1.5–2.5.")]
        public float resistanceCurve = 1.5f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private float wheelAngle     = 0f;
        private float wheelPrevAngle = 0f;
        private bool  wheelBeingHeld = false;

        private float m_value;
        public float Value { get { return m_value; } }
        public float Angle { get { return wheelAngle; } }

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            Instance = this;
            wheel    = GetComponent<Graphic>();
            wheelTR  = wheel.rectTransform;

            SimpleInputDragListener eventReceiver = gameObject.AddComponent<SimpleInputDragListener>();
            eventReceiver.Listener = this;
        }

        private void OnEnable()
        {
            axis.StartTracking();
            SimpleInput.OnUpdate += OnUpdate;
        }

        private void OnDisable()
        {
            wheelBeingHeld = false;
            wheelAngle = wheelPrevAngle = m_value = 0f;
            wheelTR.localEulerAngles = Vector3.zero;

            axis.StopTracking();
            SimpleInput.OnUpdate -= OnUpdate;
        }

        private void OnUpdate()
        {
            if (wheelBeingHeld)
            {
                ApplyResistance();
            }
            else if (!holdPositionOnRelease)
            {
                // Original snap-back behaviour, only active when hold is disabled
                float deltaAngle = wheelReleasedSpeed * Time.deltaTime;
                if (Mathf.Abs(deltaAngle) > Mathf.Abs(wheelAngle))
                    wheelAngle = 0f;
                else if (wheelAngle > 0f)
                    wheelAngle -= deltaAngle;
                else
                    wheelAngle += deltaAngle;
            }
            // If holdPositionOnRelease is true and wheel is not held: do nothing,
            // wheel stays exactly where the player left it.

            wheelAngle = Mathf.Clamp(wheelAngle, -maximumSteeringAngle, maximumSteeringAngle);

            // Apply visual rotation
            wheelTR.localEulerAngles = new Vector3(0f, 0f, -wheelAngle);

            // Compute output: normalise to -1…+1, then apply power curve
            // The curve is sign-preserving so left/right still work correctly
            float normalised = wheelAngle / maximumSteeringAngle;           // -1 to +1
            float curved     = Mathf.Sign(normalised)
                             * Mathf.Pow(Mathf.Abs(normalised), steeringCurve);

            m_value      = curved * valueMultiplier;
            axis.value   = m_value;
        }

        // ── Resistance ────────────────────────────────────────────────────────
        private void ApplyResistance()
        {
            float absAngle = Mathf.Abs(wheelAngle);

            // No resistance inside the free zone
            if (absAngle <= resistanceStartAngle) return;

            // How far past the free zone are we, normalised 0→1 toward max angle
            float overageRaw    = absAngle - resistanceStartAngle;
            float overageMax    = maximumSteeringAngle - resistanceStartAngle;
            float overage       = Mathf.Clamp01(overageRaw / overageMax);

            // Apply curve so resistance accelerates near the limit
            float resistanceFactor = Mathf.Pow(overage, resistanceCurve);

            // Counter-rotation pushes the wheel back toward center
            float counterDelta = resistanceStrength * resistanceFactor * Time.deltaTime;

            if (wheelAngle > 0f)
                wheelAngle -= counterDelta;
            else
                wheelAngle += counterDelta;
        }

        // ── Drag handlers ─────────────────────────────────────────────────────
        public void OnPointerDown(PointerEventData eventData)
        {
            wheelBeingHeld = true;
            centerPoint    = RectTransformUtility.WorldToScreenPoint(
                                 eventData.pressEventCamera, wheelTR.position);
            wheelPrevAngle = Vector2.Angle(Vector2.up, eventData.position - centerPoint);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 pointerPos    = eventData.position;
            float   wheelNewAngle = Vector2.Angle(Vector2.up, pointerPos - centerPoint);

            if ((pointerPos - centerPoint).sqrMagnitude >= 400f)
            {
                if (pointerPos.x > centerPoint.x)
                    wheelAngle += wheelNewAngle - wheelPrevAngle;
                else
                    wheelAngle -= wheelNewAngle - wheelPrevAngle;
            }

            wheelAngle     = Mathf.Clamp(wheelAngle, -maximumSteeringAngle, maximumSteeringAngle);
            wheelPrevAngle = wheelNewAngle;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnDrag(eventData);
            wheelBeingHeld = false;
        }
    }
}
