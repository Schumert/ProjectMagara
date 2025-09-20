using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public Vector2 moveDirection = Vector2.down; // sadece x,y
    private bool _shouldMove;

    public void StartMovingDown() => _shouldMove = true;
    public void StopMoving() => _shouldMove = false;

    private void Update()
    {
        if (_shouldMove)
            transform.position += (Vector3)(moveDirection.normalized * moveSpeed * Time.deltaTime);
    }

    public void ReachedStopPoint()
    {
        _shouldMove = false;
    }
}
