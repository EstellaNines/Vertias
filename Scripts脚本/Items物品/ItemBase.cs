using UnityEngine;

public class ItemBase : MonoBehaviour
{
    [Header("物品设置")]
    [Tooltip("物品显示名称")]
    public string itemDisplayName;
    
    [Header("材质设置")]
    [Tooltip("默认精灵材质")]
    public Material defaultMaterial;
    
    [Tooltip("高亮精灵材质（玩家进入触发器时使用）")]
    public Material highlightMaterial;
    
    private SpriteRenderer spriteRenderer;
    
    private void Start()
    {
        // 获取精灵渲染器组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 如果没有设置显示名称，使用GameObject的名称
        if (string.IsNullOrEmpty(itemDisplayName))
        {
            itemDisplayName = gameObject.name;
        }
        
        // 如果没有设置默认材质，使用当前材质作为默认材质
        if (defaultMaterial == null && spriteRenderer != null)
        {
            defaultMaterial = spriteRenderer.material;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.RegisterItem(this);
                Debug.Log($"可以拾取: {gameObject.name}");
                
                // 显示拾取提示UI（不传递物品名称）
                PickUpButtonNotice.ShowPickUpNotice();
                
                // 切换到高亮材质
                SetHighlightMaterial();
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
                
                // 隐藏拾取提示UI
                PickUpButtonNotice.HidePickUpNotice();
                
                // 恢复默认材质
                SetDefaultMaterial();
            }
        }
    }
    
    // 当物品被拾取时调用（可以由Player脚本调用）
    public virtual void OnPickedUp()
    {
        // 隐藏拾取提示
        PickUpButtonNotice.HidePickUpNotice();
        
        // 销毁物品对象
        Destroy(gameObject);
    }
    
    // 设置高亮材质
    private void SetHighlightMaterial()
    {
        if (spriteRenderer != null && highlightMaterial != null)
        {
            spriteRenderer.material = highlightMaterial;
            Debug.Log($"物品 {gameObject.name} 切换到高亮材质");
        }
    }
    
    // 设置默认材质
    private void SetDefaultMaterial()
    {
        if (spriteRenderer != null && defaultMaterial != null)
        {
            spriteRenderer.material = defaultMaterial;
            Debug.Log($"物品 {gameObject.name} 恢复默认材质");
        }
    }
}