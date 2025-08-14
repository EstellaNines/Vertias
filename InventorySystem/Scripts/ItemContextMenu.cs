using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemContextMenu : MonoBehaviour
{
    [Header("菜单UI组件")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button viewButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private TextMeshProUGUI useButtonText;

    [Header("菜单设置")]
    [SerializeField] private float menuOffset = 10f;
    [SerializeField] private Canvas parentCanvas;

    private InventorySystemItem currentItem;
    private RectTransform menuRectTransform;

    // 菜单事件
    public System.Action<InventorySystemItem> OnViewItem;
    public System.Action<InventorySystemItem> OnUseItem;
    public System.Action<InventorySystemItem> OnDiscardItem;

    private void Awake()
    {
        // 检查所有必要的组件引用
        if (menuPanel == null)
        {
            Debug.LogError("ItemContextMenu: menuPanel未赋值！请在检查器中设置menuPanel引用。", this);
            return;
        }

        Debug.Log($"ItemContextMenu: menuPanel引用正常，名称: {menuPanel.name}, 激活状态: {menuPanel.activeInHierarchy}");

        if (viewButton == null)
        {
            Debug.LogError("ItemContextMenu: viewButton未赋值！请在检查器中设置viewButton引用。", this);
            return;
        }

        if (useButton == null)
        {
            Debug.LogError("ItemContextMenu: useButton未赋值！请在检查器中设置useButton引用。", this);
            return;
        }

        if (discardButton == null)
        {
            Debug.LogError("ItemContextMenu: discardButton未赋值！请在检查器中设置discardButton引用。", this);
            return;
        }

        if (useButtonText == null)
        {
            Debug.LogError("ItemContextMenu: useButtonText未赋值！请在检查器中设置useButtonText引用。", this);
            return;
        }

        // 尝试获取RectTransform组件
        menuRectTransform = menuPanel.GetComponent<RectTransform>();
        if (menuRectTransform == null)
        {
            Debug.LogError($"ItemContextMenu: menuPanel({menuPanel.name})上没有找到RectTransform组件！请检查menuPanel是否为UI对象。", this);
            return;
        }

        Debug.Log($"ItemContextMenu: menuRectTransform获取成功，大小: {menuRectTransform.sizeDelta}");

        // 智能查找Canvas的方法
        FindParentCanvas();

        // 绑定按钮事件
        viewButton.onClick.AddListener(() => ViewItem());
        useButton.onClick.AddListener(() => UseItem());
        discardButton.onClick.AddListener(() => DiscardItem());

        // 初始隐藏菜单
        HideMenu();

        Debug.Log("ItemContextMenu: 初始化完成");
    }

    /// <summary>
    /// 智能查找父级Canvas
    /// </summary>
    private void FindParentCanvas()
    {
        // 如果已经手动赋值，直接使用
        if (parentCanvas != null)
        {
            Debug.Log($"ItemContextMenu: 使用手动赋值的Canvas: {parentCanvas.name}");
            return;
        }

        // 方法1: 从当前对象向上查找Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Debug.Log($"ItemContextMenu: 通过GetComponentInParent找到Canvas: {parentCanvas.name}");
            return;
        }

        // 方法2: 查找名为"BackpackCanvas"的Canvas
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.name == "BackpackCanvas")
            {
                parentCanvas = canvas;
                Debug.Log($"ItemContextMenu: 通过名称找到BackpackCanvas: {parentCanvas.name}");
                return;
            }
        }

        // 方法3: 查找渲染模式为Screen Space - Overlay的Canvas
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                parentCanvas = canvas;
                Debug.Log($"ItemContextMenu: 找到Screen Space Overlay Canvas: {parentCanvas.name}");
                return;
            }
        }

        // 方法4: 使用第一个找到的Canvas
        if (allCanvases.Length > 0)
        {
            parentCanvas = allCanvases[0];
            Debug.Log($"ItemContextMenu: 使用第一个找到的Canvas: {parentCanvas.name}");
            return;
        }

        // 如果所有方法都失败了
        Debug.LogError("ItemContextMenu: 无法找到任何Canvas！请检查场景设置。");
    }

    private void Update()
    {
        // 点击其他地方时隐藏菜单
        if (menuPanel.activeInHierarchy && Input.GetMouseButtonDown(0))
        {
            if (parentCanvas != null && !RectTransformUtility.RectangleContainsScreenPoint(menuRectTransform, Input.mousePosition, parentCanvas.worldCamera))
            {
                HideMenu();
            }
        }
    }

    /// <summary>
    /// 显示右键菜单
    /// </summary>
    /// <param name="item">要操作的物品</param>
    /// <param name="screenPosition">菜单显示位置</param>
    public void ShowMenu(InventorySystemItem item, Vector2 screenPosition)
    {
        if (item == null) return;

        currentItem = item;

        // 根据物品类型设置按钮文本
        UpdateButtonText(item);

        // 设置菜单位置
        SetMenuPosition(screenPosition);

        // 显示菜单
        menuPanel.SetActive(true);

        Debug.Log($"显示物品右键菜单: {item.Data.itemName}");
    }

    /// <summary>
    /// 隐藏右键菜单
    /// </summary>
    public void HideMenu()
    {
        menuPanel.SetActive(false);
        currentItem = null;
    }

    /// <summary>
    /// 根据物品类型更新按钮文本
    /// </summary>
    /// <param name="item">物品对象</param>
    private void UpdateButtonText(InventorySystemItem item)
    {
        if (item == null || item.Data == null) return;

        // 根据物品类别决定第二个按钮的文本
        if (item.Data.itemCategory == InventorySystemItemCategory.Backpack ||
            item.Data.itemCategory == InventorySystemItemCategory.TacticalRig)
        {
            useButtonText.text = "Open";
        }
        else
        {
            useButtonText.text = "Use";
        }
    }

    /// <summary>
    /// 设置菜单位置
    /// </summary>
    /// <param name="screenPosition">屏幕位置</param>
    private void SetMenuPosition(Vector2 screenPosition)
    {
        // 检查menuPanel是否仍然存在
        if (menuPanel == null)
        {
            Debug.LogError("ItemContextMenu: menuPanel为空！可能在运行时被销毁了。", this);
            return;
        }

        // 检查menuRectTransform
        if (menuRectTransform == null)
        {
            Debug.LogError($"ItemContextMenu: menuRectTransform为空！menuPanel状态: 存在={menuPanel != null}, 激活={menuPanel.activeInHierarchy}, 名称={menuPanel.name}", this);

            // 尝试重新获取RectTransform
            menuRectTransform = menuPanel.GetComponent<RectTransform>();
            if (menuRectTransform == null)
            {
                Debug.LogError("ItemContextMenu: 重新获取menuRectTransform失败！menuPanel可能不是UI对象。", this);
                return;
            }
            else
            {
                Debug.Log("ItemContextMenu: 重新获取menuRectTransform成功！");
            }
        }

        if (parentCanvas == null)
        {
            Debug.LogError("ItemContextMenu: parentCanvas为空，尝试重新查找...");
            FindParentCanvas();

            if (parentCanvas == null)
            {
                Debug.LogError("ItemContextMenu: 仍然无法找到Canvas，无法设置菜单位置！", this);
                return;
            }
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out localPoint);

        // 添加偏移，避免菜单被鼠标遮挡
        localPoint += new Vector2(menuOffset, -menuOffset);

        // 确保菜单不会超出屏幕边界
        Vector2 canvasSize = (parentCanvas.transform as RectTransform).sizeDelta;
        Vector2 menuSize = menuRectTransform.sizeDelta;

        // 右边界检查
        if (localPoint.x + menuSize.x > canvasSize.x / 2)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPosition,
                parentCanvas.worldCamera,
                out localPoint);
            localPoint.x -= (menuSize.x + menuOffset);
        }

        // 下边界检查
        if (localPoint.y - menuSize.y < -canvasSize.y / 2)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPosition,
                parentCanvas.worldCamera,
                out localPoint);
            localPoint.y += (menuSize.y + menuOffset);
        }

        menuRectTransform.localPosition = localPoint;

        Debug.Log($"ItemContextMenu: 菜单位置设置完成，本地坐标: {localPoint}");
    }

    /// <summary>
    /// 查看物品
    /// </summary>
    private void ViewItem()
    {
        if (currentItem != null)
        {
            OnViewItem?.Invoke(currentItem);
            Debug.Log($"查看物品: {currentItem.Data.itemName}");
        }
        HideMenu();
    }

    /// <summary>
    /// 使用物品
    /// </summary>
    private void UseItem()
    {
        if (currentItem != null)
        {
            OnUseItem?.Invoke(currentItem);
            Debug.Log($"使用物品: {currentItem.Data.itemName}");
        }
        HideMenu();
    }

    /// <summary>
    /// 丢弃物品
    /// </summary>
    private void DiscardItem()
    {
        if (currentItem != null)
        {
            OnDiscardItem?.Invoke(currentItem);
            Debug.Log($"丢弃物品: {currentItem.Data.itemName}");
        }
        HideMenu();
    }
}
