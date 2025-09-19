using UnityEngine;

namespace DYP
{
    public class GhostInputDriver2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GhostMovementController2D ghost;
        [SerializeField] private BasicMovementController2D playerController;
        [SerializeField] private Transform playerTransform;

        [Header("Camera")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Collider2D ghostCollider;

        [Header("Controls")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private KeyCode toggleControlKey = KeyCode.Q;

        [Header("Behaviour")]
        [SerializeField] private bool freezePlayerWhenPossessed = true;
        [SerializeField] private bool zeroGhostInputInFollow = true;

        private bool IsPossessing => ghost != null && ghost.Mode == GhostMode.FreeControl;

        private void Reset()
        {
            ghost = GetComponent<GhostMovementController2D>();
        }

        private void Start()
        {
            if (ghost == null)
                ghost = GetComponent<GhostMovementController2D>();

            // Player ve colliderları otomatik bul
            if (playerTransform == null || playerController == null || playerCollider == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    if (playerController == null) playerController = playerObj.GetComponent<BasicMovementController2D>();
                    if (playerCollider == null) playerCollider = playerObj.GetComponent<Collider2D>();
                }
            }

            // Ghost collider yoksa bu objeden çek
            if (ghostCollider == null)
                ghostCollider = ghost.GetComponent<Collider2D>();

            // Kamera yoksa sahnedeki ana kameradan almayı dene
            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        private void Update()
        {
            if (ghost == null) return;

            if (Input.GetKeyDown(toggleControlKey))
                TogglePossession();

            if (IsPossessing)
            {
                float h = Input.GetAxisRaw(horizontalAxis);
                float v = Input.GetAxisRaw(verticalAxis);
                ghost.InputMovement(new Vector2(h, v));
            }
            else if (zeroGhostInputInFollow)
            {
                ghost.InputMovement(Vector2.zero);
            }
        }

        private void TogglePossession()
        {
            if (!IsPossessing)
            {
                ghost.TakeControl();

                if (freezePlayerWhenPossessed && playerController != null)
                    playerController.SetFrozen(true);

                // KAMERA -> GHOST
                if (cameraFollow != null && ghostCollider != null)
                    cameraFollow.SetMainTarget(ghostCollider);
            }
            else
            {
                ghost.ReleaseControl();

                if (freezePlayerWhenPossessed && playerController != null)
                    playerController.SetFrozen(false);

                // KAMERA -> PLAYER
                if (cameraFollow != null && playerCollider != null)
                    cameraFollow.SetMainTarget(playerCollider);
            }
        }
    }
}
