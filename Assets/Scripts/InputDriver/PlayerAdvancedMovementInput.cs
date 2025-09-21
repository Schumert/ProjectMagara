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


        [Header("Audio")]
        [Tooltip("AudioManager kütüphanesindeki anahtarlar (varsa bunlar öncelikli kullanılır).")]
        [SerializeField] private string[] possessionSfxKeys;

        [Tooltip("Anahtar yoksa doğrudan bu clip'lerden rastgele çalınır.")]
        [SerializeField] private AudioClip[] possessionSfxClips;

        [SerializeField, Range(0f, 1f)]
        private float possessionSfxVolume = 1f;

        [SerializeField]
        private bool avoidImmediateRepeat = true;

        private int _lastPlayedIndex = -1;

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

            // --- Hareket & Grab ---
            float horizontal = Input.GetAxisRaw(m_HorizontalAxis);
            float vertical = Input.GetAxisRaw(m_VerticalAxis);

            m_PushPullBridge.SetMoveAxis(new Vector2(horizontal, vertical));
            bool grabHeld = Input.GetKey(m_GrabKey);
            m_PushPullBridge.HoldAction(grabHeld);

            if (grabHeld && m_Controller != null && m_PushPullBridge.IsGrabbing)
            {
                bool shouldSlow = true;
                if (m_OnlyWhenPulling)
                    shouldSlow = m_PushPullBridge.IsPullingWithInput(horizontal);

                if (shouldSlow)
                {
                    float scaledX = horizontal * m_SpeedScaleWhileGrab;
                    m_Controller.InputMovement(new Vector2(scaledX, vertical));
                }
            }

            // --- Jump ---
            if (m_Controller != null)
            {
                bool jumpPressed = Input.GetButtonDown("Jump");
                bool jumpHeld = Input.GetButton("Jump");
                bool jumpReleased = Input.GetButtonUp("Jump");

                if (jumpPressed)
                {
                    m_Controller.PressJump(true);

                    // 🔊 Jump sesi çal
                    AudioManager.I?.PlaySFX("jump", 1f, 1f);
                    // Eğer library’de "jump" key yoksa alternatif:
                    // AudioManager.I?.PlayRandomFromGroup("jump");
                }

                if (jumpHeld) m_Controller.HoldJump(true);
                if (jumpReleased) m_Controller.ReleaseJump(true);
            }

            // --- Possession Toggle ---
            if (Input.GetKeyDown(toggleControlKey))
                TogglePossession();
        }


        private void TogglePossession()
        {
            // sadece bir satır
            AudioManager.I?.PlayRandomFromGroup("possession", 1f);

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
