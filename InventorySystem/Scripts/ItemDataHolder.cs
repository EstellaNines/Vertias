using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 物品数据持有者，用于物品预制体上，持有 InventorySystemItemDataSO 数据
// 支持实例数据存储和动态属性保存
[System.Serializable]
public class ItemInstanceData
{
	// 动态属性数据
	public float currentDurability = 100f; // 当前耐久度
	public int currentStackCount = 1; // 当前堆叠数量
	public float currentHealAmount = 0f; // 当前治疗量
	public int currentUsageCount = 0; // 当前使用次数
	public bool isModified = false; // 是否被修改过
	public Dictionary<string, object> customProperties = new Dictionary<string, object>(); // 自定义属性

	// 构造函数
	public ItemInstanceData()
	{
		customProperties = new Dictionary<string, object>();
	}

	// 从ScriptableObject初始化实例数据
	public void InitializeFromItemData(InventorySystemItemDataSO itemData)
	{
		if (itemData == null) return;

		currentDurability = itemData.durability;
		currentStackCount = 1; // 默认堆叠数量为1
		currentHealAmount = itemData.maxHealAmount;
		currentUsageCount = 0;
		isModified = false;
	}

	// 验证数据有效性
	public bool ValidateData(InventorySystemItemDataSO itemData)
	{
		if (itemData == null) return false;

		// 验证耐久度范围
		if (currentDurability < 0 || currentDurability > itemData.durability)
		{
			currentDurability = Mathf.Clamp(currentDurability, 0, itemData.durability);
			isModified = true;
		}

		// 验证堆叠数量
		if (currentStackCount < 1 || currentStackCount > itemData.maxStack)
		{
			currentStackCount = Mathf.Clamp(currentStackCount, 1, itemData.maxStack);
			isModified = true;
		}

		// 验证治疗量
		if (currentHealAmount < 0 || currentHealAmount > itemData.maxHealAmount)
		{
			currentHealAmount = Mathf.Clamp(currentHealAmount, 0, itemData.maxHealAmount);
			isModified = true;
		}

		return true;
	}

	// 重置为默认值
	public void ResetToDefault(InventorySystemItemDataSO itemData)
	{
		InitializeFromItemData(itemData);
		customProperties.Clear();
	}
}

public class ItemDataHolder : MonoBehaviour
{
	[Header("物品数据")]
	[SerializeField] private InventorySystemItemDataSO itemData;

	[Header("实例数据")]
	[SerializeField] private ItemInstanceData instanceData = new ItemInstanceData();

	[Header("UI 组件引用")]
	[SerializeField] private Image itemIconImage;
	[SerializeField] private RawImage backgroundImage;


	private void Awake()
	{
		// 初始化实例数据
		if (instanceData == null)
		{
			instanceData = new ItemInstanceData();
		}

		// 如果有数据，自动刷新 UI 显示
		if (itemData != null)
		{
			// 初始化实例数据
			instanceData.InitializeFromItemData(itemData);
			// 验证数据有效性
			instanceData.ValidateData(itemData);
			UpdateItemDisplay();
		}
	}

	// 获取物品数据
	public InventorySystemItemDataSO GetItemData()
	{
		return itemData;
	}

	// 设置物品数据
	// data 需要为 InventorySystemItemDataSO
	public void SetItemData(InventorySystemItemDataSO data)
	{
		itemData = data;
		// 重新初始化实例数据
		if (instanceData == null)
		{
			instanceData = new ItemInstanceData();
		}
		instanceData.InitializeFromItemData(data);
		UpdateItemDisplay();
	}

	// === 实例数据管理方法 ===

	// 获取实例数据
	public ItemInstanceData GetInstanceData()
	{
		return instanceData;
	}

	// 设置实例数据
	public void SetInstanceData(ItemInstanceData data)
	{
		if (data != null)
		{
			instanceData = data;
			// 验证数据有效性
			instanceData.ValidateData(itemData);
		}
	}

	// 获取当前耐久度
	public float GetCurrentDurability()
	{
		return instanceData?.currentDurability ?? (itemData?.durability ?? 100f);
	}

	// 设置当前耐久度
	public void SetCurrentDurability(float durability)
	{
		if (instanceData != null)
		{
			instanceData.currentDurability = durability;
			instanceData.isModified = true;
			// 验证数据有效性
			instanceData.ValidateData(itemData);
		}
	}

	// 获取当前堆叠数量
	public int GetCurrentStackCount()
	{
		return instanceData?.currentStackCount ?? 1;
	}

