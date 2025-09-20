using UnityEngine;

namespace DYP
{
    /// <summary>
    /// RB2D kullanmadan güvenli kinematik hareket.
    /// - Collider2D.Cast ile sweep
    /// - Depenetration (başlangıçta iç içe ise)
    /// - Yüzey boyunca kayma (tangent projeksiyon)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KinematicColliderMover2D : MonoBehaviour
    {
        [Header("Collision")]
        public LayerMask solidMask;
        [Tooltip("Yüzeyden bırakılan küçük pay")]
        public float skin = 0.02f;
        [Tooltip("Tünellemeyi azaltmak için bir hareketi bu kadarlık adımlara böler")]
        public float maxStep = 0.5f;
        [Tooltip("Tek frame’de max iterasyon")]
        public int maxIterations = 8;

        private Collider2D col;
        private ContactFilter2D filter;
        private RaycastHit2D[] hits = new RaycastHit2D[8];

        void Awake()
        {
            col = GetComponent<Collider2D>();
            filter = new ContactFilter2D { useLayerMask = true, useTriggers = true };
            filter.SetLayerMask(solidMask);
            Physics2D.queriesStartInColliders = false;
        }

        /// <summary>
        /// Delta kadar güvenli şekilde taşı. Gerçekleşen kalan delta’yı döndürür.
        /// </summary>
        public Vector2 Move(Vector2 delta)
        {
            // 0) Başlangıçta iç içeyse hafifçe çıkar
            DepenetrateIfNeeded();

            Vector2 remaining = delta;
            Vector3 startPos = transform.position;

            int totalIters = 0;
            // Büyük deltaları parçalara böl
            int steps = Mathf.Max(1, Mathf.CeilToInt(remaining.magnitude / Mathf.Max(0.0001f, maxStep)));
            Vector2 stepDelta = remaining / steps;

            for (int s = 0; s < steps; s++)
            {
                Vector2 sub = stepDelta;

                int iter = 0;
                while (iter++ < maxIterations)
                {
                    float dist = sub.magnitude;
                    if (dist <= 1e-5f) break;

                    Vector2 dir = sub / dist;
                    int count = col.Cast(dir, filter, hits, dist + skin);
                    float allowed = dist;
                    Vector2 hitNormal = Vector2.zero;
                    bool hitSomething = false;

                    for (int i = 0; i < count; i++)
                    {
                        var h = hits[i];
                        if (h.collider == null || h.collider.isTrigger) continue;
                        if (((1 << h.collider.gameObject.layer) & solidMask) == 0) continue;

                        allowed = Mathf.Min(allowed, Mathf.Max(0f, h.distance - skin));
                        hitNormal = h.normal;
                        hitSomething = true;
                    }

                    // 1) çarpmaya kadar ilerle
                    transform.position += (Vector3)(dir * allowed);

                    // 2) kalan hareket yoksa bitir
                    float remainingAfter = dist - allowed;
                    if (remainingAfter <= 1e-5f) break;

                    if (!hitSomething) break; // teorik olarak olmamalı

                    // 3) kalan vektörü yüzeye teğet projeksiyon
                    Vector2 slide = ProjectAlongTangent(dir * remainingAfter, hitNormal);
                    sub = slide;
                    totalIters++;
                    if (totalIters > maxIterations * 2) break; // güvenlik
                }
            }

            return (Vector2)transform.position - (Vector2)startPos;
        }

        private void DepenetrateIfNeeded()
        {
            // Cast zero ile depenetration yok; Distance ile çöz
            // En yakın katı ile mesafe negatifse normal yönünde dışarı it
            Collider2D[] overlaps = new Collider2D[4];
            int cnt = col.Overlap(filter, overlaps);
            for (int i = 0; i < cnt; i++)
            {
                var other = overlaps[i];
                if (other == null || other.isTrigger) continue;

                var d = Physics2D.Distance(col, other);
                if (d.isOverlapped)
                {
                    transform.position += (Vector3)(d.normal * (d.distance + skin));
                }
            }
        }

        private static Vector2 ProjectAlongTangent(Vector2 v, Vector2 normal)
        {
            // v'yi normal yönündeki bileşenden arındır
            float vn = Vector2.Dot(v, normal);
            return v - normal * vn;
        }
    }
}
