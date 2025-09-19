using UnityEngine;

namespace DYP
{
    /// <summary>
    /// 2D Ghost motoru: X-Y düzleminde serbest dolaşım.
    /// - Yerçekimi yok
    /// - Kinematik Rigidbody2D ile konum günceller
    /// - İvmelenme / yavaşlama
    /// - Harici itmeler (Push) ve zamana bağlı sönümleme
    /// - En yüksek hız sınırı
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class GhostMotor2D : BaseMotor2D
    {
        [Header("Speed & Acceleration")]
        [Tooltip("Hedef hıza (units/sec) ulaşırken kullanılan temel hız ölçeği. Genelde controller, yön vektörü * BaseSpeed yollar.")]
        public float BaseSpeed = 8f;

        [Tooltip("Hedef hıza yaklaşırken ivmelenme (units/sec^2).")]
        public float Acceleration = 40f;

        [Tooltip("Girdi kesildiğinde yavaşlama (units/sec^2).")]
        public float Deceleration = 50f;

        [Tooltip("Maksimum hız (units/sec).")]
        public float MaxSpeed = 10f;

        [Header("External Force (Push)")]
        [Tooltip("Harici kuvvetin üstel sönüm katsayısı (1/sn). Daha büyük -> daha hızlı söner.")]
        public float ExternalDamping = 3f;

        [Header("Hissiyat Ayarları")]
        [Tooltip("Hız çok küçükse tamamen 0'la (titreşimi keser).")]
        public float StopThreshold = 0.05f;

        [Tooltip("Controller'dan gelen hedef hızın, anlık velocity'e katkı oranı (0..1). 1 = tam hedefe git.")]
        [Range(0f, 1f)] public float InputAggressiveness = 1f;

        Rigidbody2D _rb;
        Vector2 _targetVelocity2D;   // Controller'ın istediği hedef hız (dünya uzayı)
        Vector2 _currentVelocity2D;  // Motorun dahili hesapladığı hız (harici kuvvet hariç)
        bool _hasNewInput;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.isKinematic = true;
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // 1) Hedef hıza ivmelen / girdi yoksa yavaşla
            Vector2 desired = _targetVelocity2D * InputAggressiveness;

            // Hangi ivme? Girdi var ise Acceleration, yoksa Deceleration
            bool hasInput = _hasNewInput && _targetVelocity2D.sqrMagnitude > 0.0001f;
            float accel = hasInput ? Acceleration : Deceleration;

            // Mevcut hız vektörünü desired'a doğru it
            _currentVelocity2D = MoveTowardsVector(_currentVelocity2D, desired, accel * dt);

            // 2) Harici kuvveti üstel sönümle
            if (ExternalForce.sqrMagnitude > 0f && ExternalDamping > 0f)
            {
                float decay = Mathf.Exp(-ExternalDamping * dt);
                ExternalForce *= decay;
                if (ExternalForce.sqrMagnitude < (StopThreshold * StopThreshold))
                    ExternalForce = Vector3.zero;
            }

            // 3) Nihai hız = motor hızı + harici kuvvet
            Vector2 external2D = new Vector2(ExternalForce.x, ExternalForce.y);
            Vector2 finalVel2D = _currentVelocity2D + external2D;

            // 4) Maks hız sınırı
            if (finalVel2D.magnitude > MaxSpeed)
                finalVel2D = finalVel2D.normalized * MaxSpeed;

            // 5) Çok küçükse sıfırla
            if (finalVel2D.magnitude < StopThreshold) finalVel2D = Vector2.zero;

            // 6) Rigidbody2D ile konum güncelle
            Vector2 newPos = _rb.position + finalVel2D * dt;
            _rb.MovePosition(newPos);

            // 7) Base alanlarını güncelle
            Velocity = new Vector3(finalVel2D.x, finalVel2D.y, 0f);
            // Raw_Velocity: controller'ın talep ettiği hedef hız (clamp'siz)
            Raw_Velocity = new Vector3(_targetVelocity2D.x, _targetVelocity2D.y, 0f);
            // IsOnMovingMotor: Push sırasında set edilebilir; burada olduğu gibi kalır

            // 8) Bu frame için girdi bitti
            _hasNewInput = false;
        }

        /// <summary>
        /// Controller her frame bunu çağırır.
        /// velocity: dünya uzayında hedef hız (örn: input.normalized * BaseSpeed)
        /// onMovingMotor: hareketli platform vb. üstündeyse true (ghost için genelde false kalır).
        /// </summary>
        public override void Move(Vector3 velocity, bool onMovingMotor = false)
        {
            _hasNewInput = true;
            IsOnMovingMotor = onMovingMotor;

            // Controller'dan gelen hedef hız (genelde z=0)
            _targetVelocity2D = new Vector2(velocity.x, velocity.y);

            // İstersen: BaseSpeed çarpanı uygulamak için yorum satırını aç
            // _targetVelocity2D *= BaseSpeed;
        }

        /// <summary>
        /// Harici itmeler için (patlama, dash çarpışması vs.)
        /// Base.Push biriktirir; burada ekstra bir şey yapmaya gerek yok.
        /// </summary>
        public override void Push(Vector3 force, bool isOnMovingMotor = false)
        {
            base.Push(force, isOnMovingMotor);
        }

        /// <summary> Motorun iç hızını anında temizle (ör. teleport sonrası). </summary>
        public void ClearInternalVelocity()
        {
            _currentVelocity2D = Vector2.zero;
            Velocity = Vector3.zero;
        }

        /// <summary> Harici kuvvetleri anında temizle. </summary>
        public void ClearExternalForces()
        {
            ExternalForce = Vector3.zero;
        }

        /// <summary> Güvenli teleport. </summary>
        public void Teleport(Vector3 worldPosition)
        {
            _rb.position = new Vector2(worldPosition.x, worldPosition.y);
            _rb.MovePosition(_rb.position); // interpolation state’i güncel
            ClearInternalVelocity();
            ClearExternalForces();
        }

        // ----------------- helpers -----------------
        static Vector2 MoveTowardsVector(Vector2 current, Vector2 target, float maxDelta)
        {
            Vector2 delta = target - current;
            float dist = delta.magnitude;
            if (dist <= maxDelta || dist < Mathf.Epsilon) return target;
            return current + delta / dist * maxDelta;
        }
    }
}
