using UnityEngine;

namespace DYP
{
    /// <summary>
    /// GhostMovementController2D için input sürücüsü + Kutu Skill entegrasyonu.
    /// - Q: Takip <-> Serbest kontrol
    /// - E (Down x2): Kutu spawn
    /// - E (Hold): En yakın kutuyu kontrol et (ghost hareketsiz)
    /// </summary>
    public class GhostInputDriver2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GhostMovementController2D ghost;
        [SerializeField] private BasicMovementController2D playerController;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private GhostBoxSkill2D boxSkill;

        [Header("Camera")]
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Collider2D ghostCollider;

        [Header("Controls")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";

        [SerializeField] private KeyCode boxKey = KeyCode.E;

        [Header("Behaviour")]
        [SerializeField] private bool freezePlayerWhenPossessed = true;
        [SerializeField] private bool zeroGhostInputInFollow = true;

        private bool IsPossessing => ghost != null && ghost.Mode == GhostMode.FreeControl;

        private void Reset()
        {
            ghost = GetComponent<GhostMovementController2D>();
            boxSkill = GetComponent<GhostBoxSkill2D>();
        }

        private void Start()
        {
            if (ghost == null) ghost = GetComponent<GhostMovementController2D>();
            if (boxSkill == null) boxSkill = GetComponent<GhostBoxSkill2D>();

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
            if (ghostCollider == null && ghost != null)
                ghostCollider = ghost.GetComponent<Collider2D>();

            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        private void Update()
        {
            if (ghost == null) return;




            // Update() içinde
            bool eDown = Input.GetKeyDown(boxKey);
            bool eHeld = Input.GetKey(boxKey);
            bool eUp = Input.GetKeyUp(boxKey);

            float h = Input.GetAxisRaw(horizontalAxis);
            float v = Input.GetAxisRaw(verticalAxis);
            Vector2 axis = new Vector2(h, v);

            if (boxSkill != null)
            {
                if (eDown) boxSkill.OnEPressed();
                if (eHeld) boxSkill.OnEHold(axis);
                if (eUp) boxSkill.OnEReleased();
            }

            if (IsPossessing)
            {
                if (eHeld) ghost.InputMovement(Vector2.zero); // E tutulurken hayalet durur
                else ghost.InputMovement(axis);
            }
            else if (zeroGhostInputInFollow)
            {
                ghost.InputMovement(Vector2.zero);
            }

        }



    }
}
