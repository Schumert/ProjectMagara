using System.Collections.Generic;
using UnityEngine;

public class GhostDestroySkill : MonoBehaviour
{
    [Tooltip("Ghost'un çarpıştığı Destroyable objeler tutulur.")]
    private readonly List<GameObject> _collidingDestroyables = new List<GameObject>();
    public GameObject destroyEffectPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Destroyable"))
        {
            if (!_collidingDestroyables.Contains(other.gameObject))
                _collidingDestroyables.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Destroyable"))
        {
            _collidingDestroyables.Remove(other.gameObject);
        }
    }

    /// <summary>
    /// Bu metodu başka bir yerden çağıracaksın.
    /// Ghost çarpışma alanındaki Destroyable objeleri yok eder.
    /// </summary>
    public void OnEPressed()
    {
        if (_collidingDestroyables.Count > 0)
        {
            // İlk bulduğunu yok et
            GameObject target = _collidingDestroyables[0];
            _collidingDestroyables.RemoveAt(0);


            if (destroyEffectPrefab != null)
            {
                var effect = Instantiate(destroyEffectPrefab, target.transform.position, Quaternion.identity);
                Destroy(effect, 2f); // 2 saniye sonra efekt sahneden silinsin
            }

            Destroy(target);
            Debug.Log($"Destroyable obje yok edildi: {target.name}");
        }
        else
        {
            Debug.Log("Çarpışılan Destroyable obje yok.");
        }
    }
}
