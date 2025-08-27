// using UnityEngine;
// using UnityEditor;
// using InventorySystem;

// [CustomEditor(typeof(ItemDatabase))]
// public class ItemDatabaseEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();
        
//         ItemDatabase database = (ItemDatabase)target;
        
//         GUILayout.Space(10);
//         GUILayout.Label("数据库管理", EditorStyles.boldLabel);
        
//         if (GUILayout.Button("重新加载数据库"))
//         {
//             database.Initialize();
//         }
        
//         if (GUILayout.Button("验证数据库完整性"))
//         {
//             database.ValidateDatabase();
//         }
        
//         // 显示当前状态
//         GUILayout.Space(10);
//         EditorGUILayout.LabelField("当前状态", database.databaseStatus);
//         EditorGUILayout.LabelField("已加载物品数量", database.loadedItemCount.ToString());
//     }
// }