	// 设置当前堆叠数量
	public void SetCurrentStackCount(int count)
	{
		if (instanceData != null)
		{
			instanceData.currentStackCount = count;
			instanceData.isModified = true;
			// 验证数据有效性
			instanceData.ValidateData(itemData);
		}
	}

	// 获取当前治疗量
	public float GetCurrentHealAmount()
	{
		return instanceData?.currentHealAmount ?? (itemData?.maxHealAmount ?? 0f);
	}

	// 设置当前治疗量
	public void SetCurrentHealAmount(float amount)
	{
		if (instanceData != null)
		{
			instanceData.currentHealAmount = amount;
			instanceData.isModified = true;
			// 验证数据有效性
			instanceData.ValidateData(itemData);
		}
	}

	// 获取使用次数
	public int GetCurrentUsageCount()
	{
		return instanceData?.currentUsageCount ?? 0;
	}

	// 增加使用次数
	public void IncrementUsageCount()
	{
		if (instanceData != null)
		{
			instanceData.currentUsageCount++;
			instanceData.isModified = true;
		}
	}

	// 检查物品是否被修改过
	public bool IsModified()
	{
		return instanceData?.isModified ?? false;
	}

	// 设置自定义属性
	public void SetCustomProperty(string key, object value)
	{
		if (instanceData != null && !string.IsNullOrEmpty(key))
		{
			instanceData.customProperties[key] = value;
			instanceData.isModified = true;
		}
	}

	// 获取自定义属性
	public T GetCustomProperty<T>(string key, T defaultValue = default(T))
	{
		if (instanceData != null && !string.IsNullOrEmpty(key) && instanceData.customProperties.ContainsKey(key))
		{
			try
			{
				return (T)instanceData.customProperties[key];
			}
			catch
			{
				return defaultValue;
			}
		}
		return defaultValue;
	}

	// 验证并修复实例数据
	public bool ValidateAndRepairInstanceData()
	{
		if (instanceData == null)
		{
			instanceData = new ItemInstanceData();
			if (itemData != null)
			{
				instanceData.InitializeFromItemData(itemData);
			}
			return true;
		}

		return instanceData.ValidateData(itemData);
	}

	// 重置实例数据为默认值
	public void ResetInstanceData()
	{
		if (instanceData != null && itemData != null)
		{
			instanceData.ResetToDefault(itemData);
		}
	}

	// === 序列化支持方法 ===

	// 序列化实例数据为JSON字符串
	public string SerializeInstanceData()
	{
		if (instanceData == null) return "";

		try
		{
			return JsonUtility.ToJson(instanceData);
		}
		catch (System.Exception e)
		{
			Debug.LogError($"序列化实例数据失败: {e.Message}");
			return "";
		}
	}

