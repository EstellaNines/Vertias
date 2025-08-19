using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // 引入System命名空间以支持反射功能

/// <summary>
/// 仓库背包触发器 - 继承自BaseContainerTrigger基类
/// 实现进入仓库区域时按F键打开背包的功能
/// </summary>
public class StroageToBackpack : BaseContainerTrigger
{
    [Header("背包系统")]
    public BackpackState backpackState; // 背包状态管理器引用

    /// <summary>
    /// 重写基类的OnChildStart方法，执行背包特定的初始化逻辑
    /// </summary>
    protected override void OnChildStart()
    {
        // 如果没有手动设置BackpackState，尝试自动查找
        if (backpackState == null)
        {
            backpackState = FindObjectOfType<BackpackState>();
            if (backpackState == null)
            {
                Debug.LogWarning("未找到BackpackState组件！请确保场景中存在BackpackState脚本。");
            }
        }
    }

    /// <summary>
    /// 实现基类抽象方法：检查背包是否已打开
    /// </summary>
    /// <returns>背包是否已打开</returns>
    protected override bool IsContainerOpen()
    {
        return backpackState != null && backpackState.IsBackpackOpen();
    }

    /// <summary>
    /// 实现基类抽象方法：切换背包状态
    /// 使用反射调用ToggleBackpack方法，与Tab键保持一致的逻辑
    /// </summary>
    protected override void ToggleContainer()
    {
        if (backpackState != null)
        {
            // 调用BackpackState的ToggleBackpack方法，与Tab键保持一致的逻辑
            // 这会委托给TopNavigationTransform处理，确保F键和Tab键功能完全统一
            backpackState.GetComponent<BackpackState>().GetType().GetMethod("ToggleBackpack",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(backpackState, null);
        }
    }

    /// <summary>
    /// 实现基类抽象方法：强制关闭背包
    /// </summary>
    protected override void ForceCloseContainer()
    {
        if (backpackState != null)
        {
            backpackState.ForceCloseBackpack();
        }
    }

    /// <summary>
    /// 重写基类虚方法：返回容器类型名称
    /// </summary>
    /// <returns>容器类型名称</returns>
    protected override string GetContainerTypeName()
    {
        return "背包";
    }


}
