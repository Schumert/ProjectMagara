using UnityEngine;

namespace DYP
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Raycaster))]
    public class BoxMotor2D : BaseMotor2D
    {
        [Header("Reference")]
        [SerializeField] private Raycaster m_Raycaster;

        [Header("Collision")]
        [SerializeField] private LayerMask m_CollisionMask;

        public bool IsGrounded { get; private set; }
        private Vector3 m_GroundedNormal = Vector3.up;

        private void Reset()
        {
            m_Raycaster = GetComponent<Raycaster>();
        }

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            m_Raycaster.Init();
            Velocity = Vector3.zero;
            Raw_Velocity = Vector3.zero;
            ExternalForce = Vector3.zero;
            IsGrounded = false;
            IsOnMovingMotor = false;
        }

        public override void Move(Vector3 displacement, bool onMovingMotor = false)
        {
            m_Raycaster.UpdateRaycastOrigins();
            IsOnMovingMotor = onMovingMotor;

            Raw_Velocity = displacement + ExternalForce;
            ExternalForce = Vector3.zero;

            Vector3 v = Raw_Velocity;

            if (Mathf.Abs(v.x) > 0.000001f) HorizontalCollisions(ref v);
            if (Mathf.Abs(v.y) > 0.000001f) VerticalCollisions(ref v);

            transform.Translate(v);
            Velocity = v;
        }

        private void HorizontalCollisions(ref Vector3 v)
        {
            float dirX = Mathf.Sign(v.x);
            float rayLen = Mathf.Abs(v.x) + Raycaster.c_SkinWidth;

            for (int i = 0; i < m_Raycaster.HorizontalRayCount; i++)
            {
                Vector2 origin = (dirX == -1) ? m_Raycaster.Origins.BottomLeft : m_Raycaster.Origins.BottomRight;
                origin += Vector2.up * (m_Raycaster.HorizontalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dirX, rayLen, m_CollisionMask);
                Debug.DrawRay(origin, Vector2.right * dirX * rayLen, Color.yellow);

                if (!hit) continue;
                if (hit.distance == 0) continue;

                v.x = (hit.distance - Raycaster.c_SkinWidth) * dirX;
                rayLen = hit.distance;
            }
        }

        private void VerticalCollisions(ref Vector3 v)
        {
            IsGrounded = false;

            float dirY = Mathf.Sign(v.y);
            float rayLen = Mathf.Abs(v.y) + Raycaster.c_SkinWidth;

            for (int i = 0; i < m_Raycaster.VerticalRayCount; i++)
            {
                Vector2 origin = (dirY == -1) ? m_Raycaster.Origins.BottomLeft : m_Raycaster.Origins.TopLeft;
                origin += Vector2.right * (m_Raycaster.VerticalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up * dirY, rayLen, m_CollisionMask);
                Debug.DrawRay(origin, Vector2.up * dirY * rayLen, Color.cyan);

                if (!hit) continue;
                if (hit.distance == 0) continue;

                v.y = (hit.distance - Raycaster.c_SkinWidth) * dirY;
                rayLen = hit.distance;

                if (dirY < 0)
                {
                    IsGrounded = true;
                    m_GroundedNormal = hit.normal;
                }
            }
        }

        public override void Push(Vector3 force, bool isOnMovingMotor = false)
        {
            // force burada displacement eklemesi gibi kullanılıyor (tek karelik)
            ExternalForce += force;
            if (isOnMovingMotor) IsOnMovingMotor = true;
        }
    }
}