	// 从JSON字符串反序列化实例数据
	public bool DeserializeInstanceData(string jsonData)
	{
		if (string.IsNullOrEmpty(jsonData)) return false;

		try
		{
			ItemInstanceData deserializedData = JsonUtility.FromJson<ItemInstanceData>(jsonData);
			if (deserializedData != null)
			{
				instanceData = deserializedData;
				// 重新初始化字典（JsonUtility不支持字典序列化）
				if (instanceData.customProperties == null)
				{
					instanceData.customProperties = new Dictionary<string, object>();
				}
				// 验证数据有效性
				instanceData.ValidateData(itemData);
				return true;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError($"反序列化实例数据失败: {e.Message}");
		}

		return false;
	}

	// 刷新物品 UI 显示
	public void UpdateItemDisplay()
	{
		if (itemData == null) return;

		// 更新物品图标
		if (itemIconImage != null && itemData.itemIcon != null)
		{
			itemIconImage.sprite = itemData.itemIcon;
		}

		// 更新背景颜色
		if (backgroundImage != null)
		{
			backgroundImage.color = GetColorFromString(itemData.backgroundColor);
		}

		// 更新预制体尺寸
		UpdatePrefabSize();
	}

	// 根据物品尺寸更新预制体大小
	private void UpdatePrefabSize()
	{
		if (itemData == null) return;

		// 每格 64 像素，与系统统一
		float cellSize = 64f; // 从 80f 调整为 64f
		Vector2 newSize = new Vector2(
			itemData.width * cellSize,
			itemData.height * cellSize
		);

		// 更新预制体 RectTransform 尺寸
		RectTransform rectTransform = GetComponent<RectTransform>();
		if (rectTransform != null)
		{
			rectTransform.sizeDelta = newSize;
		}

		// 更新物品图标尺寸
		if (itemIconImage != null)
		{
			RectTransform iconRect = itemIconImage.GetComponent<RectTransform>();
			if (iconRect != null)
			{
				iconRect.sizeDelta = newSize;
			}
		}

		// 更新背景图片尺寸
		if (backgroundImage != null)
		{
			RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
			if (bgRect != null)
			{
				bgRect.sizeDelta = newSize;
			}
		}
	}

	// 将颜色名称转为 Color
	// colorName：颜色名
	// 返回：对应的 Color 值
	private Color GetColorFromString(string colorName)
	{
		Color baseColor;
		switch (colorName?.ToLower())
		{
			case "blue":
				baseColor = HexToColor("#2d3c4b"); // 蓝色
				break;
			case "violet":
			case "purple":
				baseColor = HexToColor("#583b80"); // 紫色
				break;
			case "yellow":
				baseColor = HexToColor("#80550d"); // 黄色
				break;
			case "red":
				baseColor = HexToColor("#350000"); // 红色
				break;
			default:
				baseColor = Color.white; // 默认白色
				break;
		}

		// 设置透明度为 225
		baseColor.a = 225f / 255f; // 透明度
		return baseColor;
	}

	// 十六进制颜色转为 Color
	// hex：十六进制颜色值
	// 返回：Color 实例
	private Color HexToColor(string hex)
	{
		// 移除 #
		if (hex.StartsWith("#"))
			hex = hex.Substring(1);

		// 确保为 6 位十六进制
		if (hex.Length != 6)
			return new Color(1f, 1f, 1f, 225f / 255f); // 默认白色，透明度 225

		try
		{
			byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
			byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
			byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);

			return new Color32(r, g, b, 225); // 透明度设为 225
		}
		catch
		{
			return new Color(1f, 1f, 1f, 225f / 255f); // 转换失败时返回白色，透明度 225
		}
	}

	// === 数据访问器 ===

	// 物品 ID
	public int ItemID => itemData?.id ?? 0;

	// 物品名称
	public string ItemName => itemData?.itemName ?? "";

	// 物品高度
	public int ItemHeight => itemData?.height ?? 1;

	// 物品宽度
	public int ItemWidth => itemData?.width ?? 1;

	// 物品稀有度
	public string ItemRarity => itemData?.rarity ?? "";

	// 物品类别
	public string ItemCategory => itemData?.category ?? "";

	// 弹药类型
	public string BulletType => itemData?.BulletType ?? "";

	// 容量高度（背包/战术背心）
	public int CellH => itemData?.CellH ?? 0;

	// 容量宽度（背包/战术背心）
	public int CellV => itemData?.CellV ?? 0;

	// 背景颜色名
	public string BackgroundColor => itemData?.backgroundColor ?? "";

	// 物品图标
	public Sprite ItemIcon => itemData?.itemIcon;

	// === 实用方法 ===

	// 是否为背包类物品
	// 返回：是否为背包
	public bool IsBackpack()
	{
		return itemData != null && itemData.category.ToLower().Contains("backpack");
	}

	// 是否为武器类物品
	// 返回：是否为武器
	public bool IsWeapon()
	{
		return itemData != null && itemData.category.ToLower().Contains("weapon");
	}

	// 是否为弹药类物品
	// 返回：是否为弹药
	public bool IsAmmunition()
	{
		return itemData != null && itemData.category.ToLower().Contains("ammunition");
	}

	// 是否具有内部容量（背包/战术背心）
	// 返回：是否具有容量
	public bool HasCapacity()
	{
		return itemData != null && (itemData.CellH > 0 || itemData.CellV > 0);
	}

	// 获取物品总容量
	// 返回：容量（横×纵）
	public int GetTotalCapacity()
	{
		return itemData != null ? itemData.CellH * itemData.CellV : 0;
	}

	// 获取物品占用的格子数
	// 返回：占用格数
	public int GetOccupiedSlots()
	{
		return itemData != null ? itemData.height * itemData.width : 1;
	}



