using UnityEngine;

namespace DYP
{
    [RequireComponent(typeof(BoxMotor2D))]
    public class BoxController2D : MonoBehaviour, IHas2DAxisMovement
    {
        [System.Serializable]
        class PhysicsSettings
        {
            public float Gravity = -25f;
            public float LinearDrag = 10f;
            public float MaxHorizontalSpeed = 4f;
        }

        [System.Serializable]
        class GrabSettings
        {
            public float GrabMoveForce = 45f;     // eksen ivmesi 
            public float BreakDistance = 3.0f;    // kopma toleransı 
            public float HoldOffsetX = 0.45f;     // karakterin önünde tutulacak ofset
            public float FollowSpeed = 12f;       // kutunun oyuncuyu yakalama hızı
            public float MaxSpeedWhenGrabbed = 6f;// grabliyken hız limiti
        }

        [Header("References")]
        [SerializeField] private BoxMotor2D m_Motor;

        [Header("Settings")]
        [SerializeField] private PhysicsSettings m_Physics = new PhysicsSettings();
        [SerializeField] private GrabSettings m_Grab = new GrabSettings();

        private Vector2 m_InputAxis;
        private Vector3 m_Velocity;
        private Transform m_Grabber;

        private float m_AnchorOffsetX = 0f; // grab anında belirlenen yerel x ofseti
        public bool IsGrabbed => m_Grabber != null;
        float IHas2DAxisMovement.MovementSpeed => m_Physics.MaxHorizontalSpeed;



        private void Reset()
        {
            m_Motor = GetComponent<BoxMotor2D>();
        }

        private void Start()
        {
            m_Velocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // Gravity
            if (!m_Motor.IsOnMovingMotor)
                m_Velocity.y += m_Physics.Gravity * dt;

            // Drag (X)
            float drag = Mathf.Exp(-m_Physics.LinearDrag * dt);
            //float drag = 1 - m_Physics.LinearDrag * dt;
            m_Velocity.x *= drag;

            // === GRAB IVME ===
            if (IsGrabbed)
            {
                if (m_Grabber == null || Vector2.Distance(m_Grabber.position, transform.position) > m_Grab.BreakDistance)
                {
                    Release();
                }
                else
                {
                    // Eksen ivmesi: it & çek için yeterli güç
                    m_Velocity.x += m_Grab.GrabMoveForce * m_InputAxis.x * dt;

                    //tether: Grab olduğunda kutu oyuncuya ışınlanmıyor, yavaş yavaş kayıyor
                    float targetX = m_Grabber.position.x + m_AnchorOffsetX;
                    float currX = transform.position.x;
                    float maxStep = m_Grab.FollowSpeed * dt;
                    float deltaX = Mathf.Clamp(targetX - currX, -maxStep, maxStep);

                    // tek-karelik “ek mesafe”yi motora pushladık: ek mesafe çünkü velocity’den ayrı, pozisyon düzeltmesi gibi çalışıyor.
                    if (Mathf.Abs(deltaX) > 0.0001f)
                        m_Motor.Push(new Vector3(deltaX, 0f, 0f));
                }
            }

            // Grabliyken kutuya hız limiti
            float maxH = IsGrabbed ? m_Grab.MaxSpeedWhenGrabbed : m_Physics.MaxHorizontalSpeed;
            m_Velocity.x = Mathf.Clamp(m_Velocity.x, -maxH, maxH);

            // Hareket
            m_Motor.Move(m_Velocity * dt);

            if (m_Motor.IsGrounded && m_Velocity.y < 0) m_Velocity.y = 0;
        }


        // Bridge bu API’leri çağırır
        public void InputMovement(Vector2 axis)
        {
            m_InputAxis = axis;
        }
        public void Grab(Transform grabber)
        {
            m_Grabber = grabber;
            // Karakterin baktığı tarafa göre anchora minik offset – kutu “tam göğse yapışmasın”
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

        // kısa dürtme (tek karelik displacement)
        public void Nudge(Vector2 displacement) => m_Motor.Push(displacement);

    }
}
