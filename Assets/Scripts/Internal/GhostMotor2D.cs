using UnityEngine;

namespace DYP
{
    /// <summary>
    /// Basit Ghost motoru: Verilen hız kadar hareket (no accel/decel).
    /// - Yerçekimi yok (kinematik RB2D)
    /// - Controller hızını anında uygular
    /// - Harici itme + üstel sönüm
    /// - Maks hız sınırı
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class GhostMotor2D : BaseMotor2D
    {
        [Header("Speed")]
        [Tooltip("Maksimum hız (units/sec).")]
        public float MaxSpeed = 10f;

        [Header("External Force (Push)")]
        [Tooltip("Harici kuvvetin üstel sönüm katsayısı (1/sn).")]
        public float ExternalDamping = 3f;

        [Header("Hissiyat")]
        [Tooltip("Hız çok küçükse tamamen 0'la (titreşimi keser).")]
        public float StopThreshold = 0.05f;

        private Rigidbody2D _rb;
        private Vector2 _targetVelocity2D;  // Controller'ın verdiği hız (direction * speed gibi)

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

            // 1) Harici kuvveti üstel sönümle
            if (ExternalForce.sqrMagnitude > 0f && ExternalDamping > 0f)
            {
                float decay = Mathf.Exp(-ExternalDamping * dt);
                ExternalForce *= decay;
                if (ExternalForce.sqrMagnitude < (StopThreshold * StopThreshold))
                    ExternalForce = Vector3.zero;
            }

            // 2) Nihai hız = hedef hız + harici kuvvet
            Vector2 external2D = new Vector2(ExternalForce.x, ExternalForce.y);
            Vector2 finalVel2D = _targetVelocity2D + external2D;

            // 3) Maks hız sınırı
            float mag = finalVel2D.magnitude;
            if (mag > MaxSpeed) finalVel2D = finalVel2D / mag * MaxSpeed;

            // 4) Çok küçükse sıfırla
            if (finalVel2D.magnitude < StopThreshold) finalVel2D = Vector2.zero;

            // 5) Rigidbody2D ile konum güncelle
            Vector2 newPos = _rb.position + finalVel2D * dt;
            _rb.MovePosition(newPos);

            // 6) Base alanlarını güncelle
            Velocity = new Vector3(finalVel2D.x, finalVel2D.y, 0f);
            Raw_Velocity = new Vector3(_targetVelocity2D.x, _targetVelocity2D.y, 0f);
        }

        /// <summary>
        /// Controller her frame hedef hızı gönderir (örn: input.normalized * speed).
        /// </summary>
        public override void Move(Vector3 velocity, bool onMovingMotor = false)
        {
            IsOnMovingMotor = onMovingMotor;
            _targetVelocity2D = new Vector2(velocity.x, velocity.y);
        }

        public override void Push(Vector3 force, bool isOnMovingMotor = false)
        {
            base.Push(force, isOnMovingMotor);
        }

        public void ClearInternalVelocity()
        {
            // ivme mantığı olmadığı için sadece görünür velocity'i sıfırlamak yeterli
            Velocity = Vector3.zero;
        }

        public void ClearExternalForces()
        {
            ExternalForce = Vector3.zero;
        }

        public void Teleport(Vector3 worldPosition)
        {
            _rb.position = new Vector2(worldPosition.x, worldPosition.y);
            _rb.MovePosition(_rb.position);
            ClearInternalVelocity();
            ClearExternalForces();
        }
    }
}
