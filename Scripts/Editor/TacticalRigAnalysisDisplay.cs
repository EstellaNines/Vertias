using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 战术挂具分析显示组件
/// 专门用于在Editor窗口中显示战术挂具的详细配置分析、插槽效率、负载平衡等高级信息
/// </summary>
public class TacticalRigAnalysisDisplay
{
    // GUI样式缓存
    private static GUIStyle headerStyle;
    private static GUIStyle subHeaderStyle;
    private static GUIStyle infoStyle;
    private static GUIStyle warningStyle;
    private static GUIStyle errorStyle;
    private static GUIStyle successStyle;

    // 折叠状态
    private static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    // 颜色定义
    private static readonly Color excellentColor = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color goodColor = new Color(0.6f, 0.8f, 0.2f);
    private static readonly Color averageColor = new Color(0.8f, 0.8f, 0.2f);
    private static readonly Color poorColor = new Color(0.8f, 0.4f, 0.2f);
    private static readonly Color criticalColor = new Color(0.8f, 0.2f, 0.2f);

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private static void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }

        if (subHeaderStyle == null)
        {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
        }

        if (infoStyle == null)
        {
            infoStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }

        if (warningStyle == null)
        {
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.yellow }
            };
        }

        if (errorStyle == null)
        {
            errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red }
            };
        }

        if (successStyle == null)
        {
            successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.green }
            };
        }
    }

    /// <summary>
    /// 绘制战术挂具分析信息
    /// </summary>
    public static void DrawTacticalRigAnalysis(TactiaclRigItemGrid tacticalRig)
    {
        if (tacticalRig == null) return;

        InitializeStyles();

        string rigKey = $"tactical_rig_{tacticalRig.GetInstanceID()}";

        EditorGUILayout.BeginVertical("box");

        // 战术挂具标题
        EditorGUILayout.LabelField($"? 战术挂具分析: {tacticalRig.name}", headerStyle);

        // 获取网格检测器信息
        var gridInfo = tacticalRig.GetGridDetectorInfo();
        
        // 获取战术挂具配置信息
        var configInfo = tacticalRig.GetTacticalRigConfigInfo();
        
        // 获取插槽分析信息
        var slotAnalysis = tacticalRig.GetSlotAnalysis();
        
        // 获取负载平衡信息
        var loadBalance = tacticalRig.GetLoadBalanceInfo();

        // 基础配置信息
        DrawBasicConfiguration(gridInfo, configInfo);

        // 插槽效率分析
        DrawSlotEfficiencyAnalysis(slotAnalysis, rigKey);

        // 负载平衡分析
        DrawLoadBalanceAnalysis(loadBalance, rigKey);

        // 注意：配置建议和性能指标功能已移除，可根据需要重新实现

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制基础配置信息
    /// </summary>
    private static void DrawBasicConfiguration(InventorySystem.Grid.GridDetectorInfo gridInfo, TacticalRigConfigInfo configInfo)
    {
        EditorGUILayout.LabelField("? 基础配置", subHeaderStyle);

        EditorGUI.indentLevel++;

        // 插槽配置
        EditorGUILayout.LabelField($"总插槽数: {gridInfo.totalCells}", infoStyle);
        EditorGUILayout.LabelField($"已占用插槽: {gridInfo.occupiedCellsCount}", infoStyle);
        EditorGUILayout.LabelField($"可用插槽: {gridInfo.availableCells}", infoStyle);

        // 容量信息
        EditorGUILayout.LabelField($"网格尺寸: {gridInfo.gridSize.x} × {gridInfo.gridSize.y}", infoStyle);
        EditorGUILayout.LabelField($"已放置物品: {gridInfo.placedItemsCount}", infoStyle);
        
        // 占用率显示
        float occupancyRate = gridInfo.occupancyRate * 100f;
        Color occupancyColor = GetOccupancyColor(occupancyRate);

        GUI.color = occupancyColor;
        EditorGUILayout.LabelField($"占用率: {occupancyRate:F1}%", infoStyle);
        GUI.color = Color.white;
        
        // 配置评分
        if (configInfo != null)
        {
            EditorGUILayout.LabelField($"配置评分: {configInfo.configurationScore:F1}/1.0", infoStyle);
            EditorGUILayout.LabelField($"战术效率: {configInfo.tacticalEfficiency:F1}/1.0", infoStyle);
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);
    }

    /// <summary>
    /// 绘制插槽效率分析
    /// </summary>
    private static void DrawSlotEfficiencyAnalysis(TacticalRigSlotAnalysis slotAnalysis, string rigKey)
    {
        string foldoutKey = $"{rigKey}_slot_efficiency";
        bool showSlotEfficiency = GetFoldoutState(foldoutKey, true);

        showSlotEfficiency = EditorGUILayout.Foldout(showSlotEfficiency, "? 插槽效率分析", true);
        SetFoldoutState(foldoutKey, showSlotEfficiency);

        if (showSlotEfficiency)
        {
            EditorGUI.indentLevel++;

            if (slotAnalysis != null)
            {
                // 插槽使用统计
                EditorGUILayout.LabelField("插槽使用统计:", subHeaderStyle);
                EditorGUILayout.LabelField($"  总插槽数: {slotAnalysis.totalSlots}", infoStyle);
                EditorGUILayout.LabelField($"  已使用插槽: {slotAnalysis.usedSlots}", infoStyle);
                
                float utilizationRate = slotAnalysis.totalSlots > 0 ? (float)slotAnalysis.usedSlots / slotAnalysis.totalSlots * 100f : 0f;
                Color utilizationColor = GetUtilizationColor(utilizationRate);
                
                GUI.color = utilizationColor;
                EditorGUILayout.LabelField($"  整体利用率: {utilizationRate:F1}%", infoStyle);
                GUI.color = Color.white;

                // 热点插槽
                if (slotAnalysis.hotSpots != null && slotAnalysis.hotSpots.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("高效率插槽 (热点):", subHeaderStyle);
                    foreach (var hotSpot in slotAnalysis.hotSpots)
                    {
                        GUI.color = excellentColor;
                        EditorGUILayout.LabelField($"  位置 ({hotSpot.x}, {hotSpot.y})", infoStyle);
                        GUI.color = Color.white;
                    }
                }

                // 冷点插槽
                if (slotAnalysis.coldSpots != null && slotAnalysis.coldSpots.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("低效率插槽 (冷点):", subHeaderStyle);
                    foreach (var coldSpot in slotAnalysis.coldSpots)
                    {
                        GUI.color = criticalColor;
                        EditorGUILayout.LabelField($"  位置 ({coldSpot.x}, {coldSpot.y})", infoStyle);
                        GUI.color = Color.white;
                    }
                }

                // 效率评级
                string efficiencyGrade = GetEfficiencyGrade(utilizationRate);
                Color gradeColor = GetGradeColor(efficiencyGrade);

                GUI.color = gradeColor;
                EditorGUILayout.LabelField($"  效率评级: {efficiencyGrade}", infoStyle);
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField("无插槽分析数据", warningStyle);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);
    }

    /// <summary>
    /// 绘制负载平衡分析
    /// </summary>
    private static void DrawLoadBalanceAnalysis(TacticalRigLoadBalance loadBalance, string rigKey)
    {
        string foldoutKey = $"{rigKey}_load_balance";
        bool showLoadBalance = GetFoldoutState(foldoutKey, true);

        showLoadBalance = EditorGUILayout.Foldout(showLoadBalance, "?? 负载平衡分析", true);
        SetFoldoutState(foldoutKey, showLoadBalance);

        if (showLoadBalance)
        {
            EditorGUI.indentLevel++;

            if (loadBalance != null)
            {
                // 重量分布
                EditorGUILayout.LabelField("重量分布:", subHeaderStyle);
                EditorGUILayout.LabelField($"  总重量: {loadBalance.totalWeight:F2}kg", infoStyle);
                
                // 计算平均重量和最大单项重量（基于重量分布数据）
                float averageWeight = 0f;
                float maxItemWeight = 0f;
                if (loadBalance.weightDistribution != null && loadBalance.weightDistribution.Count > 0)
                {
                    averageWeight = loadBalance.totalWeight / loadBalance.weightDistribution.Count;
                    maxItemWeight = loadBalance.weightDistribution.Values.Max();
                }
                
                EditorGUILayout.LabelField($"  平均重量: {averageWeight:F2}kg", infoStyle);
                EditorGUILayout.LabelField($"  最大单项重量: {maxItemWeight:F2}kg", infoStyle);

                // 平衡评分
                Color balanceColor = GetBalanceColor(loadBalance.balanceScore);
                GUI.color = balanceColor;
                EditorGUILayout.LabelField($"  平衡评分: {loadBalance.balanceScore:F1}/100", infoStyle);
                GUI.color = Color.white;

                // 重心分析
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("重心分析:", subHeaderStyle);
                EditorGUILayout.LabelField($"  重心位置: ({loadBalance.centerOfMass.x:F2}, {loadBalance.centerOfMass.y:F2})", infoStyle);

                // 重量分布区域
                if (loadBalance.weightDistribution != null && loadBalance.weightDistribution.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("重量分布区域:", subHeaderStyle);
                    foreach (var distribution in loadBalance.weightDistribution)
                    {
                        float percentage = loadBalance.totalWeight > 0 ? (distribution.Value / loadBalance.totalWeight) * 100f : 0f;
                        Color loadColor = GetLoadColor(percentage);

                        GUI.color = loadColor;
                        EditorGUILayout.LabelField($"  {distribution.Key}: {distribution.Value:F2}kg ({percentage:F1}%)", infoStyle);
                        GUI.color = Color.white;
                    }
                }

                // 负载建议
                if (loadBalance.balanceRecommendations != null && loadBalance.balanceRecommendations.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("负载建议:", subHeaderStyle);
                    GUI.color = new Color(0.8f, 0.9f, 1f);
                    foreach (var recommendation in loadBalance.balanceRecommendations)
                    {
                        EditorGUILayout.LabelField($"  ? {recommendation}", infoStyle);
                    }
                    GUI.color = Color.white;
                }
            }
            else
            {
                EditorGUILayout.LabelField("无负载平衡数据", warningStyle);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);
    }



    // 辅助方法
    private static bool GetFoldoutState(string key, bool defaultValue)
    {
        if (!foldoutStates.ContainsKey(key))
        {
            foldoutStates[key] = defaultValue;
        }
        return foldoutStates[key];
    }

    private static void SetFoldoutState(string key, bool value)
    {
        foldoutStates[key] = value;
    }

    private static Color GetOccupancyColor(float occupancy)
    {
        if (occupancy >= 90f) return criticalColor;
        if (occupancy >= 75f) return poorColor;
        if (occupancy >= 50f) return averageColor;
        if (occupancy >= 25f) return goodColor;
        return excellentColor;
    }

    private static Color GetEfficiencyColor(float efficiency)
    {
        if (efficiency >= 85f) return excellentColor;
        if (efficiency >= 70f) return goodColor;
        if (efficiency >= 55f) return averageColor;
        if (efficiency >= 40f) return poorColor;
        return criticalColor;
    }

    private static Color GetUtilizationColor(float utilization)
    {
        if (utilization >= 80f) return excellentColor;
        if (utilization >= 65f) return goodColor;
        if (utilization >= 50f) return averageColor;
        if (utilization >= 35f) return poorColor;
        return criticalColor;
    }

    private static Color GetBalanceColor(float balance)
    {
        if (balance >= 85f) return excellentColor;
        if (balance >= 70f) return goodColor;
        if (balance >= 55f) return averageColor;
        if (balance >= 40f) return poorColor;
        return criticalColor;
    }

    private static Color GetLoadColor(float percentage)
    {
        if (percentage >= 40f) return criticalColor;
        if (percentage >= 30f) return poorColor;
        if (percentage >= 20f) return averageColor;
        if (percentage >= 10f) return goodColor;
        return excellentColor;
    }

    private static Color GetDensityColor(float density)
    {
        if (density >= 0.8f) return excellentColor;
        if (density >= 0.6f) return goodColor;
        if (density >= 0.4f) return averageColor;
        if (density >= 0.2f) return poorColor;
        return criticalColor;
    }

    private static Color GetComplexityColor(int complexity)
    {
        if (complexity <= 3) return excellentColor;
        if (complexity <= 5) return goodColor;
        if (complexity <= 7) return averageColor;
        if (complexity <= 9) return poorColor;
        return criticalColor;
    }

    private static Color GetGradeColor(string grade)
    {
        switch (grade)
        {
            case "优秀": return excellentColor;
            case "良好": return goodColor;
            case "一般": return averageColor;
            case "较差": return poorColor;
            case "很差": return criticalColor;
            default: return Color.white;
        }
    }

    private static string GetEfficiencyGrade(float efficiency)
    {
        if (efficiency >= 85f) return "优秀";
        if (efficiency >= 70f) return "良好";
        if (efficiency >= 55f) return "一般";
        if (efficiency >= 40f) return "较差";
        return "很差";
    }


}