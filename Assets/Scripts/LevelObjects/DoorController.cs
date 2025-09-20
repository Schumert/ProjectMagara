using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorController : MonoBehaviour, IPlateReactive
{
    [Header("Motion")]
    public float moveSpeed = 1.5f;
    public Vector2 moveDirection = Vector2.down;

    private bool _shouldMove;
    private Rigidbody2D _rb;

    private void Reset()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = false;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    // Eski public API (istersen hâlâ direkt çağırabilirsin)
    public void StartMovingDown() => _shouldMove = true;
    public void StopMoving() => _shouldMove = false;
    public void ReachedStopPoint() { _shouldMove = false; }

    // IPlateReactive uygulaması — PressurePlate bunları çağıracak
    public void PlatePressed(PressurePlate plate)
    {
        StartMovingDown();
    }

    public void PlateReleased(PressurePlate plate)
    {
        StopMoving();
    }

    private void FixedUpdate()
    {
        if (!_shouldMove) return;

        Vector2 step = moveDirection.normalized * moveSpeed * Time.fixedDeltaTime;
        if (_rb != null && _rb.bodyType == RigidbodyType2D.Kinematic)
            _rb.MovePosition(_rb.position + step);
        else
            transform.position += (Vector3)step;
    }
}
