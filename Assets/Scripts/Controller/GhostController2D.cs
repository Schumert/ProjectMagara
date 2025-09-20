using UnityEngine;

namespace DYP
{
    public enum GhostMode { Follow, FreeControl }

    /// <summary>
    /// Hayalet için 2 mod:
    /// - Follow: hedef (player) + offset’e yumuşak takip
    /// - FreeControl: girdi ile X-Y’de serbest dolaşım (yerçekimi yok)
    /// GhostMotor2D (BaseMotor2D)’yi sürer.
    /// Not: GhostMotor2D ivmesiz çalışıyor; gönderdiğin hız anında uygulanır.
    /// </summary>
    [RequireComponent(typeof(GhostMotor2D))]
    public class GhostMovementController2D : MonoBehaviour, IGhostController
    {
        [System.Serializable]
        class InputBuffer { public Vector2 Axis = Vector2.zero; }

        [Header("Mode")]
        [SerializeField] private GhostMode mode = GhostMode.Follow;

        [Header("Follow Settings")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private float smoothTime = 0.25f;
        [SerializeField] private Vector3 offset = new Vector3(-1f, 1f, 0f);

        [Header("Free Control Settings")]
        [SerializeField] private float moveSpeed = 8f; // units/sec

        [Header("Leash (Player-Ghost Mesafesi)")]
        [SerializeField] private bool useLeash = true;
        [SerializeField] private float maxDistance = 6f;         // izin verilen maksimum mesafe
        [SerializeField] private float softBand = 0.5f;          // yarıçapın içindeki yumuşak bant (opsiyonel)
        [SerializeField] private bool allowTangentSlide = true;  // dışarı bileşeni kes, teğet kalsın
        [SerializeField] private bool hardClampPosition = false; // true ise pozisyonu da clamp’ler

        [Header("References")]
        [SerializeField] private Transform target;                         // player
        [SerializeField] private BasicMovementController2D playerController; // FacingDirection okumak için (opsiyonel)
        [SerializeField] private GhostInputDriver2D ghostControllerInput;

        // runtime
        private GhostMotor2D motor;
        private Vector3 dampVelocity = Vector3.zero;
        private Vector3 desiredFollowPos;
        private float xAbs; // offset.x'in mutlak değeri
        private int lastFacing = 1;
        private bool canFollow = false;

        private readonly InputBuffer input = new InputBuffer();
        private float currentTimeStep;

        public GhostMode Mode => mode;

        private void Awake()
        {
            motor = GetComponent<GhostMotor2D>();
        }

        private void Start()
        {
            if (target == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    SetTarget(playerObj.transform);
                    playerController = playerObj.GetComponent<BasicMovementController2D>();
                }
                else
                {
                    Debug.LogWarning("Player tag'li obje bulunamadı (Ghost).");
                }
            }

            xAbs = Mathf.Abs(offset.x);
            if (playerController != null)
            {
                lastFacing = Mathf.Sign(playerController.FacingDirection) >= 0 ? 1 : -1;
                offset.x = (lastFacing >= 0) ? -xAbs : +xAbs;
            }

            if (mode == GhostMode.Follow)
                Invoke(nameof(StartFollowing), startDelay);
        }

        private void FixedUpdate()
        {
            currentTimeStep = Time.fixedDeltaTime;

            if (mode == GhostMode.Follow)
                UpdateFollow(currentTimeStep);
            else
                UpdateFree(currentTimeStep);
        }

        // ---------- Public API ----------
        public void InputMovement(Vector2 axis) => input.Axis = axis;

        public void TakeControl()
        {
            mode = GhostMode.FreeControl;
            dampVelocity = Vector3.zero; // takip yumuşatma hızını sıfırla
            ghostControllerInput.enabled = true;

        }

        public void ReleaseControl()
        {
            mode = GhostMode.Follow;
            input.Axis = Vector2.zero;
            Invoke(nameof(StartFollowing), startDelay);
            ghostControllerInput.enabled = false;
        }

        public void SetTarget(Transform t) => target = t;

        // ---------- Internals ----------
        private void StartFollowing() => canFollow = true;

        private void UpdateFollow(float dt)
        {
            if (!canFollow || target == null) return;

            // player facing’e göre offset’i ayarla (sağ >=0 -> +xAbs, sol -> -xAbs)
            if (playerController != null)
            {
                int dir = playerController.FacingDirection;
                if (dir != lastFacing)
                {
                    offset.x = (dir <= 0) ? xAbs : -xAbs;
                    lastFacing = dir;
                }
            }

            desiredFollowPos = target.position + offset;

            // kinematik takip (konum)
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredFollowPos,
                ref dampVelocity,
                smoothTime,
                followSpeed);

            // motor’a 0 hız gönder (push/harici kuvvet akışı sürsün)
            motor.Move(Vector3.zero);
        }

        private void UpdateFree(float dt)
        {
            // ivmesiz motor: gönderdiğin hız = anlık hız
            Vector2 dir = input.Axis.sqrMagnitude > 1e-6f ? input.Axis.normalized : Vector2.zero;
            Vector3 desired = new Vector3(dir.x, dir.y, 0f) * moveSpeed;

            if (useLeash && target != null)
            {
                desired = ConstrainByLeash(desired, dt);
                if (hardClampPosition) ClampPositionIfNeeded();
            }

            motor.Move(desired);
        }

        // Dışarı doğru hız bileşenini keser (teğete izin verilebilir)
        private Vector3 ConstrainByLeash(Vector3 desiredVel, float dt)
        {
            Vector2 p = transform.position;
            Vector2 c = target.position;

            Vector2 to = p - c;
            float dist = to.magnitude;

            if (dist <= maxDistance - softBand)
                return desiredVel; // tamamen serbest

            Vector2 outward = (dist > 1e-5f) ? to / dist : Vector2.right;
            Vector2 v2 = new Vector2(desiredVel.x, desiredVel.y);

            float outwardSpeed = Vector2.Dot(v2, outward);

            // Sert bölge: yarıçap aşıldı -> dışarı hızını tamamen kes
            if (dist >= maxDistance)
            {
                if (outwardSpeed > 0f)
                {
                    if (allowTangentSlide) v2 -= outward * outwardSpeed; // teğet kalır
                    else v2 = Vector2.zero;                              // tamamen durdur
                }
                return new Vector3(v2.x, v2.y, 0f);
            }

            // Yumuşak bant: dist ∈ [maxDistance-softBand, maxDistance)
            if (outwardSpeed > 0f)
            {
                float t = Mathf.InverseLerp(maxDistance - softBand, maxDistance, dist); // 0..1
                v2 -= outward * outwardSpeed * t;
            }

            return new Vector3(v2.x, v2.y, 0f);
        }

        // Pozisyonu da çember üstüne kelepçele (opsiyonel sert çözüm)
        private void ClampPositionIfNeeded()
        {
            Vector2 p = transform.position;
            Vector2 c = target.position;

            Vector2 to = p - c;
            float dist = to.magnitude;

            if (dist > maxDistance)
            {
                Vector2 clamped = c + to.normalized * maxDistance;
                transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (useLeash && target != null)
            {
                UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.2f);
                UnityEditor.Handles.DrawSolidDisc(target.position, Vector3.forward, maxDistance);
            }
        }
#endif
    }
}
