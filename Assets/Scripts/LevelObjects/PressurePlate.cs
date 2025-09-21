using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask affectLayers;

    [Header("Targets")]
    [Tooltip("Bu plate basıldığında tetiklenecek objeler (Door, TurningPlatform, vb.). " +
             "IPlateReactive uygulayan tüm component'ler otomatik toplanır.")]
    public MonoBehaviour[] targetObjects;

    [Header("Visual (opsiyonel)")]
    public Transform plateVisual;
    public float pressDepth = 0.02f;
    public float pressLerp = 10f;

    private int _objectsOnPlate = 0;
    private Vector3 _plateDefaultPos;

    // Cache edilen alıcılar
    private IPlateReactive[] _reactives;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (plateVisual != null)
            _plateDefaultPos = plateVisual.localPosition;

        if (targetObjects != null && targetObjects.Length > 0)
        {
            _reactives = targetObjects
                .Where(t => t != null)
                .SelectMany(t => t.GetComponents<IPlateReactive>())
                .Distinct()
                .ToArray();
        }
        else
        {
            _reactives = new IPlateReactive[0];
        }
    }

    // class alanı
    private readonly HashSet<GameObject> _pressingObjects = new HashSet<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer)) return;
        if (_pressingObjects.Add(other.gameObject) && _pressingObjects.Count == 1)
        {
            foreach (var r in _reactives) r.PlatePressed(this);
            AudioManager.I.PlaySFX("click");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer)) return;
        if (_pressingObjects.Remove(other.gameObject) && _pressingObjects.Count == 0)
        {
            foreach (var r in _reactives) r.PlateReleased(this);
            AudioManager.I.PlaySFX("click_up");
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
