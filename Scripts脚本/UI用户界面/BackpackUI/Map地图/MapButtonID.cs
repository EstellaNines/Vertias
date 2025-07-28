using UnityEngine;
using UnityEngine.UI;

// 地图按钮ID组件
// 挂载在按钮上，用于设置和管理按钮对应的地图ID
public class MapButtonID : MonoBehaviour
{
    [Header("地图按钮设置")]
    [SerializeField] private int mapID = -1;
    
    [Header("调试信息")]
    [SerializeField] private bool showDebugInfo = true;

    // 地图ID属性
    public int MapID
    {
        get { return mapID; }
        set 
        { 
            mapID = value;
            if (showDebugInfo)
            {
                Debug.Log($"按钮 {gameObject.name} 的地图ID已设置为: {mapID}");
            }
        }
    }

    // 获取按钮组件
    public Button ButtonComponent
    {
        get { return GetComponent<Button>(); }
    }

    void Start()
    {
        // 验证组件设置
        ValidateComponent();
        
        // 添加按钮点击事件监听
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }
    
    // 按钮点击事件处理
    private void OnButtonClicked()
    {
        if (mapID >= 0)
        {
            // 发送地图ID到MapConfirmController
            MapConfirmController mapConfirmController = FindObjectOfType<MapConfirmController>();
            if (mapConfirmController != null)
            {
                mapConfirmController.SendMessage("ReceiveMapID", mapID, SendMessageOptions.DontRequireReceiver);
                Debug.Log($"MapButtonID: 已发送地图ID {mapID} 到MapConfirmController");
            }
            else
            {
                Debug.LogWarning("MapButtonID: 未找到MapConfirmController组件");
            }
        }
        else
        {
            Debug.LogWarning($"MapButtonID: 按钮 {gameObject.name} 的地图ID无效: {mapID}");
        }
    }

    // 验证组件设置是否正确
    private void ValidateComponent()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"MapButtonID组件必须挂载在有Button组件的GameObject上！当前对象: {gameObject.name}");
            return;
        }

        if (mapID < 0)
        {
            Debug.LogWarning($"按钮 {gameObject.name} 的地图ID未设置或无效 (当前值: {mapID})");
        }
        else if (showDebugInfo)
        {
            Debug.Log($"按钮 {gameObject.name} 地图ID验证通过: {mapID}");
        }
    }

    // 重置地图ID
    public void ResetMapID()
    {
        MapID = -1;
        
        if (showDebugInfo)
        {
            Debug.Log($"按钮 {gameObject.name} 的地图ID已重置");
        }
    }

#if UNITY_EDITOR
    // 在编辑器中验证设置
    void OnValidate()
    {
        // 确保地图ID不小于-1
        if (mapID < -1)
        {
            mapID = -1;
        }
    }
#endif
}