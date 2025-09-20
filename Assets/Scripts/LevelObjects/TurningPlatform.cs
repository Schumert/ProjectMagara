using UnityEngine;

public class TurningPlatform : MonoBehaviour, IPlateReactive
{
    [Header("Rotation")]
    [Tooltip("Plate basılınca BAŞLANGIÇ rotasyonuna göre eklenecek Z açısı (+/- derece).")]
    public float relativeTurnAngle = 90f;
    [Tooltip("Dönüş hızı (derece/sn).")]
    public float rotationSpeed = 90f;

    private Quaternion _startLocalRot;   // sahnedeki gerçek local rotasyon (dokunmuyoruz)
    private Quaternion _targetLocalRot;  // hedef local rotasyon

    private void Start()
    {
        _startLocalRot = transform.localRotation; // SAHNEDEKİ değeri al
        _targetLocalRot = _startLocalRot;          // hedef = mevcut
        // Not: Burada transform'a hiçbir atama yok → başlangıç pozu bozulmaz
    }

    public void PlatePressed(PressurePlate plate)
    {
        _targetLocalRot = _startLocalRot * Quaternion.Euler(0f, 0f, relativeTurnAngle);
    }

    public void PlateReleased(PressurePlate plate)
    {
        _targetLocalRot = _startLocalRot; // geri dön
    }

    private void Update()
    {
        // Mevcut local rotasyonu hedefe doğru çevir
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            _targetLocalRot,
            rotationSpeed * Time.deltaTime
        );
    }
}
