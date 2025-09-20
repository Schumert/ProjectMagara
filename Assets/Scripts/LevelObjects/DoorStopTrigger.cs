using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorStopTrigger : MonoBehaviour
{
    [Tooltip("Bu trigger'a ulaşınca duracak kapı.")]
    public DoorController doorToStop;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (doorToStop == null) return;

        // Kapının kendisi veya child objeleri bu trigger'a değdi mi?
        if (other.GetComponent<DoorController>() == doorToStop ||
            other.transform == doorToStop.transform ||
            other.transform.IsChildOf(doorToStop.transform))
        {
            doorToStop.ReachedStopPoint();
        }
    }
}
