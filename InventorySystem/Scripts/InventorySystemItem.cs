using UnityEngine;

// 移除拖拽接口，改为简单的数据容器
public class InventorySystemItem : MonoBehaviour
{
    ItemDataHolder holder;
    private bool isDraggable = true;

    public Vector2Int Size => new Vector2Int(holder.ItemWidth, holder.ItemHeight);
    public InventorySystemItemDataSO Data => holder.GetItemData();
    public bool IsDraggable { get => isDraggable; set => isDraggable = value; }

    void Awake()
    {
        // 从子对象中查找 ItemDataHolder
        holder = GetComponentInChildren<ItemDataHolder>();
        if (holder == null)
        {
            Debug.LogError($"在 {gameObject.name} 的子对象中未找到 ItemDataHolder 组件！");
        }
    }
}