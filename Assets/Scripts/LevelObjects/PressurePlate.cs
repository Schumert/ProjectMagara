using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    public LayerMask affectLayers;
    public DoorController door;
    public Transform plateVisual;
    public float pressDepth = 0.02f;
    public float pressLerp = 10f;

    private int _objectsOnPlate = 0;
    private Vector3 _plateDefaultPos;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (plateVisual != null)
            _plateDefaultPos = plateVisual.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer))
        {
            _objectsOnPlate++;
            if (_objectsOnPlate == 1 && door != null)
                door.StartMovingDown();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer))
        {
            _objectsOnPlate = Mathf.Max(0, _objectsOnPlate - 1);
            if (_objectsOnPlate == 0 && door != null)
                door.StopMoving();
        }
    }

    private bool IsInLayerMask(int layer)
    {
        return (affectLayers.value & (1 << layer)) != 0;
    }

    private void Update()
    {
        if (plateVisual == null) return;

        var target = _objectsOnPlate > 0
            ? _plateDefaultPos + Vector3.down * pressDepth
            : _plateDefaultPos;

        plateVisual.localPosition = Vector3.Lerp(
            plateVisual.localPosition, target, Time.deltaTime * pressLerp);
    }
}
