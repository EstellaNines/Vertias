using UnityEngine;

public class ItemBase : MonoBehaviour
{
    private Collider2D itemCollider;
    private void Awake()
    {
        itemCollider = GetComponent<Collider2D>();
        // 碰撞体保险机制
        if (itemCollider == null)
        {
            Debug.LogError($"物品 {name} 缺少 Collider2D 组件！");
            enabled = false;
            return;
        }

        // 确保碰撞体为触发器
        if (!itemCollider.isTrigger)
        {
            Debug.LogWarning($"物品 {name} 的碰撞体未设置为触发器，自动修正");
            itemCollider.isTrigger = true;
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查碰撞对象是否是玩家
        if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerPickItems pickScript = other.GetComponent<PlayerPickItems>();
            if (pickScript != null)
            {
                pickScript.UnregisterItem(this);
            }
        }
    }

    protected virtual void HandlePlayerCollision(Collider2D playerCollider)
    {
        PlayerPickItems pickScript = playerCollider.GetComponent<PlayerPickItems>();
        if (pickScript != null)
        {
            pickScript.RegisterItem(this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()// 可视化触发范围
    {
        Gizmos.color = Color.yellow;
        if (itemCollider is BoxCollider2D box)
        {
            Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
        }
    }
#endif
}