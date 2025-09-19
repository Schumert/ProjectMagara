using UnityEngine;
using UnityEngine.UIElements;

namespace DYP
{
    [RequireComponent(typeof(CharacterMotor2D))]
    [RequireComponent(typeof(BasicMovementController2D))]
    public class CharacterPushPullBridge2D : MonoBehaviour
    {
        [Header("References")]
        private CharacterMotor2D m_Motor;
        private BasicMovementController2D m_Controller;

        [Header("Push/Pull Settings")]
        [SerializeField] private LayerMask m_PushableBoxMask;
        [SerializeField] private float m_GrabRange = 0.6f;
        [SerializeField] private Vector2 m_GrabCheckSize = new Vector2(0.7f, 0.6f);
        [SerializeField] private float m_AutoReleaseDistance = 2.2f;
        [SerializeField] private float m_MaxNudgePerFrame = 0.2f;

        private BoxController2D m_GrabbedBox;
        private Vector2 m_InputAxis;
        private bool m_ActionHeld; // E basılı mı?
        public bool IsGrabbing => m_ActionHeld && m_GrabbedBox != null;

        public void SetMoveAxis(Vector2 axis) => m_InputAxis = axis;

        // E tuşu: performed => true, canceled => false
        public void HoldAction(bool held)
        {
            if (held && !m_ActionHeld && m_GrabbedBox == null)
            {
                var box = TryFindBoxInFront();
                if (box != null)
                {
                    box.Grab(this.transform);
                    box.InputMovement(new Vector2(m_InputAxis.x, 0));
                    m_GrabbedBox = box;
                }
            }

            if (!held && m_ActionHeld && m_GrabbedBox != null)
            {
                m_GrabbedBox.Release();
                m_GrabbedBox = null;
            }

            m_ActionHeld = held;
        }

        private void Awake()
        {
            m_Motor = GetComponent<CharacterMotor2D>();
            m_Controller = GetComponent<BasicMovementController2D>();
        }

        private void FixedUpdate()
        {
            if (m_GrabbedBox != null)
            {
                // push & pull: ekseni sürekli kutuya ilet
                m_GrabbedBox.InputMovement(new Vector2(m_InputAxis.x, 0));

                // kopma mesafesi
                if (Vector2.Distance(m_GrabbedBox.transform.position, transform.position) > m_AutoReleaseDistance)
                {
                    m_GrabbedBox.Release();
                    m_GrabbedBox = null;
                }
            }

            // Titreme önlemek: nudge sadece grab + E basılıyken
            if (m_ActionHeld && m_GrabbedBox != null)
            {
                //NudgeIfTouchingBox(Time.fixedDeltaTime);
            }
        }

        private BoxController2D TryFindBoxInFront()
        {
            int dir = (m_InputAxis.x != 0) ? (int)Mathf.Sign(m_InputAxis.x) : m_Controller.FacingDirection;

            var col = m_Motor.Collider2D;
            Vector2 center = (Vector2)col.bounds.center + Vector2.right * dir * m_GrabRange;

            Collider2D hit = Physics2D.OverlapBox(center, m_GrabCheckSize, 0f, m_PushableBoxMask);
            if (!hit) return null;

            return hit.GetComponentInParent<BoxController2D>() ?? hit.GetComponent<BoxController2D>();
        }

        private void NudgeIfTouchingBox(float dt)
        {
            if (!(m_Motor.Collisions.Left || m_Motor.Collisions.Right)) return;

            // karakterin o frame hedef yer değiştirmesi
            float dispX = m_Controller.InputVelocity.x * dt;
            if (Mathf.Abs(dispX) < 0.0005f) return;

            int dir = m_Motor.Collisions.Right ? 1 : -1;

            var col = m_Motor.Collider2D;
            Vector2 edgeCenter = (Vector2)col.bounds.center + Vector2.right * dir * (col.bounds.extents.x + 0.05f);
            Vector2 boxSize = new Vector2(0.12f, col.bounds.size.y * 0.9f);

            Collider2D hit = Physics2D.OverlapBox(edgeCenter, boxSize, 0f, m_PushableBoxMask);
            if (!hit) return;

            var box = hit.GetComponentInParent<BoxController2D>() ?? hit.GetComponent<BoxController2D>();
            if (box == null) return;

            float clamped = Mathf.Clamp(dispX, -m_MaxNudgePerFrame, m_MaxNudgePerFrame);
            box.Nudge(new Vector2(clamped, 0f));
        }



        // oyuncu - kutu x farkı (+ ise kutu oyuncunun sağında)
        public float GrabbedBoxDeltaX =>
            (m_GrabbedBox != null) ? (m_GrabbedBox.transform.position.x - transform.position.x) : 0f;

        // verilen yatay inputla ŞU AN çekiyor mu?
        public bool IsPullingWithInput(float horizontal)
        {
            if (m_GrabbedBox == null) return false;
            if (Mathf.Abs(horizontal) < 0.01f) return false;

            float rel = GrabbedBoxDeltaX;          // kutu oyuncunun sağında mı/solunda mı?
            if (Mathf.Abs(rel) < 0.001f) return false;

            // oyuncu, kutudan UZAKLAŞTIRICI yöne gidiyorsa -> çekme
            // ör: kutu sağdaysa (rel>0) sola gitmek (horizontal<0) = çekme
            return Mathf.Sign(horizontal) == -Mathf.Sign(rel);
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                var motor = GetComponent<CharacterMotor2D>();
                var controller = GetComponent<BasicMovementController2D>();
                if (motor == null || controller == null) return;

                int dir = controller.FacingDirection;
                var col = motor.Collider2D;
                Vector2 center = (Vector2)col.bounds.center + Vector2.right * dir * m_GrabRange;

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(center, m_GrabCheckSize);
            }
        }
#endif
    }
}
