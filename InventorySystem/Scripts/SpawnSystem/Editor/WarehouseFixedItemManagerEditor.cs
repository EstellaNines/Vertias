using UnityEngine;
using UnityEditor;
using InventorySystem.SpawnSystem;

namespace InventorySystem.SpawnSystem.Editor
{
    /// <summary>
    /// 仓库固定物品管理器的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(WarehouseFixedItemManager))]
    public class WarehouseFixedItemManagerEditor : UnityEditor.Editor
    {
        private WarehouseFixedItemManager manager;
        
        private void OnEnable()
        {
            manager = (WarehouseFixedItemManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            // 绘制默认的Inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("仓库生成状态", EditorStyles.boldLabel);
            
            // 显示当前状态
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("当前存档ID", manager.GetCurrentSaveGameId());
            EditorGUILayout.Toggle("仓库已生成", manager.HasWarehouseGenerated());
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("重置生成状态", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("确认重置", 
                    "确定要重置仓库生成状态吗？这将允许重新生成仓库物品。", 
                    "确定", "取消"))
                {
                    manager.ResetWarehouseGenerationStatus();
                    Debug.Log("仓库生成状态已重置");
                }
            }
            
            if (GUILayout.Button("强制生成物品", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("确认强制生成", 
                    "确定要强制生成仓库物品吗？这将忽略已生成的标记。", 
                    "确定", "取消"))
                {
                    manager.ForceGenerateWarehouseItems();
                    Debug.Log("执行强制生成仓库物品");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 状态信息
            EditorGUILayout.LabelField("详细状态信息", EditorStyles.boldLabel);
            string statusInfo = manager.GetStatusInfo();
            EditorGUILayout.HelpBox(statusInfo, MessageType.Info);
            
            EditorGUILayout.Space(5);
            
            // 帮助信息
            EditorGUILayout.LabelField("使用说明", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. 仓库固定物品管理器确保每个存档只生成一次仓库物品\n" +
                "2. 存档ID会自动检测，也可以手动设置\n" +
                "3. 重置生成状态将允许重新生成物品\n" +
                "4. 强制生成将忽略已生成标记，立即生成物品",
                MessageType.Info
            );
        }
    }
}
