// WarehouseTrigger.cs
using UnityEngine;

public class WarehouseTrigger : BaseContainerTrigger
{
    public static bool isInWarehouse = false; // Global state
    [SerializeField] private BackpackState backpackState;

    /// <summary>
    /// 仓库进入时目标应显示 Storage 网格
    /// </summary>
    protected override GridType GetGridTypeForRefresh()
    {
        return GridType.Storage;
    }

    /// <summary>
    /// 优先返回本地序列化引用的 BackpackState，失效时回退到基类查找
    /// </summary>
    protected override BackpackState GetBackpackStateReference()
    {
        if (backpackState != null && backpackState.gameObject != null)
        {
            return backpackState;
        }
        return base.GetBackpackStateReference();
    }

    protected override bool IsContainerOpen()
    {
        return backpackState != null && backpackState.IsBackpackOpen();
    }

    protected override void ToggleContainer()
    {
        if (backpackState != null)
        {
            // F键功能已移除，此方法现在仅通过其他方式调用
            if (debugLog) LogInfo($"切换仓库状态，isInWarehouse={isInWarehouse}");
            
            if (backpackState.IsBackpackOpen())
            {
                backpackState.ForceCloseBackpack();
            }
            else
            {
                // 使用和Tab键相同的打开方式
                if (backpackState.topNavigationTransform != null)
                {
                    backpackState.topNavigationTransform.ToggleBackpack();
                }
                else
                {
                    backpackState.ForceOpenBackpack();
                }
            }
        }
    }

    protected override void ForceCloseContainer()
    {
        if (backpackState != null)
        {
            backpackState.ForceCloseBackpack();
        }
    }

    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        base.OnPlayerEnterTrigger(playerCollider);
        isInWarehouse = true;

        if (debugLog) LogInfo("玩家进入仓库触发器范围，准备预刷新右侧网格为Storage");

        // 与货架保持一致：背包未打开时，预刷新为目标网格，避免第一次 Tab 显示旧网格
        ForceRefreshGridIfBackpackClosed();
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        base.OnPlayerExitTrigger(playerCollider);
        isInWarehouse = false;
    }
}
