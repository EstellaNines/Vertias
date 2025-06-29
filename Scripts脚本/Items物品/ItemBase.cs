using UnityEngine;

public class ItemBase : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.RegisterItem(this);
                Debug.Log($"可以拾取: {gameObject.name}");
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.UnregisterItem(this);
                Debug.Log($"离开拾取范围: {gameObject.name}");
            }
        }
    }
}