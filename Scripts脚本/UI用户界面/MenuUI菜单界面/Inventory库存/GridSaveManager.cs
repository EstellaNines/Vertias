using System.Collections;
using UnityEngine;

/// <summary>
/// 网格保存管理器 - 专门处理动态网格的保存和加载
/// 注意：此管理器不处理高亮提示器，提示器由InventoryController统一管理
/// </summary>
public class GridSaveManager : MonoBehaviour
{
    private ItemGrid currentItemGrid;
    private string currentGridGUID;
    // 会话级：记录本次运行中已清理过的GUID，防止重复清理
    private static System.Collections.Generic.HashSet<string> s_ClearedGuidsThisSession = new System.Collections.Generic.HashSet<string>();

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
    /// 使用指定GUID注册网格到保存系统并加载数据（新方法）
    /// </summary>
    /// <param name="uniqueGridGUID">唯一的网格GUID</param>
    /// <param name="isWarehouse">是否为仓库网格</param>
    public void RegisterAndLoadGridWithGUID(string uniqueGridGUID, bool isWarehouse)
    {
        if (currentItemGrid == null || string.IsNullOrEmpty(uniqueGridGUID))
        {
            Debug.LogError("GridSaveManager: 当前网格或GUID未设置！");
            return;
        }

        // 使用传入的唯一GUID设置网格属性
        currentItemGrid.GridGUID = uniqueGridGUID;
        currentGridGUID = uniqueGridGUID;
        
        if (isWarehouse)
        {
            currentItemGrid.GridName = $"仓库网格 ({uniqueGridGUID})";
            currentItemGrid.GridType = GridType.Storage;
        }
        else
        {
            currentItemGrid.GridName = $"地面网格 ({uniqueGridGUID})";
            currentItemGrid.GridType = GridType.Ground;
        }

        // 注册到保存系统
        if (InventorySaveManager.Instance != null)
        {
            InventorySaveManager.Instance.RegisterGridByGUID(currentItemGrid);
            Debug.Log($"GridSaveManager: 已注册唯一网格到保存系统 - GUID: {currentGridGUID}, 类型: {currentItemGrid.GridType}");

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
    /// 强制保存当前网格数据（不管是否有物品）
    /// </summary>
    /// <returns>保存是否成功</returns>
    public bool ForceSaveCurrentGrid()
    {
        return SaveCurrentGrid(true);
    }

    /// <summary>
    /// 取消注册当前网格
    /// 注意：此方法只处理网格数据的注销，不涉及高亮提示器的处理
    /// 高亮提示器的管理由BackpackPanelController负责调用InventoryController处理
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

    /// <summary>
    /// 删除指定GUID网格的存档文件（用于会话重置等场景）
    /// </summary>
    /// <param name="gridGUID">网格GUID</param>
    /// <returns>是否删除了文件（存在且删除成功返回true）</returns>
    public bool DeleteGridSaveFile(string gridGUID)
    {
        if (string.IsNullOrEmpty(gridGUID))
        {
            Debug.LogWarning("GridSaveManager: DeleteGridSaveFile 失败 - gridGUID 为空");
            return false;
        }

        string saveFileName = GetGridSaveFileName(gridGUID);
        if (ES3.FileExists(saveFileName))
        {
            try
            {
                ES3.DeleteFile(saveFileName);
                Debug.Log($"GridSaveManager: 已删除网格存档文件 - GUID: {gridGUID}, 文件: {saveFileName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GridSaveManager: 删除网格存档文件失败 - GUID: {gridGUID}, 错误: {e.Message}");
                return false;
            }
        }
        else
        {
            // 文件不存在也视为无需删除
            return false;
        }
    }

    /// <summary>
    /// 仅在本次运行中第一次为该GUID执行清理；后续调用将跳过，保证同一会话内数据保留
    /// </summary>
    /// <param name="gridGUID">网格GUID</param>
    /// <returns>是否本次调用执行了删除操作</returns>
    public bool EnsureSessionClearOnce(string gridGUID)
    {
        if (string.IsNullOrEmpty(gridGUID)) return false;
        if (s_ClearedGuidsThisSession.Contains(gridGUID))
        {
            return false; // 本会话已清过
        }

        bool deleted = DeleteGridSaveFile(gridGUID);
        s_ClearedGuidsThisSession.Add(gridGUID);
        return deleted;
    }
}
