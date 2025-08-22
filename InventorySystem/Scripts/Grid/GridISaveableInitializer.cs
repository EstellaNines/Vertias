using UnityEngine;
using InventorySystem.SaveSystem;

/// <summary>
/// 网格ISaveable接口初始化器
/// 确保动态生成的网格对象正确实现ISaveable接口
/// </summary>
public class GridISaveableInitializer : MonoBehaviour
{
    [Header("调试信息")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// 在Awake中强制初始化ISaveable接口
    /// </summary>
    private void Awake()
    {
        InitializeISaveableInterface();
    }

    /// <summary>
    /// 在Start中再次确认ISaveable接口状态
    /// </summary>
    private void Start()
    {
        ValidateISaveableInterface();
    }

    /// <summary>
    /// 初始化ISaveable接口实现
    /// </summary>
    private void InitializeISaveableInterface()
    {
        // 获取网格组件
        BaseItemGrid gridComponent = GetComponent<BaseItemGrid>();
        if (gridComponent == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[GridISaveableInitializer] {gameObject.name}: 未找到BaseItemGrid组件");
            return;
        }

        // 检查是否实现ISaveable接口
        ISaveable saveableGrid = gridComponent as ISaveable;
        if (saveableGrid == null)
        {
            Debug.LogError($"[GridISaveableInitializer] {gameObject.name}: BaseItemGrid组件未实现ISaveable接口！");
            return;
        }

        // 强制初始化保存系统
        try
        {
            // 确保有有效的保存ID
            if (!saveableGrid.IsSaveIDValid())
            {
                saveableGrid.GenerateNewSaveID();
                if (enableDebugLogs)
                    Debug.Log($"[GridISaveableInitializer] {gameObject.name}: 生成新的保存ID: {saveableGrid.GetSaveID()}");
            }

            // 调用网格的保存系统初始化方法
            if (gridComponent is BackpackItemGrid backpackGrid)
            {
                // 强制调用背包网格的初始化方法
                var initMethod = typeof(BackpackItemGrid).GetMethod("InitializeSaveSystem",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                initMethod?.Invoke(backpackGrid, null);

                if (enableDebugLogs)
                    Debug.Log($"[GridISaveableInitializer] {gameObject.name}: BackpackItemGrid保存系统初始化完成");
            }
            else if (gridComponent is TactiaclRigItemGrid tacticalGrid)
            {
                // 强制调用战术挂具网格的初始化方法
                var initMethod = typeof(TactiaclRigItemGrid).GetMethod("InitializeSaveSystem",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                initMethod?.Invoke(tacticalGrid, null);

                if (enableDebugLogs)
                    Debug.Log($"[GridISaveableInitializer] {gameObject.name}: TacticalRigItemGrid保存系统初始化完成");
            }
            else
            {
                // 调用基类的初始化方法
                var initMethod = typeof(BaseItemGrid).GetMethod("InitializeSaveSystem",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                initMethod?.Invoke(gridComponent, null);

                if (enableDebugLogs)
                    Debug.Log($"[GridISaveableInitializer] {gameObject.name}: BaseItemGrid保存系统初始化完成");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridISaveableInitializer] {gameObject.name}: 初始化ISaveable接口时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证ISaveable接口实现状态
    /// </summary>
    private void ValidateISaveableInterface()
    {
        BaseItemGrid gridComponent = GetComponent<BaseItemGrid>();
        if (gridComponent == null) return;

        ISaveable saveableGrid = gridComponent as ISaveable;
        if (saveableGrid == null)
        {
            Debug.LogError($"[GridISaveableInitializer] {gameObject.name}: ISaveable接口验证失败！");
            return;
        }

        // 测试ISaveable接口的各个方法
        try
        {
            string saveID = saveableGrid.GetSaveID();
            bool isValidID = saveableGrid.IsSaveIDValid();
            bool isModified = saveableGrid.IsModified();
            string lastModified = saveableGrid.GetLastModified();

            if (enableDebugLogs)
            {
                Debug.Log($"[GridISaveableInitializer] {gameObject.name}: ISaveable接口验证成功\n" +
                         $"保存ID: {saveID}\n" +
                         $"ID有效性: {isValidID}\n" +
                         $"已修改: {isModified}\n" +
                         $"最后修改: {lastModified}");
            }

            // 测试序列化
            string jsonData = saveableGrid.SerializeToJson();
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogWarning($"[GridISaveableInitializer] {gameObject.name}: 序列化返回空数据");
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[GridISaveableInitializer] {gameObject.name}: 序列化测试成功，数据长度: {jsonData.Length}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GridISaveableInitializer] {gameObject.name}: ISaveable接口验证时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 手动触发ISaveable接口重新初始化
    /// 可在运行时调用以修复接口问题
    /// </summary>
    [ContextMenu("重新初始化ISaveable接口")]
    public void ReinitializeISaveableInterface()
    {
        Debug.Log($"[GridISaveableInitializer] {gameObject.name}: 手动重新初始化ISaveable接口");
        InitializeISaveableInterface();
        ValidateISaveableInterface();
    }

    /// <summary>
    /// 获取网格的详细状态信息
    /// </summary>
    /// <returns>状态信息字符串</returns>
    public string GetGridStatusInfo()
    {
        BaseItemGrid gridComponent = GetComponent<BaseItemGrid>();
        if (gridComponent == null)
            return "未找到BaseItemGrid组件";

        ISaveable saveableGrid = gridComponent as ISaveable;
        if (saveableGrid == null)
            return "BaseItemGrid组件未实现ISaveable接口";

        var info = new System.Text.StringBuilder();
        info.AppendLine($"游戏对象: {gameObject.name}");
        info.AppendLine($"组件类型: {gridComponent.GetType().Name}");
        info.AppendLine($"保存ID: {saveableGrid.GetSaveID()}");
        info.AppendLine($"ID有效性: {saveableGrid.IsSaveIDValid()}");
        info.AppendLine($"已修改: {saveableGrid.IsModified()}");
        info.AppendLine($"最后修改: {saveableGrid.GetLastModified()}");
        info.AppendLine($"数据有效性: {saveableGrid.ValidateData()}");

        return info.ToString();
    }
}