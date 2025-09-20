using UnityEngine;

namespace DYP
{
    public class PlayerAdvancedMovementInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterPushPullBridge2D m_PushPullBridge;
        [SerializeField] private BasicMovementController2D m_Controller; // NEW: overwrite için
        [SerializeField] private GhostMovementController2D ghost;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Collider2D ghostCollider;
        [SerializeField] private Collider2D playerCollider;

        [Header("Bindings (Old Input Manager)")]
        [SerializeField] private string m_HorizontalAxis = "Horizontal";
        [SerializeField] private string m_VerticalAxis = "Vertical";
        [SerializeField] private KeyCode m_GrabKey = KeyCode.E; // hold

        [Header("Grab Slowdown")]
        [SerializeField, Range(0.1f, 1f)] private float m_SpeedScaleWhileGrab = 0.5f; // %50 hız
        [SerializeField] private bool m_OnlyWhenPulling = false; // true ise sadece çekmede yavaşlat

        [Header("Toggle Character")]
        [SerializeField] private KeyCode toggleControlKey = KeyCode.Q; // possess
        private bool IsPossessing => ghost != null && ghost.Mode == GhostMode.FreeControl;
        [SerializeField] private bool freezePlayerWhenPossessed = true;
        [SerializeField] private bool zeroGhostInputInFollow = true;

        private void Reset()
        {
            if (m_PushPullBridge == null) m_PushPullBridge = GetComponent<CharacterPushPullBridge2D>();
            if (m_Controller == null) m_Controller = GetComponent<BasicMovementController2D>();
        }

        private void Awake()
        {
            if (m_PushPullBridge == null) m_PushPullBridge = GetComponent<CharacterPushPullBridge2D>();
            if (m_Controller == null) m_Controller = GetComponent<BasicMovementController2D>();
        }

        private void Update()
        {
            if (m_PushPullBridge == null) return;

            // eksen oku (paket driver da okuyacak; biz bridge ve gerekirse overwrite için kullanacağız)
            float horizontal = Input.GetAxisRaw(m_HorizontalAxis);
            float vertical = Input.GetAxisRaw(m_VerticalAxis);

            // Bridge’e push/pull için ekseni ve grab durumunu ilet
            m_PushPullBridge.SetMoveAxis(new Vector2(horizontal, vertical));
            bool grabHeld = Input.GetKey(m_GrabKey);
            m_PushPullBridge.HoldAction(grabHeld);

            // === GRAB VARKEN OYUNCUYU YAVAŞLAT ===
            if (grabHeld && m_Controller != null && m_PushPullBridge.IsGrabbing)
            {
                bool shouldSlow = true;

                if (m_OnlyWhenPulling)
                {
                    // SADECE ÇEKERKEN yavaşlat
                    shouldSlow = m_PushPullBridge.IsPullingWithInput(horizontal);
                }

                if (shouldSlow)
                {
                    float scaledX = horizontal * m_SpeedScaleWhileGrab;
                    // Paket driver'ın yazdığını overwrite ediyoruz (execution order: bu script DAHA SONRA çalışsın)
                    m_Controller.InputMovement(new Vector2(scaledX, vertical));
                }
            }


            if (Input.GetKeyDown(toggleControlKey))
                TogglePossession();
        }

        private void TogglePossession()
        {
            if (!IsPossessing)
            {
                ghost.TakeControl();

                if (freezePlayerWhenPossessed && m_Controller != null)
                    m_Controller.SetFrozen(true);

                if (cameraFollow != null && ghostCollider != null)
                    cameraFollow.SetMainTarget(ghostCollider);
            }
            else
            {
                ghost.ReleaseControl();

                if (freezePlayerWhenPossessed && m_Controller != null)
                    m_Controller.SetFrozen(false);

                if (cameraFollow != null && playerCollider != null)
                    cameraFollow.SetMainTarget(playerCollider);
            }
        }
    }


}
