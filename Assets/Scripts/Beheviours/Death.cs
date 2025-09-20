using System.Collections;
using UnityEngine;

public class Death : MonoBehaviour
{




    [Header("Sampling")]
    public float sampleInterval = 0.5f;     // Her kaç saniyede bir konum alınsın
    public Transform groundCheck;           // Ayağın altındaki boş bir Transform
    public float groundCheckRadius = 0.15f; // Temas yarıçapı

    [Header("Layers")]
    public LayerMask safeLayers;            // Editörden birden fazla güvenli layer seç
    public LayerMask hazardLayers;          // Diken vb. tehlikeli layer'lar (opsiyonel)

    private Vector3 lastSafePosition;

    private IEnumerator Start()
    {
        lastSafePosition = transform.position;

        // Periyodik konum kaydı
        while (true)
        {
            yield return new WaitForSeconds(sampleInterval);

            // Sadece güvenli zeminde AYAK teması varsa snapshot al
            if (IsOnSafeGround())
            {
                lastSafePosition = transform.position;
            }
        }
    }

    private bool IsOnSafeGround()
    {
        if (groundCheck == null) return false;
        // Ayağın altında seçili safeLayers'tan bir collider var mı?
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, safeLayers) != null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsHazard(other))
            TeleportBack();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsHazard(collision.collider))
            TeleportBack();
    }

    private bool IsHazard(Collider2D col)
    {
        // Tag veya layer ile kontrol
        return col.CompareTag("Spike") || ((1 << col.gameObject.layer) & hazardLayers) != 0;
    }

    private void TeleportBack()
    {
        transform.position = lastSafePosition;

        // Rigidbody2D varsa hızları sıfırla ki geri ışınlandıktan sonra zıplamasın/akmasın
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }



}
