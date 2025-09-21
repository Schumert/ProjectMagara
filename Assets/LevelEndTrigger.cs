using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndTrigger : MonoBehaviour
{
    [Tooltip("Geçilecek sahnenin adı")]
    [SerializeField] private string nextSceneName = "LevelTest 2";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[LevelEndTrigger] Oyuncu tetikledi → {nextSceneName} sahnesi yükleniyor...");
            SceneManager.LoadScene("LevelTest2");
        }
    }
}
