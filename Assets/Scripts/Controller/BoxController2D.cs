using UnityEngine;

namespace DYP
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class BoxController2D : MonoBehaviour, IHas2DAxisMovement
    {
        [System.Serializable]
        class PhysicsSettings
        {
            public float Gravity = -25f;          // RB.gravityScale ile de oynayabilirsin
            public float LinearDrag = 2f;         // havada sürtünme
            public float MaxHorizontalSpeed = 4f;
        }

        [System.Serializable]
        class GrabSettings
        {
            public float GrabMoveForce = 45f;     // eksen ivmesi (addforce)
            public float BreakDistance = 3.0f;    // grab kopma toleransı
            public float HoldOffsetX = 0.45f;     // karakterin önünde tutulacak offset
            public float FollowSpeed = 12f;       // kutu grabber’a yaklaşma hızı
            public float MaxSpeedWhenGrabbed = 6f;
        }

        [Header("Settings")]
        [SerializeField] private PhysicsSettings m_Physics = new PhysicsSettings();
        [SerializeField] private GrabSettings m_Grab = new GrabSettings();

        private Rigidbody2D rb;
        private Vector2 m_InputAxis;
        private Transform m_Grabber;
        private float m_AnchorOffsetX = 0f;

        public bool IsGrabbed => m_Grabber != null;
        float IHas2DAxisMovement.MovementSpeed => IsGrabbed ? m_Grab.MaxSpeedWhenGrabbed : m_Physics.MaxHorizontalSpeed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            //rb.gravityScale = 1f; // ister Gravity ayarını buradan kontrol et
            rb.linearDamping = m_Physics.LinearDrag;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // === GRAB ===
            if (IsGrabbed)
            {
                if (m_Grabber == null || Vector2.Distance(m_Grabber.position, transform.position) > m_Grab.BreakDistance)
                {
                    Release();
                }
                else
                {
                    // Grab sırasında X ekseni hareketi için kuvvet uygula
                    rb.AddForce(new Vector2(m_InputAxis.x * m_Grab.GrabMoveForce, 0f));

                    // Grab konumunu yavaşça takip etsin (pozisyon düzeltme)
                    float targetX = m_Grabber.position.x + m_AnchorOffsetX;
                    float currX = transform.position.x;
                    float deltaX = targetX - currX;

                    float maxStep = m_Grab.FollowSpeed * dt;
                    deltaX = Mathf.Clamp(deltaX, -maxStep, maxStep);

                    if (Mathf.Abs(deltaX) > 0.0001f)
                        rb.MovePosition(rb.position + new Vector2(deltaX, 0f));
                }

                // Grabliyken max hız limiti
                if (Mathf.Abs(rb.linearVelocity.x) > m_Grab.MaxSpeedWhenGrabbed)
                {
                    rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * m_Grab.MaxSpeedWhenGrabbed, rb.linearVelocity.y);
                }
            }
            else
            {
                // Grablı değilken max hız limiti
                if (Mathf.Abs(rb.linearVelocity.x) > m_Physics.MaxHorizontalSpeed)
                {
                    rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * m_Physics.MaxHorizontalSpeed, rb.linearVelocity.y);
                }
            }
        }

        // Input köprüsü
        public void InputMovement(Vector2 axis) => m_InputAxis = axis;

        public void Grab(Transform grabber)
        {
            m_Grabber = grabber;

            // Karakterin baktığı yöne göre offset
            float dir = 1f;
            if (grabber.TryGetComponent<BasicMovementController2D>(out var ctrl))
                dir = Mathf.Sign(Mathf.Max(0.0001f, ctrl.FacingDirection));

            m_AnchorOffsetX = dir * m_Grab.HoldOffsetX;
        }

        public void Release()
        {
            m_Grabber = null;
            m_InputAxis = Vector2.zero;
        }

        // Tek karelik dürtme
        public void Nudge(Vector2 impulse) => rb.AddForce(impulse, ForceMode2D.Impulse);
    }
}
