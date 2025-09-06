// WarehouseTrigger.cs
using UnityEngine;

public class WarehouseTrigger : BaseContainerTrigger
{
    public static bool isInWarehouse = false; // Global state
    [SerializeField] private BackpackState backpackState;

    protected override bool IsContainerOpen()
    {
        return backpackState != null && backpackState.IsBackpackOpen();
    }

    protected override void ToggleContainer()
    {
        if (backpackState != null)
        {
            // F键功能已移除，此方法现在仅通过其他方式调用
            Debug.Log("WarehouseTrigger: 切换仓库状态，isInWarehouse = " + isInWarehouse);
            
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
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        base.OnPlayerExitTrigger(playerCollider);
        isInWarehouse = false;
    }
}
