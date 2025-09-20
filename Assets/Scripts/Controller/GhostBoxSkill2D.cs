using System.Collections.Generic;
using UnityEngine;

namespace DYP
{
    /// <summary>
    /// Ghost kutu skilli (Rigidbody2D ile):
    /// - E double-tap: prefab kutu spawn
    /// - E hold: en yakın kutuyu kontrol et (RB hız verilir), ghost yerinde durur
    /// </summary>
    public class GhostBoxSkill2D : MonoBehaviour
    {
        [Header("Spawn")]
        [SerializeField] private GameObject boxPrefab;              // İçinde: Rigidbody2D + Collider2D olmalı
        [SerializeField] private Vector2 spawnOffset = new Vector2(0.5f, 0.5f);

        [Header("Detection")]
        [SerializeField] private LayerMask boxLayer;                // Kutu collider’larının layer’ı
        [SerializeField] private float detectRadius = 6f;

        [Header("Control")]
        [SerializeField] private float controlSpeed = 6f;           // u/s (RB.velocity ile uygulanır)
        [SerializeField] private float doubleTapWindow = 0.30f;     // sn
        [Tooltip("Tutarken RB'nin gravityScale'ini 0 yapar, bırakınca geri yükler.")]
        [SerializeField] private bool zeroGravityWhileHolding = true;
        [Tooltip("Tutarken RB drag’ini artırıp daha stabil kontrol sağlar.")]
        [SerializeField] private float dragWhileHolding = 8f;

        [Header("Limitation")]
        [SerializeField] private int boxLimit = 2;
        private int currentBox = 0;
        private List<GameObject> currentBoxes = new List<GameObject>();




        public bool IsControllingBox => _holdActive && _selectedRB != null;

        // durum
        private Rigidbody2D _selectedRB;
        private Rigidbody2D _lastSpawnedRB;
        private bool _holdActive;
        private Vector2 _heldAxis;
        private float _lastETapTime = -999f;

        // orijinal RB ayarlarını geri yüklemek için
        private float _savedGravity = 1f;
        private float _savedDrag = 0f;

        // ---------------- INPUT API ----------------
        public void OnEPressed()
        {
            float t = Time.time;
            if (t - _lastETapTime <= doubleTapWindow)
            {
                _lastETapTime = -999f;
                SpawnBox(); // spawn + otomatik seç
            }
            else
            {
                _lastETapTime = t;
            }
        }

        public void OnEHold(Vector2 axis)
        {
            _holdActive = true;
            _heldAxis = axis;

            // seçili RB yoksa bul
            if (_selectedRB == null)
            {
                // öncelik: en son spawn edilen ve menzil içindeyse
                if (_lastSpawnedRB != null &&
                    Vector2.Distance(_lastSpawnedRB.position, (Vector2)transform.position) <= detectRadius)
                {
                    CacheSelection(_lastSpawnedRB);
                }
                else
                {
                    var rb = FindNearestBoxRB();
                    if (rb != null) CacheSelection(rb);
                }
            }
        }

        public void OnEReleased()
        {
            _holdActive = false;
            _heldAxis = Vector2.zero;

            // bırakırken kutuyu serbest bırak
            if (_selectedRB != null)
            {
                // istersen X hızını bırakırken sıfırla (Y’yi fizik taşısın)
                _selectedRB.linearVelocity = new Vector2(0f, _selectedRB.linearVelocity.y);
                RestoreRBProps(_selectedRB);
            }

            _selectedRB = null;
        }

        // ---------------- PHYSICS ----------------
        private void FixedUpdate()
        {
            if (!_holdActive || _selectedRB == null) return;

            // ivmesiz, direkt hız kontrolü (anlık tepki)
            Vector2 dir = _heldAxis.sqrMagnitude > 1e-6f ? _heldAxis.normalized : Vector2.zero;

            if (zeroGravityWhileHolding)
            {
                // tam 2D kontrol: yukarı-aşağı da gider
                _selectedRB.linearVelocity = dir * controlSpeed;
            }
            else
            {
                // yerçekimi açıkken: X’i direkt kontrol et, Y’yi fizik + input katkısı ile isteğe göre ekleyebilirsin
                var v = _selectedRB.linearVelocity;
                v.x = dir.x * controlSpeed;
                // Eğer yerçekimi açıkken yukarı/aşağı da istiyorsan şu satırı aç:
                // v.y = Mathf.MoveTowards(v.y, dir.y * controlSpeed, controlSpeed * 2f * Time.fixedDeltaTime);
                _selectedRB.linearVelocity = v;
            }
        }

        // ---------------- HELPERS ----------------
        private void SpawnBox()
        {
            if (boxPrefab == null)
            {
                Debug.LogWarning("GhostBoxSkill2D: boxPrefab atanmadı!");
                return;
            }



            Vector3 pos = transform.position + (Vector3)spawnOffset;
            var go = Instantiate(boxPrefab, pos, Quaternion.identity);
            currentBoxes.Add(go);


            if (currentBoxes.Count > boxLimit)
            {
                Destroy(currentBoxes[0]);
                currentBoxes.RemoveAt(0);

            }

            var rb = go.GetComponent<Rigidbody2D>();
            var col = go.GetComponent<Collider2D>();

            if (rb == null || col == null)
            {
                Debug.LogWarning("Spawnlanan kutuda Rigidbody2D ve/veya Collider2D eksik!");
                return;
            }

            // Önerilen RB ayarları
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _lastSpawnedRB = rb;
            CacheSelection(rb); // yeni kutuyu hemen kontrol etmeye başla




        }

        private void CacheSelection(Rigidbody2D rb)
        {
            _selectedRB = rb;

            // kontrol sırasında stabilite için geçici ayar
            SaveRBProps(rb);

            if (zeroGravityWhileHolding) _selectedRB.gravityScale = 0f;
            if (dragWhileHolding >= 0f) _selectedRB.linearDamping = dragWhileHolding;
        }

        private Rigidbody2D FindNearestBoxRB()
        {
            Vector2 origin = transform.position;
            float bestDist = Mathf.Infinity;
            Rigidbody2D best = null;

            var cols = Physics2D.OverlapCircleAll(origin, detectRadius, boxLayer);
            foreach (var c in cols)
            {
                var rb = c.attachedRigidbody;
                if (rb == null) continue;

                float d = Vector2.Distance(origin, c.bounds.ClosestPoint(origin));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = rb;
                }
            }
            return best;
        }

        private void SaveRBProps(Rigidbody2D rb)
        {
            _savedGravity = rb.gravityScale;
            _savedDrag = rb.linearDamping;
        }

        private void RestoreRBProps(Rigidbody2D rb)
        {
            rb.gravityScale = _savedGravity;
            rb.linearDamping = _savedDrag;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.15f);
            Gizmos.DrawSphere(transform.position, detectRadius);
        }
#endif
    }
}
