using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 世界空间延迟UI设置辅助器
/// 用于在编辑器中快速创建和配置世界空间延迟UI
/// </summary>
public class WorldSpaceDelayUISetup : MonoBehaviour
{
    [Header("自动创建设置")]
    [SerializeField][FieldLabel("自动创建世界空间Canvas")] private bool autoCreateCanvas = true;
    [SerializeField][FieldLabel("自动创建延迟UI预制体")] private bool autoCreateDelayUIPrefab = true;
    
    [Header("Canvas配置")]
    [SerializeField][FieldLabel("Canvas大小")] private Vector2 canvasSize = new Vector2(1000, 1000);
    [SerializeField][FieldLabel("Canvas排序层")] private int sortingOrder = 100;
    [SerializeField][FieldLabel("Canvas缩放因子")] private float canvasScale = 0.01f;
    
    [Header("延迟UI配置")]
    [SerializeField][FieldLabel("UI大小")] private Vector2 uiSize = new Vector2(300, 300);
    [SerializeField][FieldLabel("圆环半径")] private float circleRadius = 80f;
    [SerializeField][FieldLabel("UI背景色")] private Color backgroundColor = new Color(0, 0, 0, 0.5f);
    
    /// <summary>
    /// 创建世界空间延迟UI系统
    /// </summary>
    [ContextMenu("创建世界空间延迟UI系统")]
    public void CreateWorldSpaceDelayUISystem()
    {
        if (autoCreateCanvas)
        {
            CreateWorldSpaceCanvas();
        }
        
        if (autoCreateDelayUIPrefab)
        {
            CreateDelayUIPrefab();
        }
        
        // 创建管理器
        CreateDelayUIManager();
        
        Debug.Log("<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 世界空间延迟UI系统创建完成！");
    }
    
    /// <summary>
    /// 创建世界空间Canvas
    /// </summary>
    private GameObject CreateWorldSpaceCanvas()
    {
        GameObject canvasGO = new GameObject("WorldSpaceDelayUICanvas");
        
        // 设置Canvas组件
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = sortingOrder;
        
        // 设置CanvasScaler - 对于世界空间，使用Constant Pixel Size模式
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 100f;
        
        // 添加GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // 设置RectTransform
        RectTransform rectTransform = canvasGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = canvasSize; // 像素单位，但会被缩放
        rectTransform.localScale = Vector3.one * canvasScale; // 通过缩放控制实际大小
        
        Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 创建世界空间Canvas: {canvasGO.name}");
        Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> Canvas大小: {canvasSize}, 缩放: {canvasScale}");
        return canvasGO;
    }
    
