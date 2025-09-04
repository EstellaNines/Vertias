using UnityEngine;

namespace InventorySystem.SpawnSystem
{
    /// <summary>
    /// 库存生成标记组件
    /// 用于标记由生成系统创建的物品，便于跟踪和识别
    /// </summary>
    [AddComponentMenu("Inventory System/Spawn System/Inventory Spawn Tag")]
    public class InventorySpawnTag : MonoBehaviour
    {
        [Header("生成标记信息")]
        [FieldLabel("模板ID")]
        [Tooltip("生成模板的唯一标识符")]
        [SerializeField] public string templateId;
        
        [FieldLabel("物品ID")]
        [Tooltip("物品数据的ID")]
        [SerializeField] public string itemId;
        
        [FieldLabel("容器ID")]
        [Tooltip("生成容器的ID")]
        [SerializeField] public string containerId;
        
        [FieldLabel("生成时间")]
        [Tooltip("物品生成的时间戳")]
        [SerializeField] public string spawnTime;
        
        [FieldLabel("生成批次")]
        [Tooltip("生成批次编号")]
        [SerializeField] public string batchId;
        
        [Header("状态信息")]
        [FieldLabel("已被移动")]
        [Tooltip("物品是否已从原始位置移动")]
        [SerializeField] public bool hasMoved = false;
        
        [FieldLabel("原始位置")]
        [Tooltip("物品的原始生成位置")]
        [SerializeField] public Vector2Int originalPosition;
        
        [FieldLabel("额外数据")]
        [Tooltip("额外的自定义数据")]
        [SerializeField] public string extraData;
        
        #region 初始化方法
        
        /// <summary>
        /// 初始化生成标记
        /// </summary>
        public void Initialize(string templateId, string itemId, string containerId, Vector2Int spawnPosition)
        {
            this.templateId = templateId;
            this.itemId = itemId;
            this.containerId = containerId;
            this.originalPosition = spawnPosition;
            this.spawnTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.batchId = System.DateTime.Now.Ticks.ToString();
            this.hasMoved = false;
            this.extraData = "";
        }
        
        /// <summary>
        /// 完整初始化生成标记
        /// </summary>
        public void Initialize(string templateId, string itemId, string containerId, Vector2Int spawnPosition, string batchId, string extraData = "")
        {
            this.templateId = templateId;
            this.itemId = itemId;
            this.containerId = containerId;
            this.originalPosition = spawnPosition;
            this.spawnTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.batchId = batchId;
            this.hasMoved = false;
            this.extraData = extraData;
        }
        
        #endregion
        
        #region 状态更新方法
        
        /// <summary>
        /// 标记物品已移动
        /// </summary>
        public void MarkAsMoved()
        {
            hasMoved = true;
        }
        
        /// <summary>
        /// 更新额外数据
        /// </summary>
        public void UpdateExtraData(string newData)
        {
            extraData = newData;
        }
        
        #endregion
        
        #region 查询方法
        
        /// <summary>
        /// 检查是否为指定模板的物品
        /// </summary>
        public bool IsFromTemplate(string checkTemplateId)
        {
            return templateId == checkTemplateId;
        }
        
        /// <summary>
        /// 检查是否为指定容器的物品
        /// </summary>
        public bool IsFromContainer(string checkContainerId)
        {
            return containerId == checkContainerId;
        }
        
        /// <summary>
        /// 获取生成信息摘要
        /// </summary>
        public string GetSpawnSummary()
        {
            return $"Template: {templateId}, Item: {itemId}, Container: {containerId}, SpawnTime: {spawnTime}, Moved: {hasMoved}";
        }
        
        #endregion
        
        #region Unity生命周期
        
        private void Start()
        {
            // 可以在这里添加初始化后的逻辑
        }
        
        #endregion
        
        #region 调试方法
        
        /// <summary>
        /// 在编辑器中显示详细信息
        /// </summary>
        [ContextMenu("显示生成信息")]
        public void DebugShowSpawnInfo()
        {
            Debug.Log($"[InventorySpawnTag] {GetSpawnSummary()}");
        }
        
        #endregion
    }
}