	// 调试：打印物品信息
	[ContextMenu("打印物品信息")]
	public void LogItemInfo()
	{
		if (itemData == null)
		{
			Debug.Log("物品数据为空");
			return;
		}

		string instanceInfo = "";
		if (instanceData != null)
		{
			instanceInfo = $"\n=== 实例数据 ===\n" +
						   $"当前耐久度: {instanceData.currentDurability}/{itemData.durability}\n" +
						   $"当前堆叠数量: {instanceData.currentStackCount}\n" +
						   $"当前治疗量: {instanceData.currentHealAmount}\n" +
						   $"使用次数: {instanceData.currentUsageCount}\n" +
						   $"是否被修改: {instanceData.isModified}\n" +
						   $"自定义属性数量: {instanceData.customProperties.Count}";
		}

		Debug.Log($"物品信息:\n" +
				  $"ID: {itemData.id}\n" +
				  $"名称: {itemData.itemName}\n" +
				  $"尺寸: {itemData.width}x{itemData.height}\n" +
				  $"稀有度: {itemData.rarity}\n" +
				  $"类别: {itemData.category}\n" +
				  $"背景颜色: {itemData.backgroundColor}\n" +
				  $"容量: {itemData.CellH}x{itemData.CellV}\n" +
				  $"弹药类型: {itemData.BulletType}" +
				  instanceInfo);
	}

	// 测试：验证实例数据
	[ContextMenu("验证实例数据")]
	public void ValidateInstanceDataTest()
	{
		bool isValid = ValidateAndRepairInstanceData();
		Debug.Log($"实例数据验证结果: {(isValid ? "有效" : "无效并已修复")}");
		LogItemInfo();
	}

	// 测试：重置实例数据
	[ContextMenu("重置实例数据")]
	public void ResetInstanceDataTest()
	{
		ResetInstanceData();
		Debug.Log("实例数据已重置为默认值");
		LogItemInfo();
	}

	// ===== ISaveable接口实现 =====

	/// <summary>
	/// 物品数据持有者保存数据类
	/// </summary>
	[System.Serializable]
	public class ItemDataHolderSaveData
	{
		public string itemDataPath; // 物品数据资源路径
		public string instanceDataJson; // 实例数据JSON字符串
		public bool isModified; // 是否被修改过
		public float lastModifiedTime; // 最后修改时间
		public Vector3 worldPosition; // 世界坐标位置
		public Vector3 worldRotation; // 世界坐标旋转
		public bool isActive; // 是否激活状态
	}

	// ISaveable接口实现
	private string saveID = "";

	/// <summary>
	/// 获取保存ID
	/// </summary>
	public string GetSaveID()
	{
		if (string.IsNullOrEmpty(saveID))
		{
			GenerateNewSaveID();
		}
		return saveID;
	}

