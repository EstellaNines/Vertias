using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickUPTextNotice : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Image backgroundImage;     // 背景图像组件
    [SerializeField] private Image itemIcon;           // 物品图标组件
    [SerializeField] private TextMeshProUGUI itemNameText; // 物品名称文本组件
    
    [Header("显示设置")]
    [SerializeField] private float displayDuration = 2f;   // 显示持续时间
    [SerializeField] private float fadeOutDuration = 1f;   // 淡出持续时间
    
    [Header("玩家引用")]
    [SerializeField] private Player player;             // 玩家引用
    
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private ItemBase lastPickedItem;
    
    private void Awake()
    {
        // 获取或添加CanvasGroup组件用于淡入淡出效果
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初始化时隐藏UI
        canvasGroup.alpha = 0f;
        
        // 如果没有指定玩家，尝试自动查找
        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
    }
    
    private void Start()
    {
        // 验证必要的组件
        ValidateComponents();
    }
    
    // 添加静态事件，用于即时通知拾取
    public static System.Action<ItemBase> OnItemPickedUp;
    
    private void OnEnable()
    {
        // 订阅拾取事件
        OnItemPickedUp += ShowPickupInfo;
        Debug.Log("PickUPTextNotice: 事件订阅成功");
    }
    
    private void OnDisable()
    {
        // 取消订阅拾取事件
        OnItemPickedUp -= ShowPickupInfo;
        Debug.Log("PickUPTextNotice: 事件取消订阅");
        
        // 清理协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }
    
    /// <summary>
    /// 验证必要的UI组件是否已分配
    /// </summary>
    private void ValidateComponents()
    {
        if (backgroundImage == null)
            Debug.LogWarning("PickUPTextNotice: 背景图像组件未分配！");
            
        if (itemIcon == null)
            Debug.LogWarning("PickUPTextNotice: 物品图标组件未分配！");
            
        if (itemNameText == null)
            Debug.LogWarning("PickUPTextNotice: 物品名称文本组件未分配！");
            
        if (player == null)
            Debug.LogError("PickUPTextNotice: 玩家引用未找到！");
    }
    
    /// <summary>
    /// 检查玩家是否拾取了新物品
    /// </summary>
    private void CheckForPickedItem()
    {
        if (player == null) return;
        
        // 检查玩家当前拾取的物品是否发生变化
        if (player.currentPickedItem != lastPickedItem)
        {
            lastPickedItem = player.currentPickedItem;
            
            // 如果拾取了新物品，显示信息
            if (lastPickedItem != null)
            {
                ShowPickupInfo(lastPickedItem);
            }
        }
    }
    
    /// <summary>
    /// 显示拾取物品信息
    /// </summary>
    /// <param name="item">拾取的物品</param>
    public void ShowPickupInfo(ItemBase item)
    {
        Debug.Log($"PickUPTextNotice: 收到拾取事件，物品: {(item != null ? item.name : "null")}");
        
        if (item == null) 
        {
            Debug.LogWarning("PickUPTextNotice: 物品为空，无法显示");
            return;
        }
        
        // 检查组件是否存在
        if (canvasGroup == null)
        {
            Debug.LogError("PickUPTextNotice: CanvasGroup组件缺失");
            return;
        }
        
        // 停止之前的淡出协程
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // 更新UI内容
        UpdateUIContent(item);
        
        // 显示UI
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        
        Debug.Log($"PickUPTextNotice: UI已激活，透明度设置为1");
        
        // 开始淡出计时
        fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
        
        Debug.Log($"显示拾取信息: {item.name}");
    }
    
    /// <summary>
    /// 更新UI内容
    /// </summary>
    /// <param name="item">物品对象</param>
    private void UpdateUIContent(ItemBase item)
    {
        Debug.Log($"PickUPTextNotice: 开始更新UI内容，物品: {item.name}");
        
        // 更新物品名称
        if (itemNameText != null)
        {
            string displayName = GetItemDisplayName(item);
            itemNameText.text = displayName;
            Debug.Log($"PickUPTextNotice: 设置物品名称: {displayName}");
        }
        else
        {
            Debug.LogWarning("PickUPTextNotice: itemNameText组件未分配");
        }
        
        // 更新物品图标
        if (itemIcon != null)
        {
            Sprite itemSprite = GetItemSprite(item);
            if (itemSprite != null)
            {
                itemIcon.sprite = itemSprite;
                itemIcon.enabled = true;
                Debug.Log($"PickUPTextNotice: 设置物品图标: {itemSprite.name}");
            }
            else
            {
                itemIcon.enabled = false;
                Debug.LogWarning($"物品 {item.name} 没有找到精灵图片");
            }
        }
        else
        {
            Debug.LogWarning("PickUPTextNotice: itemIcon组件未分配");
        }
    }
    
    /// <summary>
    /// 获取物品显示名称
    /// </summary>
    /// <param name="item">物品对象</param>
    /// <returns>显示名称</returns>
    private string GetItemDisplayName(ItemBase item)
    {
        // 优先使用物品的显示名称，如果没有则使用GameObject名称
        string displayName = item.name;
        
        // 移除GameObject名称中的"(Clone)"后缀
        if (displayName.Contains("(Clone)"))
        {
            displayName = displayName.Replace("(Clone)", "").Trim();
        }
        
        return displayName;
    }
    
    /// <summary>
    /// 获取物品的精灵图片
    /// </summary>
    /// <param name="item">物品对象</param>
    /// <returns>物品精灵图片</returns>
    private Sprite GetItemSprite(ItemBase item)
    {
        // 尝试从SpriteRenderer获取精灵图片
        SpriteRenderer spriteRenderer = item.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }
        
        // 尝试从子对象中查找SpriteRenderer
        SpriteRenderer childSpriteRenderer = item.GetComponentInChildren<SpriteRenderer>();
        if (childSpriteRenderer != null && childSpriteRenderer.sprite != null)
        {
            return childSpriteRenderer.sprite;
        }
        
        // 如果是武器，尝试从WeaponManager获取精灵
        if (item.CompareTag("Weapon"))
        {
            WeaponManager weaponManager = item.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                // 这里可以根据WeaponManager的具体实现来获取武器图标
                // 暂时使用SpriteRenderer作为备选方案
                return spriteRenderer?.sprite;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 延迟后淡出的协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeOutAfterDelay()
    {
        // 等待显示时间
        yield return new WaitForSeconds(displayDuration);
        
        // 开始淡出
        yield return StartCoroutine(FadeOut());

        // 淡出完成后隐藏GameObject
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 淡出效果协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator FadeOut()
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// 立即隐藏UI
    /// </summary>
    public void HideImmediately()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 手动显示指定物品信息（可用于测试）
    /// </summary>
    /// <param name="item">要显示的物品</param>
    public void ManualShowPickupInfo(ItemBase item)
    {
        ShowPickupInfo(item);
    }
    
    // 可以保留Update方法作为备用检测机制
    private void Update()
    {
        // 保留原有的轮询检测作为备用
        CheckForPickedItem();
    }
}
