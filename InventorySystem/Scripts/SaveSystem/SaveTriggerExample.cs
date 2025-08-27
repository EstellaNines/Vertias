using UnityEngine;
using UnityEngine.UI;

public class SaveTriggerExample : MonoBehaviour
{
    [Header("UI按钮引用")]
    public Button saveButton;
    public Button loadButton;
    public Button quickSaveButton;
    
    private void Start()
    {
        // 绑定按钮事件
        if (saveButton != null)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
            
        if (loadButton != null)
            loadButton.onClick.AddListener(OnLoadButtonClicked);
            
        if (quickSaveButton != null)
            quickSaveButton.onClick.AddListener(OnQuickSaveClicked);
        
        // 监听保存/加载完成事件
        InventorySaveManager.OnSaveCompleted += OnSaveCompleted;
        InventorySaveManager.OnLoadCompleted += OnLoadCompleted;
    }
    
    private void OnDestroy()
    {
        // 取消事件监听
        InventorySaveManager.OnSaveCompleted -= OnSaveCompleted;
        InventorySaveManager.OnLoadCompleted -= OnLoadCompleted;
    }
    
    // 手动保存
    private void OnSaveButtonClicked()
    {
        if (InventorySaveManager.Instance != null)
        {
            InventorySaveManager.Instance.SaveInventory();
        }
    }
    
    // 手动加载
    private void OnLoadButtonClicked()
    {
        if (InventorySaveManager.Instance != null)
        {
            InventorySaveManager.Instance.LoadInventory();
        }
    }
    
    // 快速保存
    private void OnQuickSaveClicked()
    {
        if (InventorySaveManager.Instance != null)
        {
            InventorySaveManager.Instance.ForceSave();
        }
    }
    
    // 保存完成回调
    private void OnSaveCompleted(bool success)
    {
        if (success)
        {
            Debug.Log("保存成功！");
            // 显示保存成功提示UI
        }
        else
        {
            Debug.LogError("保存失败！");
            // 显示保存失败提示UI
        }
    }
    
    // 加载完成回调
    private void OnLoadCompleted(bool success)
    {
        if (success)
        {
            Debug.Log("加载成功！");
            // 显示加载成功提示UI
        }
        else
        {
            Debug.LogError("加载失败！");
            // 显示加载失败提示UI
        }
    }
}