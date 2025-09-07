// ShelfTrigger.cs
using UnityEngine;

public class ShelfTrigger : BaseContainerTrigger
{
    public static bool isInShelf = false; // 全局状态
    [SerializeField] private BackpackState backpackState;

    protected override bool IsContainerOpen()
    {
        return backpackState != null && backpackState.IsBackpackOpen();
    }

    protected override void ToggleContainer()
    {
        if (backpackState != null)
        {
            // 注意：货架触发器不需要主动处理Tab键切换
            // Tab键由正常输入系统处理，货架只负责设置isInShelf状态标志
            // 这个方法仅在需要程序化切换时调用（如F键，但已被移除）
            Debug.Log("ShelfTrigger: 程序化切换货架状态，isInShelf = " + isInShelf);
            
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

    protected override string GetContainerTypeName()
    {
        return "货架";
    }

    protected override void OnPlayerEnterTrigger(Collider2D playerCollider)
    {
        base.OnPlayerEnterTrigger(playerCollider);
        isInShelf = true;
        
        // 更新UI文本为货架专用文本
        if (tmpText != null)
        {
            tmpText.text = "[Tab] Search";
        }
    }

    protected override void OnPlayerExitTrigger(Collider2D playerCollider)
    {
        base.OnPlayerExitTrigger(playerCollider);
        isInShelf = false;
    }
}
