using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace InventorySystem
{
    /// <summary>
    /// 编辑器工具：自动清理EquipmentSlotManager实例
    /// 防止场景切换时出现残留对象警告
    /// </summary>
    [InitializeOnLoad]
    public class EquipmentSlotManagerCleaner
    {
        static EquipmentSlotManagerCleaner()
        {
            // 监听场景变化事件
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            // 场景打开时检查是否有残留的EquipmentSlotManager
            if (mode == OpenSceneMode.Single)
            {
                CleanupIfNeeded();
            }
        }
        
        private static void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            // 场景关闭时清理EquipmentSlotManager
            CleanupIfNeeded();
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 退出播放模式时清理
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                CleanupIfNeeded();
            }
        }
        
        private static void CleanupIfNeeded()
        {
            if (!EditorApplication.isPlaying)
            {
                // 只在编辑器模式下清理
                EquipmentSlotManager.ForceCleanup();
            }
        }
        
        /// <summary>
        /// 手动清理菜单项
        /// </summary>
        [MenuItem("Tools/Inventory System/Clean EquipmentSlotManager")]
        public static void ManualCleanup()
        {
            EquipmentSlotManager.ForceCleanup();
            Debug.Log("EquipmentSlotManager cleanup completed manually.");
        }
    }
}
