using System.Collections;
using UnityEngine;

/// <summary>
/// 网格保存管理器 - 专门处理动态网格的保存和加载
/// </summary>
public class GridSaveManager : MonoBehaviour
{
    private ItemGrid currentItemGrid;
    private string currentGridGUID;

    /// <summary>
    /// 设置当前管理的网格
    /// </summary>
    /// <param name="itemGrid">要管理的网格</param>
    /// <param name="gridGUID">网格的GUID</param>
    public void SetCurrentGrid(ItemGrid itemGrid, string gridGUID)
    {
        currentItemGrid = itemGrid;
        currentGridGUID = gridGUID;
    }

    /// <summary>
    /// 注册网格到保存系统并加载数据
    /// </summary>
    /// <param name="isWarehouse">是否为仓库网格</param>
    public void RegisterAndLoadGrid(bool isWarehouse)
    {
        if (currentItemGrid == null || string.IsNullOrEmpty(currentGridGUID))
        {
            Debug.LogError("GridSaveManager: 当前网格或GUID未设置！");
            return;
        }

        // 设置网格属性
        if (isWarehouse)
        {
            currentItemGrid.GridGUID = "warehouse_grid_main";
            currentItemGrid.GridName = "仓库网格";
            currentItemGrid.GridType = GridType.Storage;
        }
        else
        {
            currentItemGrid.GridGUID = "ground_grid_main";
            currentItemGrid.GridName = "地面网格";
            currentItemGrid.GridType = GridType.Ground;
        }

        currentGridGUID = currentItemGrid.GridGUID;

        // 注册到保存系统
        if (InventorySaveManager.Instance != null)
        {
            InventorySaveManager.Instance.RegisterGridByGUID(currentItemGrid);
            Debug.Log($"GridSaveManager: 已注册网格到保存系统 - GUID: {currentGridGUID}, 类型: {currentItemGrid.GridType}");

            // 延迟加载数据
            StartCoroutine(LoadGridDataDelayed());
        }
        else
        {
            Debug.LogWarning("GridSaveManager: InventorySaveManager实例不存在，无法注册网格保存功能");
        }
    }

    /// <summary>
    /// 延迟加载网格数据
    /// </summary>
    private IEnumerator LoadGridDataDelayed()
    {
        yield return null; // 等待一帧确保网格完全注册

        if (InventorySaveManager.Instance != null && currentItemGrid != null && !string.IsNullOrEmpty(currentGridGUID))
        {
            // 生成独立的存档文件名
            string saveFileName = GetGridSaveFileName(currentGridGUID);
            
            // 只在存档文件存在时才加载
            if (ES3.FileExists(saveFileName))
            {
                try
                {
                    // 使用InventorySaveManager的单个网格加载逻辑
                    bool loadResult = InventorySaveManager.Instance.LoadSingleGrid(currentItemGrid, saveFileName);
                    Debug.Log($"GridSaveManager: 加载网格数据 - GUID: {currentGridGUID}, 文件: {saveFileName}, 结果: {(loadResult ? "成功" : "失败")}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"GridSaveManager: 加载网格数据失败 - GUID: {currentGridGUID}, 错误: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"GridSaveManager: 存档文件不存在，跳过加载 - GUID: {currentGridGUID}, 文件: {saveFileName}（这是正常的，可能是首次使用该网格）");
            }
        }
    }

    /// <summary>
    /// 保存当前网格数据
    /// </summary>
    /// <param name="force">是否强制保存（无论是否有物品）</param>
    /// <returns>保存是否成功</returns>
    public bool SaveCurrentGrid(bool force = false)
    {
        if (currentItemGrid == null || InventorySaveManager.Instance == null || string.IsNullOrEmpty(currentGridGUID))
        {
            return false;
        }

        try
        {
            // 检查是否需要保存
            bool hasItems = HasItemsInGrid();
            
            if (force || hasItems)
            {
                // 生成独立的存档文件名
                string saveFileName = GetGridSaveFileName(currentGridGUID);
                
                // 使用InventorySaveManager的单个网格保存逻辑
                bool saveResult = InventorySaveManager.Instance.SaveSingleGrid(currentItemGrid, saveFileName);
                int itemCount = CountItemsInGrid();
                
                Debug.Log($"GridSaveManager: 保存网格数据 - GUID: {currentGridGUID}, 文件: {saveFileName}, 结果: {(saveResult ? "成功" : "失败")}, 物品数量: {itemCount}");
                return saveResult;
            }
            else
            {
                Debug.Log($"GridSaveManager: 网格为空，跳过保存 - GUID: {currentGridGUID}");
                return true; // 空网格跳过保存也算成功
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GridSaveManager: 保存网格时发生错误 - GUID: {currentGridGUID}, 错误: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 取消注册当前网格
    /// </summary>
    public void UnregisterCurrentGrid()
    {
        if (InventorySaveManager.Instance != null && !string.IsNullOrEmpty(currentGridGUID))
        {
            InventorySaveManager.Instance.UnregisterGrid(currentGridGUID);
            Debug.Log($"GridSaveManager: 已取消注册网格 - GUID: {currentGridGUID}");
        }

        // 清理引用
        currentItemGrid = null;
        currentGridGUID = null;
    }

    /// <summary>
    /// 清理并保存当前网格
    /// </summary>
    /// <param name="force">是否强制保存</param>
    public void CleanupAndSave(bool force = false)
    {
        SaveCurrentGrid(force);
        UnregisterCurrentGrid();
    }

    /// <summary>
    /// 检查网格中是否有物品
    /// </summary>
    private bool HasItemsInGrid()
    {
        if (currentItemGrid == null) return false;

        for (int x = 0; x < currentItemGrid.CurrentWidth; x++)
        {
            for (int y = 0; y < currentItemGrid.CurrentHeight; y++)
            {
                if (currentItemGrid.GetItemAt(x, y) != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 计算网格中的物品数量
    /// </summary>
    private int CountItemsInGrid()
    {
        if (currentItemGrid == null) return 0;

        int count = 0;
        for (int x = 0; x < currentItemGrid.CurrentWidth; x++)
        {
            for (int y = 0; y < currentItemGrid.CurrentHeight; y++)
            {
                if (currentItemGrid.GetItemAt(x, y) != null)
                {
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// 根据网格GUID生成存档文件名
    /// </summary>
    /// <param name="gridGUID">网格GUID</param>
    /// <returns>存档文件名</returns>
    private string GetGridSaveFileName(string gridGUID)
    {
        // 为不同类型的网格生成不同的文件名
        switch (gridGUID)
        {
            case "ground_grid_main":
                return "ground_grid_inventory.es3";
            case "warehouse_grid_main":
                return "warehouse_grid_inventory.es3";
            default:
                // 对于其他网格（如背包网格），使用GUID作为文件名
                return $"{gridGUID}_inventory.es3";
        }
    }
}
