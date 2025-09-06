using UnityEngine;
using UnityEngine.UI;
using GlobalMessaging;

// 地图按钮ID组件
// 挂载在按钮上，用于设置和管理按钮对应的地图ID
public class MapButtonID : MonoBehaviour
{
    [Header("地图按钮设置")]
    [SerializeField] [FieldLabel("地图ID")]private int mapID = -1;

    [Header("调试信息")]
    [SerializeField] [FieldLabel("显示调试信息")]private bool showDebugInfo = true;

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
            // 获取地图数据
            MapButtonManager mapManager = FindObjectOfType<MapButtonManager>();
            if (mapManager != null)
            {
                MapData mapData = mapManager.GetMapDataByID(mapID);
                if (mapData != null)
                {
                    // 使用MessagingCenter发送地图ID消息
                    MapIDSelectedMessage message = new MapIDSelectedMessage(mapData.id, mapData.name, mapData.isUnlocked);
                    MessagingCenter.Instance.Send(message);
                    Debug.Log($"MapButtonID: 已通过MessagingCenter发送地图ID {mapID} 消息");
                }
                else
                {
                    Debug.LogWarning($"MapButtonID: 未找到ID为 {mapID} 的地图数据");
                }
            }
            else
            {
                Debug.LogWarning("MapButtonID: 未找到MapButtonManager组件");
            }
        }
        else
        {
            Debug.LogWarning($"MapButtonID: 按钮 {gameObject.name} 的地图ID无效: {mapID}");
        }
    }

    // 验证组件设置
    private void ValidateComponent()
    {
        Button button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"MapButtonID组件在GameObject {gameObject.name} 上没有找到Button组件");
            return;
        }

        if (mapID < 0)
        {
            Debug.LogWarning($"地图按钮 {gameObject.name} 的地图ID未被设置或无效 (当前值: {mapID})");
        }
        else if (showDebugInfo)
        {
            Debug.Log($"地图按钮 {gameObject.name} 的地图ID验证通过: {mapID}");
        }
    }

    // 重置地图ID
    public void ResetMapID()
    {
        MapID = -1;

        if (showDebugInfo)
        {
            Debug.Log($"地图按钮 {gameObject.name} 的地图ID已被重置");
        }
    }

#if UNITY_EDITOR
    // 编辑器验证组件设置
    void OnValidate()
    {
        // 确保地图ID小于-1
        if (mapID < -1)
        {
            mapID = -1;
        }
    }
#endif
}