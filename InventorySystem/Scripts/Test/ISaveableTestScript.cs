using UnityEngine;
using InventorySystem.SaveSystem;

/// <summary>
/// 测试ISaveable接口实现的脚本
/// </summary>
public class ISaveableTestScript : MonoBehaviour
{
    [Header("测试按钮")]
    [SerializeField] private KeyCode testKey = KeyCode.T;

    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            TestISaveableImplementation();
        }
    }

    /// <summary>
    /// 测试ISaveable接口实现
    /// </summary>
    private void TestISaveableImplementation()
    {
        Debug.Log("=== 开始测试ISaveable接口实现 ===");

        // 查找所有BackpackItemGrid组件（包括Clone对象）
        BackpackItemGrid[] backpackGrids = FindObjectsOfType<BackpackItemGrid>();
        Debug.Log($"找到 {backpackGrids.Length} 个BackpackItemGrid组件");

        foreach (var grid in backpackGrids)
        {
            TestGridISaveable(grid, "BackpackItemGrid");
            TestGridInitializer(grid.gameObject);
        }

        // 查找所有TactiaclRigItemGrid组件（包括Clone对象）
        TactiaclRigItemGrid[] tacticalGrids = FindObjectsOfType<TactiaclRigItemGrid>();
        Debug.Log($"找到 {tacticalGrids.Length} 个TactiaclRigItemGrid组件");

        foreach (var grid in tacticalGrids)
        {
            TestGridISaveable(grid, "TactiaclRigItemGrid");
            TestGridInitializer(grid.gameObject);
        }

        // 测试所有BaseItemGrid组件
        BaseItemGrid[] allGrids = FindObjectsOfType<BaseItemGrid>();
        Debug.Log($"找到 {allGrids.Length} 个BaseItemGrid组件（总计）");

        Debug.Log("=== ISaveable接口测试完成 ===");
    }

    /// <summary>
    /// 测试单个网格的ISaveable实现
    /// </summary>
    /// <param name="gridComponent">网格组件</param>
    /// <param name="gridType">网格类型名称</param>
    private void TestGridISaveable(Component gridComponent, string gridType)
    {
        if (gridComponent == null)
        {
            Debug.LogError($"{gridType}: 组件为空");
            return;
        }

        // 检查是否实现ISaveable接口
        ISaveable saveableGrid = gridComponent as ISaveable;
        if (saveableGrid == null)
        {
            Debug.LogError($"{gridType} {gridComponent.name}: 未实现ISaveable接口");
            return;
        }

        Debug.Log($"{gridType} {gridComponent.name}: 成功实现ISaveable接口");

        // 测试保存ID
        string saveID = saveableGrid.GetSaveID();
        Debug.Log($"{gridType} {gridComponent.name}: 保存ID = {saveID}");

        // 测试保存ID有效性
        bool isValidID = saveableGrid.IsSaveIDValid();
        Debug.Log($"{gridType} {gridComponent.name}: 保存ID有效性 = {isValidID}");

        // 如果ID无效，生成新ID
        if (!isValidID)
        {
            saveableGrid.GenerateNewSaveID();
            string newSaveID = saveableGrid.GetSaveID();
            Debug.Log($"{gridType} {gridComponent.name}: 生成新保存ID = {newSaveID}");
        }

        // 测试保存数据序列化
        try
        {
            string jsonData = saveableGrid.SerializeToJson();
            Debug.Log($"{gridType} {gridComponent.name}: 序列化数据长度 = {jsonData.Length}");

            // 测试反序列化
            bool deserializeResult = saveableGrid.DeserializeFromJson(jsonData);
            Debug.Log($"{gridType} {gridComponent.name}: 反序列化结果 = {deserializeResult}");

            // 测试数据验证
            bool dataValid = saveableGrid.ValidateData();
            Debug.Log($"{gridType} {gridComponent.name}: 数据验证结果 = {dataValid}");

            // 检查是否为Clone对象
            if (gridComponent.name.Contains("Clone"))
            {
                Debug.Log($"[CLONE对象] {gridType} {gridComponent.name}: ISaveable接口测试通过");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{gridType} {gridComponent.name}: 序列化/反序列化测试失败 - {ex.Message}");
        }
    }

    /// <summary>
    /// 测试网格初始化器组件
    /// </summary>
    /// <param name="gridObject">网格游戏对象</param>
    private void TestGridInitializer(GameObject gridObject)
    {
        GridISaveableInitializer initializer = gridObject.GetComponent<GridISaveableInitializer>();
        if (initializer != null)
        {
            Debug.Log($"[初始化器] {gridObject.name}: 找到GridISaveableInitializer组件");
            
            // 获取详细状态信息
            string statusInfo = initializer.GetGridStatusInfo();
            Debug.Log($"[初始化器] {gridObject.name}: 状态信息\n{statusInfo}");
        }
        else
        {
            Debug.LogWarning($"[初始化器] {gridObject.name}: 未找到GridISaveableInitializer组件");
        }
    }
}