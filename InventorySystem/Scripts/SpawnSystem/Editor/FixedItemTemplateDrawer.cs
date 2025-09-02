using UnityEngine;
using UnityEditor;
using InventorySystem.SpawnSystem;

namespace InventorySystem.SpawnSystem.Editor
{
    /// <summary>
    /// FixedItemTemplate的自定义PropertyDrawer
    /// </summary>
    [CustomPropertyDrawer(typeof(FixedItemTemplate))]
    public class FixedItemTemplateDrawer : PropertyDrawer
    {
        private bool foldout = false;
        private const float lineHeight = 18f;
        private const float spacing = 2f;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!foldout)
                return lineHeight;
            
            // 计算展开后的高度
            float height = lineHeight * 2; // 标题和基础信息头部
            
            // 基础信息部分 (3行)
            height += lineHeight * 3 + spacing * 3;
            
            // 位置配置部分 (4行)
            height += lineHeight * 5 + spacing * 5;
            
            // 生成策略部分 (4行)
            height += lineHeight * 5 + spacing * 5;
            
            // 生成条件部分 (3行)
            height += lineHeight * 4 + spacing * 4;
            
            return height + 20; // 额外边距
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            float currentY = position.y;
            float width = position.width;
            
            // 获取属性
            var templateIdProp = property.FindPropertyRelative("templateId");
            var itemDataProp = property.FindPropertyRelative("itemData");
            
            // 创建标题
            string displayName = string.IsNullOrEmpty(templateIdProp.stringValue) 
                ? "新物品模板" 
                : templateIdProp.stringValue;
            
            if (itemDataProp.objectReferenceValue != null)
            {
                displayName = $"{displayName} ({itemDataProp.objectReferenceValue.name})";
            }
            
            // 折叠面板
            Rect foldoutRect = new Rect(position.x, currentY, width, lineHeight);
            foldout = EditorGUI.Foldout(foldoutRect, foldout, displayName, true);
            currentY += lineHeight + spacing;
            
            if (foldout)
            {
                EditorGUI.indentLevel++;
                
                // 基础信息
                DrawSectionHeader(ref currentY, width, position.x, "基础信息");
                DrawProperty(ref currentY, width, position.x, property, "templateId", "物品唯一ID");
                DrawProperty(ref currentY, width, position.x, property, "itemData", "物品数据引用");
                DrawProperty(ref currentY, width, position.x, property, "quantity", "生成数量");
                
                currentY += spacing;
                
                // 位置配置
                DrawSectionHeader(ref currentY, width, position.x, "位置配置");
                DrawProperty(ref currentY, width, position.x, property, "placementType", "放置类型");
                
                var placementType = property.FindPropertyRelative("placementType");
                PlacementType placementTypeEnum = (PlacementType)placementType.enumValueIndex;
                
                switch (placementTypeEnum)
                {
                    case PlacementType.Exact:
                        DrawProperty(ref currentY, width, position.x, property, "exactPosition", "精确位置");
                        break;
                    case PlacementType.AreaConstrained:
                        DrawProperty(ref currentY, width, position.x, property, "constrainedArea", "约束区域");
                        break;
                    case PlacementType.Priority:
                        DrawProperty(ref currentY, width, position.x, property, "preferredArea", "优先区域");
                        break;
                }
                
                currentY += spacing;
                
                // 生成策略
                DrawSectionHeader(ref currentY, width, position.x, "生成策略");
                DrawProperty(ref currentY, width, position.x, property, "priority", "生成优先级");
                DrawProperty(ref currentY, width, position.x, property, "scanPattern", "扫描模式");
                DrawProperty(ref currentY, width, position.x, property, "allowRotation", "允许旋转");
                DrawProperty(ref currentY, width, position.x, property, "conflictResolution", "冲突解决策略");
                
                currentY += spacing;
                
                // 生成条件
                DrawSectionHeader(ref currentY, width, position.x, "生成条件");
                DrawProperty(ref currentY, width, position.x, property, "isUniqueSpawn", "是否唯一生成");
                DrawProperty(ref currentY, width, position.x, property, "maxRetryAttempts", "最大重试次数");
                DrawProperty(ref currentY, width, position.x, property, "enableDebugLog", "启用调试日志");
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        private void DrawSectionHeader(ref float currentY, float width, float x, string title)
        {
            Rect headerRect = new Rect(x, currentY, width, lineHeight);
            EditorGUI.LabelField(headerRect, title, EditorStyles.boldLabel);
            currentY += lineHeight + spacing;
        }
        
        private void DrawProperty(ref float currentY, float width, float x, SerializedProperty parent, string propertyName, string displayName)
        {
            var prop = parent.FindPropertyRelative(propertyName);
            if (prop != null)
            {
                Rect propRect = new Rect(x, currentY, width, lineHeight);
                EditorGUI.PropertyField(propRect, prop, new GUIContent(displayName));
                currentY += lineHeight + spacing;
            }
        }
    }
}
