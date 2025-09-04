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
            // 确保状态已正确设置
            Debug.Log("WarehouseTrigger: F键触发，isInWarehouse = " + isInWarehouse);
            
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
