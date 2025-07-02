using UnityEngine;

public class PlayerPickItems : MonoBehaviour
{
    public Transform handTransform; // 手部位置的Transform
    private ItemBase currentPickedItem; // 当前已拾取的物品
    private ItemBase nearbyItem; // 附近可拾取的物品

    private void Update()
    {
        // 检测F键输入
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (nearbyItem != null)
            {
                if (currentPickedItem != null)
                {
                    DropCurrentItem();
                }
                PickUpItem(nearbyItem);
            }
        }
    }

    // 注册可拾取物品
    public void RegisterItem(ItemBase item)
    {
        nearbyItem = item;
    }

    // 取消注册离开触发器的物品
    public void UnregisterItem(ItemBase item)
    {
        if (nearbyItem == item)
        {
            nearbyItem = null;
        }
    }

    // 拾取物品方法
    private void PickUpItem(ItemBase item)
    {
        currentPickedItem = item;
        item.transform.SetParent(handTransform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.Euler(Vector3.zero);

        // 禁用碰撞器和刚体防止物理干扰
        Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider2D collider = item.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    // 丢弃当前物品方法
    private void DropCurrentItem()
    {
        Transform itemTransform = currentPickedItem.transform;
        itemTransform.SetParent(null);
        itemTransform.rotation = Quaternion.Euler(Vector3.zero);
        itemTransform.localScale = Vector3.one;

        Rigidbody2D rb = itemTransform.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = itemTransform.gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Collider2D collider = itemTransform.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        currentPickedItem = null;
    }
}