    /// <summary>
    /// 创建延迟UI预制体
    /// </summary>
    private GameObject CreateDelayUIPrefab()
    {
        // 创建根对象
        GameObject delayUIGO = new GameObject("DelayMagnifierUI");
        
        // 添加RectTransform
        RectTransform rootRect = delayUIGO.AddComponent<RectTransform>();
        rootRect.sizeDelta = uiSize;
        
        // 添加CanvasGroup用于淡入淡出
        CanvasGroup canvasGroup = delayUIGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // 创建背景
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(delayUIGO.transform, false);
        
        RectTransform bgRect = backgroundGO.AddComponent<RectTransform>();
        bgRect.sizeDelta = uiSize;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = backgroundGO.AddComponent<Image>();
        bgImage.color = backgroundColor;
        bgImage.raycastTarget = false;
        
        // 创建圆环中心点
        GameObject centerGO = new GameObject("CircleCenter");
        centerGO.transform.SetParent(delayUIGO.transform, false);
        
        RectTransform centerRect = centerGO.AddComponent<RectTransform>();
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = Vector2.zero;
        
        // 创建放大镜图标
        GameObject magnifierGO = new GameObject("MagnifierIcon");
        magnifierGO.transform.SetParent(delayUIGO.transform, false);
        
        RectTransform magnifierRect = magnifierGO.AddComponent<RectTransform>();
        magnifierRect.sizeDelta = new Vector2(40, 40);
        magnifierRect.anchoredPosition = new Vector2(circleRadius, 0); // 起始位置在右侧
        
        Image magnifierImage = magnifierGO.AddComponent<Image>();
        magnifierImage.color = Color.white;
        magnifierImage.raycastTarget = false;
        
        // 尝试加载放大镜图标
        Sprite magnifierSprite = Resources.Load<Sprite>("UI/MagnifierIcon");
        if (magnifierSprite != null)
        {
            magnifierImage.sprite = magnifierSprite;
        }
        else
        {
            // 如果没有图标，创建一个简单的圆形
            magnifierImage.color = new Color(1, 1, 1, 0.8f);
            Debug.LogWarning("未找到放大镜图标，使用默认白色圆形");
        }
        
        // 添加圆环动画组件
        MagnifierCircleAnimation circleAnimation = delayUIGO.AddComponent<MagnifierCircleAnimation>();
        
        // 通过反射设置私有字段（因为它们是SerializeField）
        var magnifierIconField = typeof(MagnifierCircleAnimation).GetField("magnifierIcon", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var circleCenterField = typeof(MagnifierCircleAnimation).GetField("circleCenter", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var circleRadiusField = typeof(MagnifierCircleAnimation).GetField("circleRadius", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (magnifierIconField != null) magnifierIconField.SetValue(circleAnimation, magnifierRect);
        if (circleCenterField != null) circleCenterField.SetValue(circleAnimation, centerRect);
        if (circleRadiusField != null) circleRadiusField.SetValue(circleAnimation, circleRadius);
        
        // 添加延迟UI控制器
        DelayMagnifierUIController delayController = delayUIGO.AddComponent<DelayMagnifierUIController>();
        
        // 通过反射设置私有字段
        var magnifierAnimationField = typeof(DelayMagnifierUIController).GetField("magnifierAnimation", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rootObjectField = typeof(DelayMagnifierUIController).GetField("rootObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var canvasGroupField = typeof(DelayMagnifierUIController).GetField("canvasGroup", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (magnifierAnimationField != null) magnifierAnimationField.SetValue(delayController, circleAnimation);
        if (rootObjectField != null) rootObjectField.SetValue(delayController, delayUIGO);
        if (canvasGroupField != null) canvasGroupField.SetValue(delayController, canvasGroup);
        
        Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 创建延迟UI预制体: {delayUIGO.name}");
        return delayUIGO;
    }
    
    /// <summary>
    /// 创建延迟UI管理器
    /// </summary>
    private void CreateDelayUIManager()
    {
        GameObject managerGO = new GameObject("WorldSpaceDelayUIManager");
        WorldSpaceDelayUIManager manager = managerGO.AddComponent<WorldSpaceDelayUIManager>();
        
        Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 创建延迟UI管理器: {managerGO.name}");
        Debug.Log($"<color=#FF9800>[WorldSpaceDelayUISetup]</color> 请在Inspector中配置世界空间Canvas和延迟UI预制体引用！");
    }
    
    /// <summary>
    /// 为现有货架添加世界空间延迟UI支持
    /// </summary>
    [ContextMenu("为现有货架添加世界空间延迟UI支持")]
    public void AddWorldSpaceDelayUIToExistingShelves()
    {
        ShelfTrigger[] shelves = FindObjectsOfType<ShelfTrigger>();
        
        int updatedCount = 0;
        foreach (ShelfTrigger shelf in shelves)
        {
            // 通过反射设置useWorldSpaceDelayUI字段
            var useWorldSpaceField = typeof(ShelfTrigger).GetField("useWorldSpaceDelayUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (useWorldSpaceField != null)
            {
                useWorldSpaceField.SetValue(shelf, true);
                updatedCount++;
            }
        }
        
        Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 已为 {updatedCount} 个货架启用世界空间延迟UI");
    }
    
    /// <summary>
    /// 验证世界空间延迟UI系统设置
    /// </summary>
    [ContextMenu("验证世界空间延迟UI系统")]
    public void ValidateWorldSpaceDelayUISystem()
    {
        Debug.Log("<color=#2196F3>[WorldSpaceDelayUISetup]</color> === 世界空间延迟UI系统验证 ===");
        
        // 检查管理器
        WorldSpaceDelayUIManager manager = FindObjectOfType<WorldSpaceDelayUIManager>();
        if (manager != null)
        {
            Debug.Log("<color=#4CAF50>[WorldSpaceDelayUISetup]</color> ?7?7 找到WorldSpaceDelayUIManager");
            manager.GetManagerStatus();
        }
        else
        {
            Debug.LogWarning("<color=#FF9800>[WorldSpaceDelayUISetup]</color> ?7?1 未找到WorldSpaceDelayUIManager");
        }
        
        // 检查世界空间Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas worldSpaceCanvas = null;
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                worldSpaceCanvas = canvas;
                break;
            }
        }
        
        if (worldSpaceCanvas != null)
        {
            Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> ?7?7 找到世界空间Canvas: {worldSpaceCanvas.name}");
        }
        else
        {
            Debug.LogWarning("<color=#FF9800>[WorldSpaceDelayUISetup]</color> ?7?1 未找到世界空间Canvas");
        }
        
        // 检查货架设置
        ShelfTrigger[] shelves = FindObjectsOfType<ShelfTrigger>();
        int worldSpaceEnabledCount = 0;
        
        foreach (ShelfTrigger shelf in shelves)
        {
            var useWorldSpaceField = typeof(ShelfTrigger).GetField("useWorldSpaceDelayUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (useWorldSpaceField != null)
            {
                bool useWorldSpace = (bool)useWorldSpaceField.GetValue(shelf);
                if (useWorldSpace) worldSpaceEnabledCount++;
            }
        }
        
        Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 货架总数: {shelves.Length}, 启用世界空间UI: {worldSpaceEnabledCount}");
        
        if (shelves.Length > 0 && worldSpaceEnabledCount == shelves.Length)
        {
            Debug.Log("<color=#4CAF50>[WorldSpaceDelayUISetup]</color> ?7?7 所有货架都已启用世界空间延迟UI");
        }
        else if (worldSpaceEnabledCount > 0)
        {
            Debug.LogWarning($"<color=#FF9800>[WorldSpaceDelayUISetup]</color> ?7?2 只有 {worldSpaceEnabledCount}/{shelves.Length} 个货架启用了世界空间延迟UI");
        }
        else
        {
            Debug.LogWarning("<color=#FF9800>[WorldSpaceDelayUISetup]</color> ?7?1 没有货架启用世界空间延迟UI");
        }
        
        Debug.Log("<color=#2196F3>[WorldSpaceDelayUISetup]</color> === 验证完成 ===");
    }
    
    /// <summary>
    /// 更新现有世界空间Canvas设置以修复定位问题
    /// </summary>
    [ContextMenu("修复现有世界空间Canvas设置")]
    public void FixExistingWorldSpaceCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas worldSpaceCanvas = null;
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.name.Contains("WorldSpace"))
            {
                worldSpaceCanvas = canvas;
                break;
            }
        }
        
        if (worldSpaceCanvas != null)
        {
            Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 找到现有世界空间Canvas: {worldSpaceCanvas.name}");
            
            // 更新CanvasScaler设置
            CanvasScaler scaler = worldSpaceCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                scaler.referencePixelsPerUnit = 100f;
                Debug.Log("<color=#4CAF50>[WorldSpaceDelayUISetup]</color> ? 更新CanvasScaler设置");
            }
            
            // 更新Canvas大小和缩放
            RectTransform rectTransform = worldSpaceCanvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = canvasSize;
                rectTransform.localScale = Vector3.one * canvasScale;
                Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> ? 更新Canvas大小为: {canvasSize}, 缩放为: {canvasScale}");
            }
            
            // 更新排序层
            worldSpaceCanvas.sortingOrder = sortingOrder;
            Debug.Log($"<color=#4CAF50>[WorldSpaceDelayUISetup]</color> ? 更新排序层为: {sortingOrder}");
            
            Debug.Log("<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 世界空间Canvas设置修复完成！");
        }
        else
        {
            Debug.LogWarning("<color=#FF9800>[WorldSpaceDelayUISetup]</color> 未找到现有的世界空间Canvas");
        }
    }
    
    /// <summary>
    /// 测试边界位置UI显示
    /// </summary>
    [ContextMenu("测试边界位置UI显示")]
    public void TestBoundaryPositionUI()
    {
        WorldSpaceDelayUIManager manager = FindObjectOfType<WorldSpaceDelayUIManager>();
        if (manager != null)
        {
            Debug.Log("<color=#4CAF50>[WorldSpaceDelayUISetup]</color> 开始边界位置UI测试");
            manager.TestUIPositionAccuracy();
        }
        else
        {
            Debug.LogWarning("<color=#FF9800>[WorldSpaceDelayUISetup]</color> 未找到WorldSpaceDelayUIManager，无法进行测试");
        }
    }
}
