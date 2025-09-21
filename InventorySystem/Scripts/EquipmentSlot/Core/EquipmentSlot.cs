using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using GlobalMessaging;
using System.Collections.Generic;

namespace InventorySystem
{
    /// <summary>
    /// 通用装备槽组件
    /// 支持所有类型的装备槽，通过配置文件驱动行为
    /// </summary>
    public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("配置")]
        [FieldLabel("装备槽配置")]
        [Tooltip("装备槽的配置资产，决定槽位行为")]
        public EquipmentSlotConfig config;

        [Header("UI组件引用")]
        [FieldLabel("槽位背景")]
        [Tooltip("槽位背景图片组件")]
        public Image equipmentSlotBackground;

        [FieldLabel("槽位标题")]
        [Tooltip("槽位标题文本组件")]
        public TextMeshProUGUI equipmentSlotTitle;

        [Header("武器弹药显示")]
        [FieldLabel("弹药文本（主/副武器槽使用）")]
        [Tooltip("用于显示当前武器弹药数量（当前/总数），非武器槽可留空")]
        public TextMeshProUGUI weaponAmmoText;

        [FieldLabel("物品显示区域")]
        [Tooltip("装备物品的显示区域，默认为根节点")]
        public Transform itemDisplayArea;

        [Header("容器功能")]
        [FieldLabel("容器显示父级")]
        [Tooltip("容器网格的显示父级")]
        public Transform containerParent;

        [Header("运行时状态")]
        [FieldLabel("当前装备物品")]
        [SerializeField] private ItemDataReader currentEquippedItem;

        [FieldLabel("当前物品实例")]
        [SerializeField] private GameObject currentItemInstance;

        [FieldLabel("是否已装备")]
        [SerializeField] private bool isItemEquipped = false;

        [FieldLabel("容器网格实例")]
        [SerializeField] private ItemGrid containerGrid;

        [FieldLabel("原始物品位置")]
        [SerializeField] private Vector2Int originalItemPosition;

        [FieldLabel("原始所在网格")]
        [SerializeField] private ItemGrid originalItemGrid;

        [FieldLabel("物品原始尺寸")]
        [SerializeField] private Vector2 originalItemSize;

        [FieldLabel("物品背景原始颜色")]
        [SerializeField] private Color originalBackgroundColor;

        // 装备槽事件
        public static event System.Action<EquipmentSlotType, ItemDataReader> OnItemEquipped;
        public static event System.Action<EquipmentSlotType, ItemDataReader> OnItemUnequipped;
        public static event System.Action<EquipmentSlotType, ItemGrid> OnContainerSlotActivated;
        public static event System.Action<EquipmentSlotType, ItemGrid> OnContainerSlotDeactivated;

        // 内部组件引用
        private InventoryController inventoryController;
        private Canvas canvas;
        private bool isDragHovering = false;

        // 🔧 容器内容加载标志
        private bool needsContainerContentLoad = false;
        // 当槽位在未激活状态被装备时，延迟在启用后校准尺寸
        private bool needsSizeEnsureOnEnable = false;

        // 武器弹药显示相关
        [SerializeField] private WeaponManager observedWeaponManager;
        private int? weaponSlotIndexCache; // 0=主武器, 1=副武器, 其他为null

        #region Unity生命周期

        private void Awake()
        {
            InitializeComponents();
            ValidateSetup();
        }

        private void Start()
        {
            InitializeSlot();
            FindSystemComponents();
        }

        private void OnEnable()
        {
            // 🔧 检查是否有装备但UI未显示的情况（跨会话恢复后的显示修复）
            if (currentEquippedItem != null && (currentItemInstance == null || !currentItemInstance.activeInHierarchy))
            {
                LogDebugInfo($"装备槽激活时发现已装备物品 {currentEquippedItem.ItemData.itemName} 但UI未显示，开始修复显示");

                // 查找场景中对应的装备实例
                var foundInstance = FindEquippedItemInstance();
                if (foundInstance != null)
                {
                    LogDebugInfo($"找到装备实例 {foundInstance.name}，重新建立连接并显示");

                    // 重新建立连接
                    currentItemInstance = foundInstance.gameObject;

                    // 确保装备显示在正确位置
                    if (currentItemInstance.transform.parent != itemDisplayArea)
                    {
                        currentItemInstance.transform.SetParent(itemDisplayArea, false);
                    }

                    // 确保激活状态
                    if (!currentItemInstance.activeInHierarchy)
                    {
                        currentItemInstance.SetActive(true);
                    }

                    // 触发UI更新
                    StartCoroutine(RefreshEquipmentDisplay());
                }
            }

            // 🔧 检查是否需要加载容器内容
            if (needsContainerContentLoad && containerGrid != null && currentEquippedItem != null)
            {
                needsContainerContentLoad = false; // 重置标志
                LogDebugInfo($"装备槽激活，开始加载容器内容");

                try
                {
                    StartCoroutine(DelayedLoadContainerContent());
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[EquipmentSlot] OnEnable中加载容器内容失败: {e.Message}");
                }
            }

            // 若之前因未激活而跳过了尺寸校准，这里补做一次
            if (needsSizeEnsureOnEnable && isItemEquipped && currentItemInstance != null)
            {
                needsSizeEnsureOnEnable = false;
                if (isActiveAndEnabled)
                {
                    StartCoroutine(EnsureItemSizeAfterFrame());
                }
            }

            // 订阅武器相关消息（仅主/副武器槽）
            if (IsWeaponSlot() && MessagingCenter.Instance != null)
            {
                MessagingCenter.Instance.Register<WeaponMessageBus.WeaponEquippedEvent>(OnWeaponEquipped);
                MessagingCenter.Instance.Register<WeaponMessageBus.WeaponUnequippedEvent>(OnWeaponUnequipped);
                // 重新绑定当前槽位的武器（若已装备），并立即刷新显示
                TryRebindWeaponOnEnable();
            }
        }

        private void OnValidate()
        {
            if (config != null)
            {
                // 自动设置标题
                if (equipmentSlotTitle != null)
                {
                    equipmentSlotTitle.text = config.slotName;
                }

                // 在编辑器中预览物品尺寸调整效果
#if UNITY_EDITOR
                if (Application.isPlaying && isItemEquipped && currentItemInstance != null)
                {
                    var rectTransform = currentItemInstance.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        AdjustItemSizeToFitSlot(rectTransform);
                    }
                }
#endif
            }
        }

        private void OnDisable()
        {
            // 取消消息订阅与事件绑定（仅主/副武器槽）
            if (IsWeaponSlot() && MessagingCenter.Instance != null)
            {
                MessagingCenter.Instance.Unregister<WeaponMessageBus.WeaponEquippedEvent>(OnWeaponEquipped);
                MessagingCenter.Instance.Unregister<WeaponMessageBus.WeaponUnequippedEvent>(OnWeaponUnequipped);
            }
            UnbindObservedWeaponEvents();

            // 不主动清空文本，避免频繁开关UI导致闪烁；仅在卸下或无装备时清空
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有指定物品显示区域，默认使用根节点
            if (itemDisplayArea == null)
                itemDisplayArea = transform;

            // 查找背景组件
            if (equipmentSlotBackground == null)
                equipmentSlotBackground = GetComponentInChildren<Image>();

            // 查找标题组件
            if (equipmentSlotTitle == null)
                equipmentSlotTitle = GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>
        /// 验证设置
        /// </summary>
        private void ValidateSetup()
        {
            if (config == null)
            {
                Debug.LogError($"[EquipmentSlot] {gameObject.name}: 未设置装备槽配置！", this);
                enabled = false;
                return;
            }

            var (isValid, errorMessage) = config.ValidateConfig();
            if (!isValid)
            {
                Debug.LogError($"[EquipmentSlot] {gameObject.name}: 配置验证失败 - {errorMessage}", this);
            }
        }

        /// <summary>
        /// 初始化槽位
        /// </summary>
        private void InitializeSlot()
        {
            if (config == null) return;

            // 设置槽位标题
            if (equipmentSlotTitle != null)
            {
                equipmentSlotTitle.text = config.slotName;
            }

            // 设置空槽状态
            UpdateSlotDisplay();

            Debug.Log($"[EquipmentSlot] 初始化装备槽: {config.slotName} ({config.slotType})");
        }

        /// <summary>
        /// 查找系统组件
        /// </summary>
        private void FindSystemComponents()
        {
            // 查找背包控制器
            inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController == null)
            {
                Debug.LogWarning($"[EquipmentSlot] 未找到InventoryController组件");
            }

            // 查找画布
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
        }

        #endregion


        #region 装备槽核心逻辑

        /// <summary>
        /// 检查物品是否可以装备到此槽位
        /// </summary>
        /// <param name="item">要检查的物品</param>
        /// <returns>是否可以装备</returns>
        public bool CanAcceptItem(ItemDataReader item)
        {
            if (item == null || config == null) return false;

            // 检查是否已有装备且不允许替换
            if (isItemEquipped && !config.allowEquipmentReplacement)
                return false;

            // 使用配置的兼容性检查
            if (!config.IsItemCompatible(item.ItemData))
                return false;

            // 检查旋转物品
            if (item.GetComponent<Item>()?.IsRotated() == true && !config.allowRotatedItems)
                return false;

            // 高级验证
            if (config.enableAdvancedValidation)
            {
                return AdvancedValidation(item);
            }

            return true;
        }

        /// <summary>
        /// 高级验证逻辑
        /// </summary>
        /// <param name="item">要验证的物品</param>
        /// <returns>验证结果</returns>
        private bool AdvancedValidation(ItemDataReader item)
        {
            // 这里可以添加更复杂的验证逻辑
            // 例如：等级限制、职业限制、前置装备要求等

            // 暂时返回true，后续可扩展
            return true;
        }

        /// <summary>
        /// 装备物品
        /// </summary>
        /// <param name="item">要装备的物品</param>
        /// <returns>是否装备成功</returns>
        public bool EquipItem(ItemDataReader item)
        {
            // 被排除的槽（如任务镜像槽）仅用于显示，不参与全局事件/持久化
            bool excluded = GetComponent<ExcludeFromEquipmentSystem>() != null;
            if (!CanAcceptItem(item))
            {
                Debug.LogWarning($"[EquipmentSlot] 物品 {item.ItemData.itemName} 无法装备到 {config.slotName}");
                return false;
            }

            // 如果已有装备，先卸下
            if (isItemEquipped && currentEquippedItem != null)
            {
                Debug.Log($"[EquipmentSlot] 装备槽已有装备 {currentEquippedItem.ItemData.itemName}，准备替换");
                var unequippedItem = UnequipItem();
                if (unequippedItem != null)
                {
                    // 尝试将卸下的物品放回背包
                    TryReturnItemToInventory(unequippedItem);
                }
            }

            // 记录物品原始位置信息，并从原网格中移除
            var itemComponent = item.GetComponent<Item>();
            if (itemComponent != null)
            {
                originalItemGrid = itemComponent.OnGridReference;
                originalItemPosition = itemComponent.OnGridPosition;

                // 记录物品原始尺寸（使用真实视觉尺寸）
                var itemRectTransform = item.GetComponent<RectTransform>();
                if (itemRectTransform != null)
                {
                    // 使用真实的视觉尺寸而不是网格计算的尺寸
                    originalItemSize = GetItemRealVisualSize(itemRectTransform);
                    Debug.Log($"[EquipmentSlot] 记录物品 {item.ItemData.itemName} 真实原始尺寸: {originalItemSize}");
                }

                // 确保从原网格中完全移除物品
                if (originalItemGrid != null && itemComponent.IsOnGrid())
                {
                    Debug.Log($"[EquipmentSlot] 从网格 {originalItemGrid.GridName} 位置 {originalItemPosition} 移除物品 {item.ItemData.itemName}");
                    originalItemGrid.PickUpItem(originalItemPosition.x, originalItemPosition.y);
                }
            }

            // 装备新物品
            currentEquippedItem = item;
            currentItemInstance = item.gameObject;

            // 清除物品的网格状态
            if (itemComponent != null)
            {
                itemComponent.ResetGridState();
            }

            // 保持拖拽功能启用，但标记为装备状态
            var draggableComponent = currentItemInstance.GetComponent<DraggableItem>();
            if (draggableComponent != null)
            {
                // 装备状态下仍然允许拖拽，让DraggableItem处理装备槽的逻辑
                draggableComponent.SetDragEnabled(true);
            }

            // 设置物品Transform以确保正确显示
            SetupEquippedItemTransform();

            isItemEquipped = true;
            UpdateSlotDisplay();

            // 若为武器槽，尝试绑定武器并更新弹药文本
            if (IsWeaponSlot())
            {
                BindWeaponIfPresent(currentItemInstance);
                UpdateWeaponAmmoText();
            }

            // 延迟一帧再次确保尺寸设置正确（防止其他系统覆盖）。若未激活则在OnEnable补做
            if (isActiveAndEnabled)
            {
                StartCoroutine(EnsureItemSizeAfterFrame());
            }
            else
            {
                needsSizeEnsureOnEnable = true;
            }

            // 触发装备事件（跳过被排除的槽）
            if (!excluded)
            {
                OnItemEquipped?.Invoke(config.slotType, currentEquippedItem);
            }

            // 如果是容器类装备，激活容器功能
            if (config.isContainerSlot && currentEquippedItem.ItemData.IsContainer())
            {
                ActivateContainerGrid();
            }

            Debug.Log($"[EquipmentSlot] 成功装备 {currentEquippedItem.ItemData.itemName} 到 {config.slotName}");
            return true;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        /// <returns>卸下的物品</returns>
        public ItemDataReader UnequipItem()
        {
            bool excluded = GetComponent<ExcludeFromEquipmentSystem>() != null;
            if (!isItemEquipped || currentEquippedItem == null) return null;

            var unequippedItem = currentEquippedItem;
            var unequippedItemInstance = currentItemInstance;

            // 如果是容器类装备，先处理容器（排除镜像槽）
            if (!excluded && config.isContainerSlot && containerGrid != null)
            {
                DeactivateContainerGrid();
            }

            // 只有当物品实例存在且有效时才进行组件操作
            if (unequippedItemInstance != null)
            {
                // 恢复拖拽功能
                var draggableComponent = unequippedItemInstance.GetComponent<DraggableItem>();
                if (draggableComponent != null)
                {
                    draggableComponent.SetDragEnabled(true);
                }

                // 恢复网格相关组件
                RestoreGridRelatedComponents(unequippedItemInstance);
            }
            else
            {
                Debug.LogWarning($"[EquipmentSlot] 卸下装备时，物品实例为null，可能已被销毁: {unequippedItem?.ItemData?.itemName}");
            }

            // 清理状态
            currentEquippedItem = null;
            currentItemInstance = null;
            isItemEquipped = false;

            UpdateSlotDisplay();

            // 若为武器槽，取消绑定并清空文本
            if (IsWeaponSlot())
            {
                UnbindObservedWeaponEvents();
                if (weaponAmmoText != null) weaponAmmoText.text = string.Empty;
            }

            // 触发卸装事件（跳过被排除的槽）
            if (!excluded)
            {
                OnItemUnequipped?.Invoke(config.slotType, unequippedItem);
            }

            Debug.Log($"[EquipmentSlot] 成功卸下 {unequippedItem?.ItemData?.itemName} 从 {config.slotName}");
            return unequippedItem;
        }

        #endregion

        #region UI相关

        /// <summary>
        /// 设置装备物品的Transform
        /// </summary>
        private void SetupEquippedItemTransform()
        {
            if (currentItemInstance == null || itemDisplayArea == null) return;

            var rectTransform = currentItemInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 设置父级
                rectTransform.SetParent(itemDisplayArea);

                // 重置位置和缩放
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localScale = Vector3.one;

                // 设置锚点为中心，但保持拖拽组件正常工作
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;

                // 自动调整物品大小以适应装备槽
                AdjustItemSizeToFitSlot(rectTransform);
            }

            // 禁用物品的网格相关组件，避免边界检查冲突
            DisableGridRelatedComponents();

            // 设置装备栏物品的ItemBackground为透明
            SetItemBackgroundTransparent();

            // 若为武器槽，尝试绑定武器组件
            if (IsWeaponSlot())
            {
                BindWeaponIfPresent(currentItemInstance);
                UpdateWeaponAmmoText();
            }
        }

        /// <summary>
        /// 调整物品大小以适应装备槽
        /// </summary>
        /// <param name="itemRectTransform">物品的RectTransform</param>
        private void AdjustItemSizeToFitSlot(RectTransform itemRectTransform)
        {
            if (itemRectTransform == null || itemDisplayArea == null) return;

            // 获取装备槽的尺寸
            RectTransform slotRect = GetSlotRectTransform();
            if (slotRect == null) return;

            Vector2 slotSize = slotRect.sizeDelta;

            // 获取内边距值（优先使用配置文件中的值）
            float padding = config != null ? config.itemPadding : 6f;

            // 计算考虑内边距后的可用空间
            Vector2 availableSize = new Vector2(
                slotSize.x - (padding * 2f),
                slotSize.y - (padding * 2f)
            );

            // 确保最小尺寸
            availableSize.x = Mathf.Max(availableSize.x, 32f);
            availableSize.y = Mathf.Max(availableSize.y, 32f);

            // 获取物品的真实视觉尺寸（而不是网格计算的尺寸）
            Vector2 originalSize = GetItemRealVisualSize(itemRectTransform);

            // 计算缩放比例
            float scale = 1f;
            Vector2 newSize = originalSize;

            // 如果物品尺寸超过可用空间，则按比例缩放
            if (originalSize.x > availableSize.x || originalSize.y > availableSize.y)
            {
                // 计算缩放比例，保持宽高比
                float scaleX = availableSize.x / originalSize.x;
                float scaleY = availableSize.y / originalSize.y;
                scale = Mathf.Min(scaleX, scaleY);

                // 应用缩放后的尺寸
                newSize = originalSize * scale;

                Debug.Log($"[EquipmentSlot] 调整物品 {currentEquippedItem.ItemData.itemName} 尺寸: {originalSize} -> {newSize} (缩放: {scale:F2})");
            }
            else
            {
                Debug.Log($"[EquipmentSlot] 保持物品 {currentEquippedItem.ItemData.itemName} 原始尺寸: {originalSize}");
            }

            // 缩放物品及其所有子组件
            ScaleItemAndChildren(itemRectTransform, newSize, scale);
        }

        /// <summary>
        /// 缩放物品及其所有子组件
        /// </summary>
        /// <param name="itemRectTransform">物品根节点的RectTransform</param>
        /// <param name="newSize">新的尺寸</param>
        /// <param name="scale">缩放比例</param>
        private void ScaleItemAndChildren(RectTransform itemRectTransform, Vector2 newSize, float scale)
        {
            if (itemRectTransform == null) return;

            // 设置主物品的尺寸
            itemRectTransform.sizeDelta = newSize;

            // 缩放所有子组件
            for (int i = 0; i < itemRectTransform.childCount; i++)
            {
                Transform child = itemRectTransform.GetChild(i);
                RectTransform childRect = child.GetComponent<RectTransform>();

                if (childRect != null)
                {
                    string childName = child.name;

                    if (childName == ItemPrefabConstants.ChildNames.ItemBackground ||
                        childName == ItemPrefabConstants.ChildNames.ItemIcon ||
                        childName == ItemPrefabConstants.ChildNames.ItemHighlight)
                    {
                        // 背景、图标和高亮需要与主物品同步缩放
                        childRect.sizeDelta = newSize;
                        Debug.Log($"[EquipmentSlot] 缩放子组件 {childName}: {newSize}");
                    }
                    else if (childName == ItemPrefabConstants.ChildNames.ItemText)
                    {
                        // 文本组件需要特殊处理，保持在右下角位置
                        ScaleItemText(childRect, scale);
                    }
                }
            }
        }

        /// <summary>
        /// 缩放物品文本组件并保持其在右下角位置
        /// </summary>
        /// <param name="textRect">文本的RectTransform</param>
        /// <param name="scale">缩放比例</param>
        private void ScaleItemText(RectTransform textRect, float scale)
        {
            if (textRect == null) return;

            // 获取文本组件
            var textComponent = textRect.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent == null) return;

            // 获取缩放后的物品尺寸
            var itemRectTransform = currentItemInstance.GetComponent<RectTransform>();
            if (itemRectTransform == null) return;

            Vector2 scaledItemSize = itemRectTransform.sizeDelta;

            // 使用通用方法计算文本位置和尺寸
            textRect.anchoredPosition = ItemPrefabConstants.ItemTextDefaults.CalculateTextPosition(scaledItemSize);
            textRect.sizeDelta = ItemPrefabConstants.ItemTextDefaults.CalculateTextSize(scaledItemSize);

            // 计算适合的字体大小
            float fontSize = ItemPrefabConstants.ItemTextDefaults.CalculateFontSize(scaledItemSize, scale);
            textComponent.fontSize = fontSize;

            Debug.Log($"[EquipmentSlot] 缩放文本组件: 位置={textRect.anchoredPosition}, 尺寸={textRect.sizeDelta}, 字体={fontSize}");
        }

        /// <summary>
        /// 获取装备槽的RectTransform
        /// </summary>
        /// <returns>装备槽的RectTransform</returns>
        private RectTransform GetSlotRectTransform()
        {
            // 优先使用物品显示区域的尺寸
            RectTransform displayAreaRect = itemDisplayArea.GetComponent<RectTransform>();
            if (displayAreaRect != null)
            {
                return displayAreaRect;
            }

            // 如果没有，使用装备槽背景的尺寸
            if (equipmentSlotBackground != null)
            {
                return equipmentSlotBackground.GetComponent<RectTransform>();
            }

            // 最后使用根节点的尺寸
            return GetComponent<RectTransform>();
        }

        /// <summary>
        /// 获取物品的真实视觉尺寸
        /// </summary>
        /// <param name="itemRectTransform">物品的RectTransform</param>
        /// <returns>物品的真实视觉尺寸</returns>
        private Vector2 GetItemRealVisualSize(RectTransform itemRectTransform)
        {
            if (itemRectTransform == null) return Vector2.zero;

            // 使用Item类的方法获取真实尺寸
            var itemComponent = itemRectTransform.GetComponent<Item>();
            if (itemComponent != null)
            {
                Vector2 realSize = itemComponent.GetRealVisualSize();
                Debug.Log($"[EquipmentSlot] 获取物品真实尺寸: {realSize}");
                return realSize;
            }

            // 如果没有Item组件，使用当前RectTransform尺寸
            Vector2 currentSize = itemRectTransform.sizeDelta;
            Debug.Log($"[EquipmentSlot] 使用当前RectTransform尺寸: {currentSize}");
            return currentSize;
        }

        /// <summary>
        /// 延迟一帧确保物品尺寸设置正确
        /// </summary>
        /// <returns></returns>
        private IEnumerator EnsureItemSizeAfterFrame()
        {
            yield return null; // 等待一帧

            if (currentItemInstance != null && isItemEquipped)
            {
                var rectTransform = currentItemInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 再次调整尺寸以确保不被其他系统覆盖
                    AdjustItemSizeToFitSlot(rectTransform);
                    Debug.Log($"[EquipmentSlot] 延迟确保物品 {currentEquippedItem.ItemData.itemName} 尺寸正确");
                }
            }
        }

        /// <summary>
        /// 禁用网格相关组件
        /// </summary>
        private void DisableGridRelatedComponents()
        {
            if (currentItemInstance == null) return;

            // 暂时禁用GridInteract组件（如果有的话）
            var gridInteract = currentItemInstance.GetComponent<GridInteract>();
            if (gridInteract != null)
            {
                gridInteract.enabled = false;
            }

            // 确保Item组件的网格状态已清除
            var itemComponent = currentItemInstance.GetComponent<Item>();
            if (itemComponent != null)
            {
                // 清除网格引用，避免边界检查
                itemComponent.ResetGridState();
                // 确保物品不在任何网格的控制下
                itemComponent.OnGridReference = null;

                // 禁用自动网格尺寸调整，保持装备槽的缩放设置
                itemComponent.SetAutoGridSizeAdjustment(false);
            }

            // 如果物品有高亮组件，暂时禁用
            var itemHighlight = currentItemInstance.GetComponent<ItemHighlight>();
            if (itemHighlight != null)
            {
                itemHighlight.HideHighlight();
            }

            // 保持碰撞器启用以支持拖拽检测
            // 碰撞器对于拖拽功能是必需的，所以不禁用
        }

        /// <summary>
        /// 恢复网格相关组件
        /// </summary>
        private void RestoreGridRelatedComponents(GameObject itemInstance)
        {
            if (itemInstance == null) return;

            // 重新启用GridInteract组件
            var gridInteract = itemInstance.GetComponent<GridInteract>();
            if (gridInteract != null)
            {
                gridInteract.enabled = true;
            }

            // 重新启用自动网格尺寸调整
            var itemComponent = itemInstance.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.SetAutoGridSizeAdjustment(true);

                // 强制重新应用网格尺寸，这会同时处理所有子组件
                itemComponent.AdjustVisualSizeForGrid();
            }

            // 碰撞器一直保持启用状态，无需恢复

            // 恢复ItemBackground的原始颜色
            RestoreItemBackgroundColor(itemInstance);

            Debug.Log($"[EquipmentSlot] 已恢复物品 {itemInstance.name} 的网格相关组件，包括子组件尺寸");
        }

        /// <summary>
        /// 恢复物品原始尺寸
        /// </summary>
        /// <param name="itemInstance">物品实例</param>
        private void RestoreItemOriginalSize(GameObject itemInstance)
        {
            if (itemInstance == null) return;

            var rectTransform = itemInstance.GetComponent<RectTransform>();
            if (rectTransform != null && originalItemSize != Vector2.zero)
            {
                // 恢复物品及其所有子组件的原始尺寸
                RestoreItemAndChildrenSize(rectTransform, originalItemSize);

                Debug.Log($"[EquipmentSlot] 恢复物品原始尺寸: {originalItemSize}");

                // 清除记录的原始尺寸
                originalItemSize = Vector2.zero;
            }
        }

        /// <summary>
        /// 恢复物品及其所有子组件的原始尺寸
        /// </summary>
        /// <param name="itemRectTransform">物品根节点的RectTransform</param>
        /// <param name="originalSize">原始尺寸</param>
        private void RestoreItemAndChildrenSize(RectTransform itemRectTransform, Vector2 originalSize)
        {
            if (itemRectTransform == null) return;

            // 恢复主物品的尺寸
            itemRectTransform.sizeDelta = originalSize;

            // 恢复所有子组件的原始尺寸
            for (int i = 0; i < itemRectTransform.childCount; i++)
            {
                Transform child = itemRectTransform.GetChild(i);
                RectTransform childRect = child.GetComponent<RectTransform>();

                if (childRect != null)
                {
                    string childName = child.name;

                    if (childName == ItemPrefabConstants.ChildNames.ItemBackground ||
                        childName == ItemPrefabConstants.ChildNames.ItemIcon ||
                        childName == ItemPrefabConstants.ChildNames.ItemHighlight)
                    {
                        // 背景、图标和高亮恢复为与主物品相同的尺寸
                        childRect.sizeDelta = originalSize;
                        Debug.Log($"[EquipmentSlot] 恢复子组件 {childName} 原始尺寸: {originalSize}");
                    }
                    else if (childName == ItemPrefabConstants.ChildNames.ItemText)
                    {
                        // 文本组件恢复原始位置和尺寸
                        RestoreItemTextOriginalSize(childRect);
                    }
                }
            }
        }

        /// <summary>
        /// 恢复物品文本组件的原始尺寸和位置（基于物品当前尺寸）
        /// </summary>
        /// <param name="textRect">文本的RectTransform</param>
        private void RestoreItemTextOriginalSize(RectTransform textRect)
        {
            if (textRect == null) return;

            // 获取文本组件
            var textComponent = textRect.GetComponent<TMPro.TextMeshProUGUI>();
            if (textComponent == null) return;

            // 获取物品当前尺寸
            var itemRectTransform = textRect.parent.GetComponent<RectTransform>();
            if (itemRectTransform == null) return;

            Vector2 itemSize = itemRectTransform.sizeDelta;

            // 使用通用方法计算文本位置和尺寸
            textRect.anchoredPosition = ItemPrefabConstants.ItemTextDefaults.CalculateTextPosition(itemSize);
            textRect.sizeDelta = ItemPrefabConstants.ItemTextDefaults.CalculateTextSize(itemSize);
            textComponent.fontSize = ItemPrefabConstants.ItemTextDefaults.CalculateFontSize(itemSize);

            Debug.Log($"[EquipmentSlot] 恢复文本组件尺寸: 位置={textRect.anchoredPosition}, 尺寸={textRect.sizeDelta}, 字体={textComponent.fontSize}");
        }

        /// <summary>
        /// 更新槽位显示
        /// </summary>
        private void UpdateSlotDisplay()
        {
            if (equipmentSlotBackground == null) return;

            if (isItemEquipped)
            {
                // 有装备时的显示状态
                equipmentSlotBackground.color = Color.gray;
            }
            else
            {
                // 空槽时的显示状态
                equipmentSlotBackground.color = Color.white;
            }

            // 同步武器弹药文本的可见性（若引用存在且本槽是武器槽）
            if (weaponAmmoText != null && IsWeaponSlot())
            {
                if (isItemEquipped)
                {
                    UpdateWeaponAmmoText();
                }
                else
                {
                    weaponAmmoText.text = string.Empty;
                }
            }
        }

        /// <summary>
        /// 设置拖拽高亮
        /// </summary>
        /// <param name="canEquip">是否可以装备</param>
        /// <param name="show">是否显示高亮</param>
        private void SetDragHighlight(bool canEquip, bool show)
        {
            if (equipmentSlotBackground == null) return;

            if (show)
            {
                Color highlightColor = canEquip ? config.canEquipHighlightColor : config.cannotEquipHighlightColor;
                equipmentSlotBackground.color = highlightColor;
            }
            else
            {
                UpdateSlotDisplay(); // 恢复正常显示
            }
        }

        /// <summary>
        /// 设置装备栏物品的ItemBackground为透明
        /// </summary>
        private void SetItemBackgroundTransparent()
        {
            if (currentItemInstance == null) return;

            // 查找ItemBackground子对象
            Transform backgroundTransform = currentItemInstance.transform.Find(ItemPrefabConstants.ChildNames.ItemBackground);
            if (backgroundTransform == null) return;

            // 获取Image组件
            var backgroundImage = backgroundTransform.GetComponent<UnityEngine.UI.Image>();
            if (backgroundImage == null) return;

            // 保存原始颜色
            originalBackgroundColor = backgroundImage.color;

            // 设置为透明（保持RGB值，只改变Alpha）
            Color transparentColor = originalBackgroundColor;
            transparentColor.a = 0f;
            backgroundImage.color = transparentColor;

            LogDebugInfo($"设置物品 {currentItemInstance.name} 的ItemBackground为透明，原始颜色: {originalBackgroundColor}");
        }

        /// <summary>
        /// 恢复装备栏物品的ItemBackground原始透明度
        /// </summary>
        /// <param name="itemInstance">物品实例</param>
        private void RestoreItemBackgroundColor(GameObject itemInstance)
        {
            if (itemInstance == null || originalBackgroundColor == default(Color)) return;

            // 查找ItemBackground子对象
            Transform backgroundTransform = itemInstance.transform.Find(ItemPrefabConstants.ChildNames.ItemBackground);
            if (backgroundTransform == null) return;

            // 获取Image组件
            var backgroundImage = backgroundTransform.GetComponent<UnityEngine.UI.Image>();
            if (backgroundImage == null) return;

            // 恢复原始颜色
            backgroundImage.color = originalBackgroundColor;

            LogDebugInfo($"恢复物品 {itemInstance.name} 的ItemBackground原始颜色: {originalBackgroundColor}");

            // 清除记录的原始颜色
            originalBackgroundColor = default(Color);
        }

        #endregion

        #region 容器功能

        /// <summary>
        /// 强制激活容器网格（用于装备恢复时槽位未激活的情况）
        /// </summary>
        public void ForceActivateContainerGrid()
        {
            if (!config.isContainerSlot || currentEquippedItem == null) return;

            LogDebugInfo($"强制激活容器网格: {config.slotName}");
            ActivateContainerGrid();
        }

        /// <summary>
        /// 激活容器网格（重构版本，集成新系统）
        /// </summary>
        private void ActivateContainerGrid()
        {
            if (!config.isContainerSlot || currentEquippedItem == null) return;

            LogDebugInfo($"🔄 开始激活容器网格");

            // 🔧 修复：确保旧的容器网格完全清理
            if (containerGrid != null)
            {
                LogDebugInfo($"⚠️ 发现现有容器网格，先进行清理: {containerGrid.name}");
                DeactivateContainerGrid();
            }

            // 获取容器尺寸
            Vector2Int containerSize = GetContainerSize();

            // 创建容器网格
            CreateContainerGrid(containerSize);

            // 🔧 修复：添加重复加载保护，只有在网格创建成功后才加载内容
            if (containerGrid != null)
            {
                // 加载容器内容
                LoadContainerContent();

                // 触发容器激活事件
                OnContainerSlotActivated?.Invoke(config.slotType, containerGrid);

                LogDebugInfo($"✅ 容器网格激活完成: {containerSize.x}x{containerSize.y}");
            }
            else
            {
                Debug.LogError($"[EquipmentSlot] 容器网格创建失败，无法激活容器功能");
            }
        }

        /// <summary>
        /// 停用容器网格（重构版本，集成新系统）
        /// </summary>
        private void DeactivateContainerGrid()
        {
            if (containerGrid == null) return;

            LogDebugInfo($"🗑️ 开始停用并销毁容器网格: {containerGrid.name}");

            // 立即保存容器内容（网格销毁前）
            SaveContainerImmediate();

            // 取消事件监听，防止内存泄漏
            CleanupContainerEventListeners();

            // 触发容器停用事件
            OnContainerSlotDeactivated?.Invoke(config.slotType, containerGrid);

            // 🔧 修复：立即且彻底销毁容器网格，避免残留
            var gridToDestroy = containerGrid.gameObject;
            containerGrid = null; // 先清空引用

            if (gridToDestroy != null)
            {
                // 先强制清理网格中的所有物品
                var itemGrid = gridToDestroy.GetComponent<ItemGrid>();
                if (itemGrid != null)
                {
                    ForceCleanupContainerItems(itemGrid);
                }

                // 立即销毁，不再延迟
                Destroy(gridToDestroy);
                LogDebugInfo($"🗑️ 立即销毁容器网格完成");
            }

            // 通知InventoryController刷新网格列表，清除已销毁的网格引用
            RefreshInventoryControllerGrids();

            LogDebugInfo($"✅ 容器网格停用完成");
        }

        /// <summary>
        /// 强制清理容器网格中的所有物品
        /// </summary>
        /// <param name="itemGrid">要清理的容器网格</param>
        private void ForceCleanupContainerItems(ItemGrid itemGrid)
        {
            if (itemGrid == null) return;

            LogDebugInfo($"🧹 开始强制清理容器网格物品: {itemGrid.name}");

            var itemsToDestroy = new List<GameObject>();

            try
            {
                // 收集所有子对象中的物品
                for (int i = itemGrid.transform.childCount - 1; i >= 0; i--)
                {
                    var child = itemGrid.transform.GetChild(i);
                    if (child != null)
                    {
                        var item = child.GetComponent<Item>();
                        var draggableItem = child.GetComponent<DraggableItem>();

                        // 如果是物品对象，添加到销毁列表
                        if (item != null || draggableItem != null)
                        {
                            itemsToDestroy.Add(child.gameObject);
                        }
                    }
                }

                // 立即销毁所有物品
                foreach (var itemObj in itemsToDestroy)
                {
                    if (itemObj != null)
                    {
                        DestroyImmediate(itemObj);
                    }
                }

                LogDebugInfo($"🧹 强制清理完成，销毁了 {itemsToDestroy.Count} 个物品");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlot] 强制清理容器物品时发生错误: {e.Message}");
            }
        }

        /// <summary>
        /// 延迟销毁网格GameObject（已弃用，改为立即销毁）
        /// </summary>
        /// <param name="gridToDestroy">要销毁的网格GameObject</param>
        [System.Obsolete("已改为立即销毁，此方法保留用于兼容性")]
        private System.Collections.IEnumerator DelayedDestroyGrid(GameObject gridToDestroy)
        {
            // 等待几帧确保保存操作完全完成
            yield return null;
            yield return null;

            if (gridToDestroy != null)
            {
                Destroy(gridToDestroy);
                LogDebugInfo($"延迟销毁容器网格完成");
            }
        }

        /// <summary>
        /// 刷新InventoryController的网格列表
        /// </summary>
        private void RefreshInventoryControllerGrids()
        {
            // 查找InventoryController并刷新其网格列表
            var inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController != null)
            {
                // 延迟一帧刷新，确保销毁操作完成
                StartCoroutine(DelayedRefreshGrids(inventoryController));
                LogDebugInfo($"通知InventoryController刷新网格列表");
            }
        }

        /// <summary>
        /// 延迟刷新网格列表（确保销毁操作完成）
        /// </summary>
        /// <param name="controller">InventoryController实例</param>
        private System.Collections.IEnumerator DelayedRefreshGrids(InventoryController controller)
        {
            yield return null; // 等待一帧
            if (controller != null)
            {
                controller.RefreshGridInteracts();
                LogDebugInfo($"InventoryController网格列表已刷新");
            }
        }

        /// <summary>
        /// 获取容器尺寸
        /// </summary>
        /// <returns>容器尺寸</returns>
        private Vector2Int GetContainerSize()
        {
            if (currentEquippedItem?.ItemData != null && currentEquippedItem.ItemData.IsContainer())
            {
                // 使用物品的容器尺寸
                return new Vector2Int(currentEquippedItem.ItemData.cellH, currentEquippedItem.ItemData.cellV);
            }

            // 使用配置的默认尺寸
            return config.defaultContainerSize;
        }

        /// <summary>
        /// 创建容器网格
        /// </summary>
        /// <param name="size">网格尺寸</param>
        private void CreateContainerGrid(Vector2Int size)
        {
            if (config.containerGridPrefab == null)
            {
                Debug.LogWarning($"[EquipmentSlot] {config.slotName}: 未设置容器网格预制件");
                return;
            }

            // 确定容器父级 - 根据装备槽类型查找对应的容器对象
            Transform parent = FindContainerParent();

            // 实例化容器网格
            GameObject gridObject = Instantiate(config.containerGridPrefab, parent);
            containerGrid = gridObject.GetComponent<ItemGrid>();

            if (containerGrid != null)
            {
                // 设置网格逻辑尺寸
                containerGrid.gridSizeWidth = size.x;
                containerGrid.gridSizeHeight = size.y;

                // 设置网格名称和类型
                containerGrid.GridName = $"{config.slotName}_Container";
                containerGrid.GridType = GridType.Equipment;

                // 动态调整网格UI尺寸
                AdjustContainerGridSize(containerGrid, size);

                // 设置位置偏移
                var rectTransform = containerGrid.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = config.containerDisplayOffset;
                }

                // 确保容器网格支持完整的交互功能
                SetupContainerGridInteraction(containerGrid);

                // 容器内容加载已移除

                LogDebugInfo($"创建容器网格 - 尺寸: {size.x}x{size.y}, UI尺寸: {size.x * 64}x{size.y * 64}");
            }
        }

        /// <summary>
        /// 动态调整容器网格的UI尺寸
        /// </summary>
        /// <param name="grid">容器网格</param>
        /// <param name="size">目标网格尺寸</param>
        private void AdjustContainerGridSize(ItemGrid grid, Vector2Int size)
        {
            const float CELL_SIZE = 64f; // 单元格尺寸

            // 计算目标UI尺寸
            Vector2 targetSize = new Vector2(size.x * CELL_SIZE, size.y * CELL_SIZE);

            // 获取网格的RectTransform
            var rectTransform = grid.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 设置网格的UI尺寸
                rectTransform.sizeDelta = targetSize;

                // 确保锚点设置正确（左上角对齐）
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);

                LogDebugInfo($"调整容器网格UI尺寸: {targetSize.x}x{targetSize.y}");
            }

            // 获取背景Image组件并调整尺寸
            var backgroundImage = grid.GetComponent<UnityEngine.UI.Image>();
            if (backgroundImage != null)
            {
                // 背景图片会自动跟随RectTransform尺寸
                LogDebugInfo($"背景图片已自动调整到网格尺寸");
            }
        }

        /// <summary>
        /// 设置容器网格的交互功能
        /// </summary>
        /// <param name="grid">容器网格</param>
        private void SetupContainerGridInteraction(ItemGrid grid)
        {
            if (grid == null) return;

            // 确保网格有GridInteract组件用于交互
            var gridInteract = grid.GetComponent<GridInteract>();
            if (gridInteract == null)
            {
                gridInteract = grid.gameObject.AddComponent<GridInteract>();
                LogDebugInfo($"为容器网格添加了GridInteract组件");
            }

            // 查找InventoryController并设置
            var inventoryController = FindObjectOfType<InventoryController>();
            if (inventoryController != null && gridInteract != null)
            {
                gridInteract.SetInventoryController(inventoryController);
                LogDebugInfo($"为容器网格设置了InventoryController");
            }

            // 确保网格在InventoryController中注册
            if (inventoryController != null)
            {
                inventoryController.RefreshGridInteracts();
                LogDebugInfo($"刷新了InventoryController的网格交互列表");
            }

            // 确保容器网格支持拖拽交互
            grid.GridType = GridType.Equipment;  // 设置为装备类型网格

            // 监听容器网格的物品变化事件，实现自动保存
            SetupContainerAutoSave(grid);

            LogDebugInfo($"容器网格交互功能设置完成");
        }

        /// <summary>
        /// 设置容器自动保存功能
        /// </summary>
        /// <param name="grid">容器网格</param>
        private void SetupContainerAutoSave(ItemGrid grid)
        {
            if (grid == null) return;

            // 监听物品放置事件
            grid.OnItemPlaced += OnContainerItemPlaced;
            grid.OnItemRemoved += OnContainerItemRemoved;

            LogDebugInfo($"设置容器自动保存监听器");
        }

        /// <summary>
        /// 容器物品放置事件处理
        /// </summary>
        /// <param name="item">放置的物品</param>
        /// <param name="position">放置位置</param>
        private void OnContainerItemPlaced(Item item, Vector2Int position)
        {
            LogDebugInfo($"📦 容器中放置物品: {item.name} 到位置 {position}");

            // 🔧 强化：立即保存容器内容，确保状态同步
            // 防止在背包重复装备时丢失新放入的物品
            SaveContainerContentImmediate();
            
            LogDebugInfo($"💾 物品放置后立即保存完成，确保状态同步");
        }

        /// <summary>
        /// 容器物品移除事件处理
        /// </summary>
        /// <param name="item">移除的物品</param>
        /// <param name="position">移除位置</param>
        private void OnContainerItemRemoved(Item item, Vector2Int position)
        {
            LogDebugInfo($"🔄 从容器中移除物品: {item.name} 从位置 {position}");

            // 🔧 修复：延迟保存确保物品已从网格中完全移除
            // 这是防止保存时序问题的关键修复
            StartCoroutine(DelayedSaveAfterItemRemoval(item, position));
            
            LogDebugInfo($"⏰ 物品移除后启动延迟保存，确保时序正确");
        }

        /// <summary>
        /// 延迟保存协程，确保物品已从网格中完全移除后再保存
        /// </summary>
        private System.Collections.IEnumerator DelayedSaveAfterItemRemoval(Item item, Vector2Int position)
        {
            // 等待当前帧结束，确保物品已从网格中移除
            yield return null;
            
            // 再等待一帧，确保所有相关的清理操作都完成
            yield return null;
            
            // 验证物品确实已从网格中移除
            bool itemStillExists = false;
            if (containerGrid != null)
            {
                try
                {
                    Item gridItem = containerGrid.GetItemAt(position.x, position.y);
                    itemStillExists = (gridItem != null && gridItem == item);
                }
                catch
                {
                    // 如果访问失败，认为物品已被移除
                    itemStillExists = false;
                }
            }
            
            if (itemStillExists)
            {
                LogDebugInfo($"⚠️ 物品 {item.name} 仍在网格中，再等待一帧");
                yield return null;
            }
            
            // 现在执行保存
            LogDebugInfo($"💾 延迟保存开始 - 物品 {item.name} 已从位置 {position} 移除");
            SaveContainerContentImmediate();
            LogDebugInfo($"✅ 延迟保存完成，防止物品复制");
        }

        /// <summary>
        /// 清理容器事件监听器
        /// </summary>
        private void CleanupContainerEventListeners()
        {
            if (containerGrid != null)
            {
                containerGrid.OnItemPlaced -= OnContainerItemPlaced;
                containerGrid.OnItemRemoved -= OnContainerItemRemoved;
                LogDebugInfo($"清理容器事件监听器");
            }
        }

        /// <summary>
        /// 根据装备槽类型查找对应的容器父级对象
        /// </summary>
        /// <returns>容器父级Transform</returns>
        private Transform FindContainerParent()
        {
            // 如果已经手动设置了容器父级，优先使用
            if (containerParent != null)
            {
                return containerParent;
            }

            // 根据装备槽类型查找对应的容器对象
            string containerName = "";
            switch (config.slotType)
            {
                case EquipmentSlotType.Backpack:
                    containerName = "PackContainer";
                    break;
                case EquipmentSlotType.TacticalRig:
                    containerName = "RigContainer";
                    break;
                default:
                    // 其他类型使用默认父级
                    return transform.parent;
            }

            // 在当前装备槽中查找指定名称的子对象
            Transform containerTransform = transform.Find(containerName);
            if (containerTransform != null)
            {
                LogDebugInfo($"找到容器父级: {containerName}");
                return containerTransform;
            }

            // 如果没找到，尝试在整个装备槽预制件中递归查找
            containerTransform = FindChildRecursive(transform, containerName);
            if (containerTransform != null)
            {
                LogDebugInfo($"递归找到容器父级: {containerName}");
                return containerTransform;
            }

            // 都没找到，使用默认父级并警告
            Debug.LogWarning($"[EquipmentSlot] {config.slotName}: 未找到容器对象 {containerName}，使用默认父级");
            return transform.parent;
        }

        /// <summary>
        /// 递归查找指定名称的子对象
        /// </summary>
        /// <param name="parent">父级对象</param>
        /// <param name="name">要查找的对象名称</param>
        /// <returns>找到的Transform，如果没找到返回null</returns>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            // 检查直接子对象
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                // 递归检查子对象的子对象
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        #endregion

        #region 拖拽接口实现

        /// <summary>
        /// 拖拽放下处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnDrop(PointerEventData eventData)
        {
            // 获取拖拽的物品
            var draggedObject = eventData.pointerDrag;
            if (draggedObject == null) return;

            var itemDataReader = draggedObject.GetComponent<ItemDataReader>();
            if (itemDataReader == null) return;

            // 尝试装备物品
            bool equipSuccess = EquipItem(itemDataReader);

            // 清除拖拽高亮
            SetDragHighlight(false, false);
            isDragHovering = false;

            // 通知控制器拖拽结束
            if (inventoryController != null)
            {
                inventoryController.OnItemDragEnd();
            }
        }

        /// <summary>
        /// 鼠标点击处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 右键点击卸下装备
            if (eventData.button == PointerEventData.InputButton.Right && isItemEquipped)
            {
                var unequippedItem = UnequipItem();
                if (unequippedItem != null && config.returnToOriginalPosition)
                {
                    TryReturnItemToInventory(unequippedItem);
                }
            }

        }

        /// <summary>
        /// 鼠标进入处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 检查是否有物品正在拖拽
            if (eventData.pointerDrag != null)
            {
                var itemDataReader = eventData.pointerDrag.GetComponent<ItemDataReader>();
                if (itemDataReader != null)
                {
                    bool canEquip = CanAcceptItem(itemDataReader);
                    SetDragHighlight(canEquip, true);
                    isDragHovering = true;
                }
            }
        }

        /// <summary>
        /// 鼠标退出处理
        /// </summary>
        /// <param name="eventData">事件数据</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragHovering)
            {
                SetDragHighlight(false, false);
                isDragHovering = false;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 尝试将物品返回背包
        /// </summary>
        /// <param name="item">要返回的物品</param>
        private void TryReturnItemToInventory(ItemDataReader item)
        {
            if (item == null || item.gameObject == null)
            {
                Debug.LogWarning("[EquipmentSlot] 尝试返回背包的物品为null或已被销毁");
                return;
            }

            // 确保恢复网格相关组件
            try
            {
                RestoreGridRelatedComponents(item.gameObject);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlot] 恢复物品组件时出错: {e.Message}");
                return;
            }

            // 尝试返回原位置
            if (originalItemGrid != null && config.returnToOriginalPosition)
            {
                var itemComponent = item.GetComponent<Item>();
                if (itemComponent != null)
                {
                    bool returnSuccess = originalItemGrid.PlaceItem(itemComponent,
                        originalItemPosition.x, originalItemPosition.y);

                    if (returnSuccess)
                    {
                        Debug.Log($"[EquipmentSlot] 成功将 {item.ItemData.itemName} 返回原位置");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"[EquipmentSlot] 物品 {item.ItemData.itemName} 缺少Item组件");
                }
            }

            // 如果无法返回原位置，尝试在任意可用位置放置
            if (TryPlaceItemInAnyGrid(item))
            {
                Debug.Log($"[EquipmentSlot] 成功将 {item.ItemData.itemName} 放置到其他位置");
                return;
            }

            // 如果完全无法放置，销毁物品（或可以实现其他逻辑，如掉落到地面）
            Debug.LogWarning($"[EquipmentSlot] 无法将 {item.ItemData.itemName} 返回背包，已销毁");
            Destroy(item.gameObject);

            // 若为武器槽，清空显示
            if (IsWeaponSlot() && weaponAmmoText != null)
            {
                weaponAmmoText.text = string.Empty;
            }
        }

        /// <summary>
        /// 尝试在任意网格中放置物品
        /// </summary>
        /// <param name="item">要放置的物品</param>
        /// <returns>是否成功放置</returns>
        private bool TryPlaceItemInAnyGrid(ItemDataReader item)
        {
            if (item == null) return false;

            // 查找所有可用的网格
            var allGrids = FindObjectsOfType<ItemGrid>();
            var itemComponent = item.GetComponent<Item>();

            if (itemComponent == null) return false;

            foreach (var grid in allGrids)
            {
                // 跳过装备槽相关的网格
                if (grid.GridType == GridType.Equipment) continue;

                // 尝试在网格中找到空位置
                for (int x = 0; x < grid.gridSizeWidth; x++)
                {
                    for (int y = 0; y < grid.gridSizeHeight; y++)
                    {
                        if (grid.PlaceItem(itemComponent, x, y))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 是否有装备
        /// </summary>
        public bool HasEquippedItem => isItemEquipped;

        /// <summary>
        /// 当前装备的物品
        /// </summary>
        public ItemDataReader CurrentEquippedItem => currentEquippedItem;

        /// <summary>
        /// 装备槽类型
        /// </summary>
        public EquipmentSlotType SlotType => config?.slotType ?? EquipmentSlotType.Helmet;

        /// <summary>
        /// 装备槽名称
        /// </summary>
        public string SlotName => config?.slotName ?? "未知槽位";

        /// <summary>
        /// 容器网格（如果是容器槽位）
        /// </summary>
        public ItemGrid ContainerGrid => containerGrid;

        /// <summary>
        /// 获取装备槽状态信息
        /// </summary>
        /// <returns>状态信息</returns>
        public string GetSlotStatusInfo()
        {
            if (config == null) return "配置缺失";

            if (isItemEquipped && currentEquippedItem != null)
            {
                return $"已装备: {currentEquippedItem.ItemData.itemName}";
            }

            return $"空槽位 - 允许类别: {config.GetAllowedCategoriesDisplay()}";
        }

        #endregion

        #region 调试功能

        /// <summary>
        /// 调试信息
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogDebugInfo(string message)
        {
            Debug.Log($"[EquipmentSlot] {config?.slotName ?? "Unknown"}: {message}");
        }

        #endregion

        #region 容器持久化集成 - ContainerSaveManager

        /// <summary>
        /// 保存容器内容
        /// </summary>
        private void SaveContainerContent()
        {
            if (containerGrid == null || currentEquippedItem == null) return;

            try
            {
                ContainerSaveManager.Instance.SaveContainerContent(
                    currentEquippedItem,
                    config.slotType,
                    containerGrid
                );
                LogDebugInfo($"触发容器内容保存");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlot] 保存容器内容失败: {e.Message}");
            }
        }

        /// <summary>
        /// 加载容器内容
        /// </summary>
        private void LoadContainerContent()
        {
            if (containerGrid == null || currentEquippedItem == null) return;

            try
            {
                // 🔧 修复：检查GameObject是否激活，如果不激活则设置标志待后续加载
                if (gameObject.activeInHierarchy)
                {
                    // 延迟一帧加载，确保网格完全初始化
                    StartCoroutine(DelayedLoadContainerContent());
                }
                else
                {
                    // 设置标志，在OnEnable时加载
                    needsContainerContentLoad = true;
                    LogDebugInfo($"装备槽未激活，设置延迟加载标志");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlot] 加载容器内容失败: {e.Message}");
            }
        }

        /// <summary>
        /// 延迟加载容器内容
        /// </summary>
        private System.Collections.IEnumerator DelayedLoadContainerContent()
        {
            yield return null; // 等待一帧确保网格完全初始化

            if (containerGrid != null && currentEquippedItem != null)
            {
                ContainerSaveManager.Instance.LoadContainerContent(
                    currentEquippedItem,
                    config.slotType,
                    containerGrid
                );
                LogDebugInfo($"触发容器内容加载");
            }
        }

        /// <summary>
        /// 立即保存容器内容（用于对象销毁前）
        /// </summary>
        private void SaveContainerImmediate()
        {
            if (containerGrid == null || currentEquippedItem == null) return;

            try
            {
                // 确保网格初始化完成再保存
                if (!containerGrid.IsGridInitialized)
                {
                    LogDebugInfo($"网格未初始化，跳过立即保存");
                    return;
                }

                // 直接调用保存，不依赖协程
                ContainerSaveManager.Instance.SaveContainerContent(
                    currentEquippedItem,
                    config.slotType,
                    containerGrid
                );
                LogDebugInfo($"立即保存容器内容完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlot] 立即保存容器内容失败: {e.Message}");
            }
        }

        /// <summary>
        /// 立即保存容器内容（强化版本，用于物品变化时的实时保存）
        /// </summary>
        private void SaveContainerContentImmediate()
        {
            if (containerGrid == null || currentEquippedItem == null) return;

            try
            {
                // 确保网格初始化完成再保存
                if (!containerGrid.IsGridInitialized)
                {
                    LogDebugInfo($"⚠️ 网格未初始化，延迟保存");
                    // 如果网格未初始化，尝试延迟保存
                    StartCoroutine(DelayedSaveContainer());
                    return;
                }

                // 🔧 强化：绕过所有节流机制，立即保存
                LogDebugInfo($"💾 强制立即保存容器内容...");
                
                ContainerSaveManager.Instance.SaveContainerContentImmediate(
                    currentEquippedItem,
                    config.slotType,
                    containerGrid
                );
                
                LogDebugInfo($"✅ 强制立即保存容器内容完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EquipmentSlot] 强制立即保存容器内容失败: {e.Message}");
            }
        }

        /// <summary>
        /// 延迟保存容器内容（当网格未初始化时使用）
        /// </summary>
        private System.Collections.IEnumerator DelayedSaveContainer()
        {
            // 等待网格初始化
            int maxRetries = 10;
            for (int i = 0; i < maxRetries; i++)
            {
                yield return null;
                
                if (containerGrid != null && containerGrid.IsGridInitialized)
                {
                    SaveContainerImmediate();
                    yield break;
                }
            }
            
            Debug.LogWarning($"[EquipmentSlot] 网格初始化超时，无法保存容器内容");
        }

        #endregion

        #region 武器弹药显示集成

        private bool IsWeaponSlot()
        {
            return config != null && (config.slotType == EquipmentSlotType.PrimaryWeapon || config.slotType == EquipmentSlotType.SecondaryWeapon);
        }

        private void BindWeaponIfPresent(GameObject itemGO)
        {
            UnbindObservedWeaponEvents();
            observedWeaponManager = null;
            weaponSlotIndexCache = MapWeaponSlotIndex();

            if (itemGO == null || weaponSlotIndexCache == null) return;

            // 物品实例本身可能不是武器Prefab，需要在其子层级中寻找 WeaponManager
            var wm = itemGO.GetComponentInChildren<WeaponManager>();
            if (wm == null)
            {
                // 通过玩家武器控制器（若存在）定位当前槽位的武器实例
                var player = FindObjectOfType<PlayerWeaponEquipController>();
                if (player != null && weaponSlotIndexCache.Value >= 0 && weaponSlotIndexCache.Value < player.equipped.Length)
                {
                    var equippedGO = player.equipped[weaponSlotIndexCache.Value];
                    wm = equippedGO != null ? equippedGO.GetComponent<WeaponManager>() : null;
                }
            }

            if (wm != null)
            {
                observedWeaponManager = wm;
                // 绑定事件以在开火与换弹时更新
                observedWeaponManager.OnFired += OnObservedWeaponFired;
                observedWeaponManager.OnReloadEnd += OnObservedWeaponReloadEnd;
                UpdateWeaponAmmoText();
            }
        }

        private void UnbindObservedWeaponEvents()
        {
            if (observedWeaponManager != null)
            {
                observedWeaponManager.OnFired -= OnObservedWeaponFired;
                observedWeaponManager.OnReloadEnd -= OnObservedWeaponReloadEnd;
                observedWeaponManager = null;
            }
        }

        private int? MapWeaponSlotIndex()
        {
            if (config == null) return null;
            switch (config.slotType)
            {
                case EquipmentSlotType.PrimaryWeapon: return 0;
                case EquipmentSlotType.SecondaryWeapon: return 1;
                default: return null;
            }
        }

        private void OnWeaponEquipped(WeaponMessageBus.WeaponEquippedEvent msg)
        {
            if (weaponSlotIndexCache == null) weaponSlotIndexCache = MapWeaponSlotIndex();
            if (weaponSlotIndexCache == null) return;
            if (msg.slotIndex != weaponSlotIndexCache.Value) return;

            // 绑定新武器
            BindWeaponIfPresent(msg.instance);
            UpdateWeaponAmmoText();
        }

        private void OnWeaponUnequipped(WeaponMessageBus.WeaponUnequippedEvent msg)
        {
            if (weaponSlotIndexCache == null) weaponSlotIndexCache = MapWeaponSlotIndex();
            if (weaponSlotIndexCache == null) return;
            if (msg.slotIndex != weaponSlotIndexCache.Value) return;

            UnbindObservedWeaponEvents();
            if (weaponAmmoText != null) weaponAmmoText.text = string.Empty;
        }

        private void OnObservedWeaponFired(WeaponManager _)
        {
            UpdateWeaponAmmoText();
        }

        private void OnObservedWeaponReloadEnd(WeaponManager _)
        {
            UpdateWeaponAmmoText();
        }

        private void UpdateWeaponAmmoText()
        {
            if (weaponAmmoText == null) return;
            if (!IsWeaponSlot()) { weaponAmmoText.text = string.Empty; return; }

            if (!isItemEquipped || currentEquippedItem == null)
            {
                weaponAmmoText.text = string.Empty;
                return;
            }

            // 优先从已绑定的 WeaponManager 获取
            if (observedWeaponManager != null)
            {
                int current = observedWeaponManager.GetCurrentAmmo();
                int total = observedWeaponManager.GetMagazineCapacity();
                weaponAmmoText.text = $"{current}/{total}";
                return;
            }

            // 备用：尝试在当前实例查找
            var wm = currentItemInstance != null ? currentItemInstance.GetComponentInChildren<WeaponManager>() : null;
            if (wm != null)
            {
                int current = wm.GetCurrentAmmo();
                int total = wm.GetMagazineCapacity();
                weaponAmmoText.text = $"{current}/{total}";
            }
        }

        private void TryRebindWeaponOnEnable()
        {
            weaponSlotIndexCache = MapWeaponSlotIndex();
            if (weaponSlotIndexCache == null) return;

            // 优先使用当前已缓存的实例
            if (currentItemInstance != null)
            {
                BindWeaponIfPresent(currentItemInstance);
                UpdateWeaponAmmoText();
                return;
            }

            // 若当前实例不可用，尝试从玩家控制器读取当前槽位武器
            var player = FindObjectOfType<PlayerWeaponEquipController>();
            if (player != null && weaponSlotIndexCache.Value >= 0 && weaponSlotIndexCache.Value < player.equipped.Length)
            {
                var equippedGO = player.equipped[weaponSlotIndexCache.Value];
                if (equippedGO != null)
                {
                    BindWeaponIfPresent(equippedGO);
                    UpdateWeaponAmmoText();
                }
                else
                {
                    if (weaponAmmoText != null) weaponAmmoText.text = string.Empty;
                }
            }
        }

        /// <summary>
        /// 查找场景中对应的装备实例
        /// </summary>
        private ItemDataReader FindEquippedItemInstance()
        {
            if (currentEquippedItem == null) return null;

            // 根据装备信息查找对应的装备实例
            var allItemReaders = FindObjectsOfType<ItemDataReader>(true);
            foreach (var reader in allItemReaders)
            {
                if (reader.ItemData != null &&
                    reader.ItemData.GlobalId == currentEquippedItem.ItemData.GlobalId &&
                    reader.ItemData.itemName == currentEquippedItem.ItemData.itemName)
                {
                    return reader;
                }
            }

            return null;
        }

        /// <summary>
        /// 刷新装备显示
        /// </summary>
        private System.Collections.IEnumerator RefreshEquipmentDisplay()
        {
            yield return new WaitForEndOfFrame();

            if (currentItemInstance != null && currentEquippedItem != null)
            {
                LogDebugInfo($"刷新装备 {currentEquippedItem.ItemData.itemName} 的显示");

                // 确保装备尺寸正确
                if (isActiveAndEnabled)
                {
                    yield return StartCoroutine(EnsureItemSizeAfterFrame());
                }

                // 如果是容器类装备，触发容器网格显示
                if (IsContainerType(currentEquippedItem.ItemData))
                {
                    if (containerGrid != null)
                    {
                        LogDebugInfo($"刷新容器 {currentEquippedItem.ItemData.itemName} 的网格显示");
                        containerGrid.gameObject.SetActive(true);

                        // 触发容器内容加载
                        LoadContainerContent();
                    }
                }

                LogDebugInfo($"装备 {currentEquippedItem.ItemData.itemName} 显示刷新完成");
            }
        }

        /// <summary>
        /// 检查是否为容器类装备
        /// </summary>
        private bool IsContainerType(ItemDataSO itemData)
        {
            if (itemData == null) return false;

            return itemData.category == ItemCategory.Backpack ||
                   itemData.category == ItemCategory.TacticalRig;
        }

        #endregion
    }
}
