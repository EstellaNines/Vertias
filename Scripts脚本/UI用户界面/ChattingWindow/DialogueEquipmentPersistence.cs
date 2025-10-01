using System;
using System.Collections;
using UnityEngine;
using InventorySystem;

namespace DialogueSystem
{
    /// <summary>
    /// 对话界面中装备栏的持久化管理器
    /// 负责管理背包和挂具装备槽及其容器网格的保存与加载
    /// </summary>
    public class DialogueEquipmentPersistence : MonoBehaviour
    {
        [Header("装备栏组件引用")]
        [FieldLabel("背包装备槽")]
        [Tooltip("背包装备槽预制件或场景中的实例")]
        [SerializeField] private EquipmentSlot backpackSlot;

        [FieldLabel("挂具装备槽")]
        [Tooltip("战术挂具装备槽预制件或场景中的实例")]
        [SerializeField] private EquipmentSlot tacticalRigSlot;

        [Header("预制件引用（可选）")]
        [FieldLabel("背包装备槽预制件")]
        [Tooltip("如果需要动态创建装备槽，提供预制件引用")]
        [SerializeField] private GameObject backpackSlotPrefab;

        [FieldLabel("挂具装备槽预制件")]
        [Tooltip("如果需要动态创建装备槽，提供预制件引用")]
        [SerializeField] private GameObject tacticalRigSlotPrefab;

        [Header("容器父级")]
        [FieldLabel("MidPattern父级")]
        [Tooltip("装备栏和容器网格的父级Transform")]
        [SerializeField] private Transform midPatternParent;

        [Header("保存设置")]
        [FieldLabel("自动保存")]
        [Tooltip("是否在装备变化时自动保存")]
        [SerializeField] private bool enableAutoSave = true;

        [Header("调试")]
        [FieldLabel("显示日志")]
        [SerializeField] private bool showDebugLog = true;


        // 装备槽管理器引用
        private EquipmentSlotManager equipmentManager;

        #region Unity生命周期

        private void Awake()
        {
            InitializeEquipmentManager();
            ValidateReferences();
        }

		private void Start()
		{
			// 延迟一帧初始化，确保其他系统已准备好
			StartCoroutine(DelayedInitialization());
		}

        private void OnEnable()
        {
            // 订阅装备变化事件
            if (enableAutoSave)
            {
                EquipmentSlot.OnItemEquipped += OnEquipmentChanged;
                EquipmentSlot.OnItemUnequipped += OnEquipmentChanged;
                EquipmentSlot.OnContainerSlotActivated += OnContainerActivated;
                EquipmentSlot.OnContainerSlotDeactivated += OnContainerDeactivated;
            }

			// 对话系统激活时：加载装备并开启持续保存
			StartCoroutine(LoadEquipmentWithRetry());
			StartActiveAutoSave();
        }

