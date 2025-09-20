using System.Reflection;
using UnityEngine;

public class UnlockPlatform : MonoBehaviour, IPlateReactive
{
    private DYP.PlatformController2D _controller;
    private object _movementSettings;
    private FieldInfo _movementField;
    private FieldInfo _baseSpeedField;
    private FieldInfo _inputBufferField; // m_MovementInputBuffer (NaN guard)
    private float _originalBaseSpeed;
    private bool _originalEnabled;

    private void Awake()
    {
        _controller = GetComponent<DYP.PlatformController2D>();
        if (_controller == null)
        {
            Debug.LogError("[UnlockPlatform] PlatformController2D bulunamadı.");
            return;
        }

        _originalEnabled = _controller.enabled;

        // private m_Movement alanı
        _movementField = typeof(DYP.PlatformController2D)
            .GetField("m_Movement", BindingFlags.NonPublic | BindingFlags.Instance);

        _movementSettings = _movementField?.GetValue(_controller);
        if (_movementSettings != null)
        {
            _baseSpeedField = _movementSettings.GetType()
                .GetField("BaseSpeed", BindingFlags.Public | BindingFlags.Instance);

            if (_baseSpeedField != null)
                _originalBaseSpeed = (float)_baseSpeedField.GetValue(_movementSettings);
        }

        // NaN önlemi: m_MovementInputBuffer’ı sıfırlayabilmek için
        _inputBufferField = typeof(DYP.PlatformController2D)
            .GetField("m_MovementInputBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public void PlatePressed(PressurePlate plate)
    {
        if (_controller == null) return;

        // Controller’ı aç
        _controller.enabled = true;

        // Eski BaseSpeed’e dön
        if (_movementSettings != null && _baseSpeedField != null)
            _baseSpeedField.SetValue(_movementSettings, _originalBaseSpeed);

        // Güvenlik: input’u sıfırla (NaN ihtimaline karşı)
        _inputBufferField?.SetValue(_controller, Vector2.zero);
    }

    public void PlateReleased(PressurePlate plate)
    {
        if (_controller == null) return;

        // Güvenlik: input’u sıfırla
        _inputBufferField?.SetValue(_controller, Vector2.zero);

        // İsteğin doğrultusunda: scripti kapat
        _controller.enabled = false;
    }

    private void OnDisable()
    {
        // Objeyi kapatırsan tekrar açıldığında NaN yememesi için input’u sıfırda tut
        if (_controller != null)
            _inputBufferField?.SetValue(_controller, Vector2.zero);
    }

    private void OnDestroy()
    {
        // Sahneden çıkarken durumu eski haline bırakmak istersen
        if (_controller != null)
            _controller.enabled = _originalEnabled;
    }
}