	/// <summary>
	/// 设置保存ID
	/// </summary>
	public void SetSaveID(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			saveID = id;
		}
	}

	/// <summary>
	/// 生成新的保存ID
	/// 格式: ItemData_[物品名称]_[8位GUID]_[实例ID]
	/// </summary>
	public void GenerateNewSaveID()
	{
		string itemName = itemData != null ? itemData.itemName.Replace(" ", "").Replace("(", "").Replace(")", "") : "Unknown";
		string guidPart = System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
		int instanceID = GetInstanceID();
		saveID = $"ItemData_{itemName}_{guidPart}_{instanceID}";

		if (Application.isPlaying)
		{
			Debug.Log($"为物品数据持有者生成新的保存ID: {saveID}");
		}
	}

	/// <summary>
	/// 获取保存数据
	/// </summary>
	public object GetSaveData()
	{
		ItemDataHolderSaveData saveData = new ItemDataHolderSaveData();

		// 保存物品数据路径
		if (itemData != null)
		{
#if UNITY_EDITOR
			saveData.itemDataPath = UnityEditor.AssetDatabase.GetAssetPath(itemData);
#else
			saveData.itemDataPath = itemData.name; // 运行时使用名称
#endif
		}

		// 保存实例数据
		saveData.instanceDataJson = SerializeInstanceData();
		saveData.isModified = IsModified();
		saveData.lastModifiedTime = Time.time;

		// 保存世界坐标信息
		if (transform != null)
		{
			saveData.worldPosition = transform.position;
			saveData.worldRotation = transform.eulerAngles;
		}

		saveData.isActive = gameObject.activeInHierarchy;

		return saveData;
	}

	/// <summary>
	/// 加载保存数据
	/// </summary>
	public void LoadSaveData(object data)
	{
		if (data is ItemDataHolderSaveData saveData)
		{
			try
			{
				// 恢复物品数据引用
				if (!string.IsNullOrEmpty(saveData.itemDataPath))
				{
					RestoreItemDataReference(saveData.itemDataPath);
				}

				// 恢复实例数据
				if (!string.IsNullOrEmpty(saveData.instanceDataJson))
				{
					DeserializeInstanceData(saveData.instanceDataJson);
				}

				// 恢复世界坐标
				if (transform != null)
				{
					transform.position = saveData.worldPosition;
					transform.eulerAngles = saveData.worldRotation;
				}

				// 恢复激活状态
				gameObject.SetActive(saveData.isActive);

				// 更新显示
				UpdateItemDisplay();

				Debug.Log($"成功加载物品数据持有者保存数据: {GetSaveID()}");
			}
			catch (System.Exception e)
			{
				Debug.LogError($"加载物品数据持有者保存数据失败: {e.Message}");
			}
		}
		else
		{
			Debug.LogError("无效的物品数据持有者保存数据格式");
		}
	}

	/// <summary>
	/// 恢复物品数据引用
	/// </summary>
	private void RestoreItemDataReference(string assetPath)
	{
		if (string.IsNullOrEmpty(assetPath)) return;

		try
		{
#if UNITY_EDITOR
			// 编辑器模式下通过路径加载
			InventorySystemItemDataSO loadedData = UnityEditor.AssetDatabase.LoadAssetAtPath<InventorySystemItemDataSO>(assetPath);
			if (loadedData != null)
			{
				SetItemData(loadedData);
				Debug.Log($"成功恢复物品数据引用: {loadedData.itemName}");
			}
			else
			{
				Debug.LogWarning($"无法从路径加载物品数据: {assetPath}");
			}
#else
			// 运行时通过名称在Resources中查找
			InventorySystemItemDataSO[] allItems = Resources.LoadAll<InventorySystemItemDataSO>("");
			foreach (var item in allItems)
			{
				if (item.name == assetPath || item.itemName == assetPath)
				{
					SetItemData(item);
					Debug.Log($"成功恢复物品数据引用: {item.itemName}");
					return;
				}
			}
			Debug.LogWarning($"无法在Resources中找到物品数据: {assetPath}");
#endif
		}
		catch (System.Exception e)
		{
			Debug.LogError($"恢复物品数据引用失败: {e.Message}");
		}
	}

	/// <summary>
	/// 验证保存数据
	/// </summary>
	public bool ValidateData()
	{
		bool isValid = true;

		// 验证保存ID
		if (string.IsNullOrEmpty(saveID))
		{
			GenerateNewSaveID();
			isValid = false;
			Debug.LogWarning("物品数据持有者保存ID为空，已重新生成");
		}

		// 验证物品数据引用
		if (itemData == null)
		{
			isValid = false;
			Debug.LogWarning("物品数据引用为空");
		}

		// 验证实例数据
		if (instanceData == null)
		{
			instanceData = new ItemInstanceData();
			if (itemData != null)
			{
				instanceData.InitializeFromItemData(itemData);
			}
			isValid = false;
			Debug.LogWarning("实例数据为空，已重新初始化");
		}
		else
		{
			// 验证实例数据有效性
			if (!instanceData.ValidateData(itemData))
			{
				isValid = false;
				Debug.LogWarning("实例数据验证失败，已修复");
			}
		}

		return isValid;
	}

	/// <summary>
	/// 初始化保存系统
	/// </summary>
	public void InitializeSaveSystem()
	{
		// 确保有有效的保存ID
		if (string.IsNullOrEmpty(saveID))
		{
			GenerateNewSaveID();
		}

		// 验证数据完整性
		ValidateData();

		// 初始化实例数据
		if (instanceData == null)
		{
			instanceData = new ItemInstanceData();
			if (itemData != null)
			{
				instanceData.InitializeFromItemData(itemData);
			}
		}

		Debug.Log($"物品数据持有者保存系统初始化完成: {saveID}");
	}

	/// <summary>
	/// 获取物品数据持有者状态摘要
	/// </summary>
	public string GetItemDataHolderStatusSummary()
	{
		string itemInfo = itemData != null ? $"{itemData.itemName} (ID:{itemData.id})" : "无物品数据";
		string instanceInfo = instanceData != null ? $"耐久度:{instanceData.currentDurability}, 堆叠:{instanceData.currentStackCount}" : "无实例数据";
		string modifiedStatus = IsModified() ? "已修改" : "未修改";

		return $"物品数据持有者 [{saveID}] - 物品:{itemInfo}, 实例:{instanceInfo}, 状态:{modifiedStatus}";
	}

	/// <summary>
	/// 在Start中自动初始化保存系统
	/// </summary>
	private void Start()
	{
		if (Application.isPlaying)
		{
			InitializeSaveSystem();
		}
	}
}