        private void OnDisable()
        {
            // 保存当前状态
            if (enableAutoSave)
            {
                SaveAllEquipment();
            }

			// 停止持续保存
			StopActiveAutoSave();

            // 取消事件订阅
            EquipmentSlot.OnItemEquipped -= OnEquipmentChanged;
            EquipmentSlot.OnItemUnequipped -= OnEquipmentChanged;
            EquipmentSlot.OnContainerSlotActivated -= OnContainerActivated;
            EquipmentSlot.OnContainerSlotDeactivated -= OnContainerDeactivated;
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化装备管理器
        /// </summary>
        private void InitializeEquipmentManager()
        {
            equipmentManager = EquipmentSlotManager.Instance;
            if (equipmentManager == null)
            {
                Debug.LogWarning("[DialogueEquipmentPersistence] 未找到装备槽管理器");
            }
        }

        /// <summary>
        /// 验证组件引用
        /// </summary>
        private void ValidateReferences()
        {
            bool hasErrors = false;

            if (midPatternParent == null)
            {
                Debug.LogError("[DialogueEquipmentPersistence] 未设置MidPattern父级！", this);
                hasErrors = true;
            }

            // 检查装备槽引用
            if (backpackSlot == null && backpackSlotPrefab == null)
            {
                Debug.LogWarning("[DialogueEquipmentPersistence] 背包装备槽和预制件都未设置");
            }

            if (tacticalRigSlot == null && tacticalRigSlotPrefab == null)
            {
                Debug.LogWarning("[DialogueEquipmentPersistence] 挂具装备槽和预制件都未设置");
            }

            if (hasErrors)
            {
                enabled = false;
            }
        }

        /// <summary>
        /// 延迟初始化
        /// </summary>
        private IEnumerator DelayedInitialization()
        {
            yield return new WaitForEndOfFrame();

            // 如果需要，动态创建装备槽
            CreateEquipmentSlotsIfNeeded();

            // 注册装备槽到管理器
            RegisterEquipmentSlots();

			// 加载保存的装备（带初始化与重试）
			StartCoroutine(LoadEquipmentWithRetry());

            LogDebug("对话装备持久化系统初始化完成");
        }

        /// <summary>
        /// 动态创建装备槽（如果需要）
        /// </summary>
        private void CreateEquipmentSlotsIfNeeded()
        {
            if (backpackSlot == null && backpackSlotPrefab != null && midPatternParent != null)
            {
                GameObject backpackObj = Instantiate(backpackSlotPrefab, midPatternParent);
                backpackSlot = backpackObj.GetComponent<EquipmentSlot>();
                LogDebug("动态创建背包装备槽");
            }

            if (tacticalRigSlot == null && tacticalRigSlotPrefab != null && midPatternParent != null)
            {
                GameObject rigObj = Instantiate(tacticalRigSlotPrefab, midPatternParent);
                tacticalRigSlot = rigObj.GetComponent<EquipmentSlot>();
                LogDebug("动态创建挂具装备槽");
            }
        }

        /// <summary>
        /// 注册装备槽到管理器
        /// </summary>
        private void RegisterEquipmentSlots()
        {
            if (equipmentManager == null) return;

            // 这些装备槽会自动通过EquipmentSlotManager的Awake注册
            // 这里只是确认注册状态
            if (backpackSlot != null)
            {
                LogDebug($"背包装备槽已准备: {backpackSlot.SlotName}");
            }

            if (tacticalRigSlot != null)
            {
                LogDebug($"挂具装备槽已准备: {tacticalRigSlot.SlotName}");
            }
        }

        #endregion

        #region 保存功能

        /// <summary>
        /// 保存所有装备
        /// </summary>
        public void SaveAllEquipment()
        {
            // 保存装备（背包/挂具等）到 EquipmentSave.es3
            var equipMgr = EquipmentPersistenceManager.Instance;
            if (equipMgr != null)
            {
				equipMgr.SaveEquipmentData(); // 使用带冷却的保存以避免高频IO
                LogDebug("已触发装备存档 (EquipmentSave.es3)");
            }

            // 容器内容由 ContainerSaveManager 通过网格事件自动保存到 ContainerData.es3
        }

        /// <summary>
        /// 保存背包装备
        /// </summary>
        private void SaveBackpackEquipment()
        {
            // 使用全局存档，无需单独保存
        }

        /// <summary>
        /// 保存挂具装备
        /// </summary>
        private void SaveTacticalRigEquipment()
        {
            // 使用全局存档，无需单独保存
        }

        /// <summary>
        /// 保存容器内容
        /// </summary>
        private void SaveContainerContent(ItemGrid containerGrid, string fileName)
        {
            // 容器内容由全局存档统一管理
        }

        #endregion

        #region 加载功能

        /// <summary>
        /// 加载所有装备
        /// </summary>
        public void LoadAllEquipment()
        {
            // 从 EquipmentSave.es3 加载装备；容器由 EquipmentPersistenceManager 调用 ContainerSaveManager 恢复
            var equipMgr = EquipmentPersistenceManager.Instance;
            if (equipMgr != null)
            {
                equipMgr.LoadEquipmentData();
                StartCoroutine(EnsureContainerGridsAfterLoad());
                LogDebug("已触发装备加载 (EquipmentSave.es3)");
            }
        }

		/// <summary>
		/// 确保管理器与槽位就绪后再加载装备，并兼容按需加载路径
		/// </summary>
		private IEnumerator LoadEquipmentWithRetry()
		{
			// 等待管理器实例与槽位准备就绪
			int retries = 60; // 最多约1秒（60帧）
			var equipMgr = EquipmentPersistenceManager.Instance;
			while (retries-- > 0)
			{
				if (equipMgr != null && (equipMgr.IsInitialized || EquipmentSlotManager.Instance != null)
					&& (backpackSlot != null || backpackSlotPrefab != null)
					&& (tacticalRigSlot != null || tacticalRigSlotPrefab != null))
				{
					break;
				}
				yield return null;
				equipMgr = EquipmentPersistenceManager.Instance;
			}

			if (equipMgr == null)
				yield break;

			// 解除可能的启动期抑制，保证能保存/加载
			equipMgr.EnsureSaveNotSuppressed();

			// 优先走“按需加载”路径：模拟背包打开事件
			equipMgr.OnBackpackOpened();

			// 如果仍未触发加载且确有存档，则直接触发加载
			if (equipMgr.HasSavedData())
			{
				equipMgr.LoadEquipmentData();
			}

			// 等待恢复完成或超时
			bool restored = false;
			System.Action onRestored = () => restored = true;
			EquipmentPersistenceManager.OnEquipmentRestored += onRestored;
			float timeout = 3f; float elapsed = 0f;
			while (!restored && elapsed < timeout)
			{
				if (!equipMgr.IsLoading && equipMgr.HasSavedData() == false) break;
				elapsed += Time.deltaTime;
				yield return null;
			}
			EquipmentPersistenceManager.OnEquipmentRestored -= onRestored;

			// 确保容器网格激活
			yield return StartCoroutine(EnsureContainerGridsAfterLoad());
		}

        /// <summary>
        /// 加载背包装备
        /// </summary>
        private void LoadBackpackEquipment()
        {
            // 由全局加载处理
        }

        /// <summary>
        /// 加载挂具装备
        /// </summary>
        private void LoadTacticalRigEquipment()
        {
            // 由全局加载处理
        }

        /// <summary>
        /// 恢复装备物品
        /// </summary>
        private IEnumerator RestoreEquipment(EquipmentSlot slot, DialogueEquipmentSaveData saveData, string containerFileName)
        {
            // 全局加载负责恢复，这里不再单独实现
            yield break;
        }

        /// <summary>
        /// 加载容器内容
        /// </summary>
        private void LoadContainerContent(ItemGrid containerGrid, string fileName)
        {
            // 容器内容由全局存档统一管理
        }

        /// <summary>
        /// 全局加载后，确保背包/挂具的容器网格被激活
        /// </summary>
        private IEnumerator EnsureContainerGridsAfterLoad()
        {
            yield return null; // 等一帧等待装备恢复完成
            if (backpackSlot != null && backpackSlot.HasEquippedItem)
            {
                backpackSlot.ForceActivateContainerGrid();
            }
            if (tacticalRigSlot != null && tacticalRigSlot.HasEquippedItem)
            {
                tacticalRigSlot.ForceActivateContainerGrid();
            }
        }

		#region 持续保存（对话激活期间）

		[Header("持续保存设置")]
		[FieldLabel("对话激活时自动保存间隔(秒)")]
		[SerializeField] private float activeAutoSaveInterval = 3f;
		private Coroutine activeAutoSaveCo;

		private void StartActiveAutoSave()
		{
			if (!enableAutoSave) return;
			StopActiveAutoSave();
			activeAutoSaveCo = StartCoroutine(ActiveAutoSaveLoop());
		}

		private void StopActiveAutoSave()
		{
			if (activeAutoSaveCo != null)
			{
				StopCoroutine(activeAutoSaveCo);
				activeAutoSaveCo = null;
			}
		}

		private IEnumerator ActiveAutoSaveLoop()
		{
			var equipMgr = EquipmentPersistenceManager.Instance;
			while (isActiveAndEnabled)
			{
				if (equipMgr != null && !equipMgr.IsLoading)
				{
					SaveAllEquipment();
				}
				yield return new WaitForSeconds(Mathf.Max(1f, activeAutoSaveInterval));
			}
		}

		#endregion

        #endregion

        #region 辅助方法

        /// <summary>
        /// 根据ID获取物品数据
        /// </summary>
        private ItemDataSO GetItemDataByID(string itemID)
        {
            if (!int.TryParse(itemID, out int id))
            {
                Debug.LogWarning($"[DialogueEquipmentPersistence] 无效的物品ID: {itemID}");
                return null;
            }

            // 从Resources加载所有ItemDataSO
            ItemDataSO[] allItems = Resources.LoadAll<ItemDataSO>("InventorySystemResources/ItemScriptableObject");
            foreach (var item in allItems)
            {
                if (item != null && item.id == id)
                {
                    return item;
                }
            }

            Debug.LogWarning($"[DialogueEquipmentPersistence] 未找到ID为 {itemID} 的物品");
            return null;
        }

        /// <summary>
        /// 创建物品实例
        /// </summary>
        private GameObject CreateItemInstance(ItemDataSO itemData)
        {
            if (itemData == null) return null;

            try
            {
                // 根据类别和ID加载预制件
                string categoryFolder = GetCategoryFolderName(itemData.category);
                string prefabPath = $"InventorySystemResources/Prefabs/{categoryFolder}/{itemData.id}";

                GameObject prefab = Resources.Load<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[DialogueEquipmentPersistence] 未找到预制件: {prefabPath}");
                    return null;
                }

                GameObject instance = Instantiate(prefab);
                
                // 设置物品数据
                ItemDataReader reader = instance.GetComponent<ItemDataReader>();
                if (reader != null)
                {
                    reader.SetItemData(itemData);
                }

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueEquipmentPersistence] 创建物品实例失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取类别文件夹名称
        /// </summary>
        private string GetCategoryFolderName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Backpack: return "Backpack_背包";
                case ItemCategory.TacticalRig: return "TacticalRig_战术背心";
                case ItemCategory.Helmet: return "Helmet_头盔";
                case ItemCategory.Armor: return "Armor_护甲";
                case ItemCategory.Weapon: return "Weapon_武器";
                case ItemCategory.Ammunition: return "Ammunition_弹药";
                case ItemCategory.Food: return "Food_食物";
                case ItemCategory.Drink: return "Drink_饮料";
                case ItemCategory.Sedative: return "Sedative_镇静剂";
                case ItemCategory.Hemostatic: return "Hemostatic_止血剂";
                case ItemCategory.Healing: return "Healing_治疗药物";
                case ItemCategory.Intelligence: return "Intelligence_情报";
                case ItemCategory.Currency: return "Currency_货币";
                case ItemCategory.Special: return "Special";
                default: return "Helmet_头盔";
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 装备变化事件处理
        /// </summary>
        private void OnEquipmentChanged(EquipmentSlotType slotType, ItemDataReader item)
        {
            // 只处理背包和挂具的变化
            if (slotType != EquipmentSlotType.Backpack && slotType != EquipmentSlotType.TacticalRig)
                return;

            if (!enableAutoSave) return;

            LogDebug($"装备变化: {slotType} -> {item?.ItemData?.itemName ?? "空"}");

            // 延迟保存，避免频繁IO
            CancelInvoke(nameof(DelayedSave));
            Invoke(nameof(DelayedSave), 0.5f);
        }

        /// <summary>
        /// 容器激活事件处理
        /// </summary>
        private void OnContainerActivated(EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (slotType != EquipmentSlotType.Backpack && slotType != EquipmentSlotType.TacticalRig)
                return;

            LogDebug($"容器激活: {slotType} -> {containerGrid?.GridName}");
        }

        /// <summary>
        /// 容器停用事件处理
        /// </summary>
        private void OnContainerDeactivated(EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (slotType != EquipmentSlotType.Backpack && slotType != EquipmentSlotType.TacticalRig)
                return;

            LogDebug($"容器停用: {slotType} -> {containerGrid?.GridName}");

            // 容器停用时立即保存
            if (enableAutoSave)
            {
                DelayedSave();
            }
        }

        /// <summary>
        /// 延迟保存
        /// </summary>
        private void DelayedSave()
        {
            SaveAllEquipment();
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 清除所有保存的装备数据
        /// </summary>
        public void ClearAllSaveData()
        {
            try
            {
                var equipMgr = EquipmentPersistenceManager.Instance;
                if (equipMgr != null)
                {
                    equipMgr.ClearAllSaveData();
                    LogDebug("已清除装备存档 (EquipmentSave.es3)");
                }

                var containerMgr = ContainerSaveManager.Instance;
                if (containerMgr != null)
                {
                    containerMgr.ClearAllContainerData();
                    LogDebug("已清除容器存档 (ContainerData.es3)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueEquipmentPersistence] 清除存档失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取背包装备槽
        /// </summary>
        public EquipmentSlot GetBackpackSlot() => backpackSlot;

        /// <summary>
        /// 获取挂具装备槽
        /// </summary>
        public EquipmentSlot GetTacticalRigSlot() => tacticalRigSlot;

        #endregion

        #region 调试

        private void LogDebug(string message)
        {
            if (showDebugLog)
            {
                Debug.Log($"[DialogueEquipmentPersistence] {message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 对话装备保存数据结构
    /// </summary>
    [Serializable]
    public class DialogueEquipmentSaveData
    {
        public EquipmentSlotType slotType;
        public bool hasEquipment;
        public string equippedItemID;
        public string equippedItemName;
        public bool hasContainer;
        public string timestamp;
    }
}
