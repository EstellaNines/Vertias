using UnityEditor;
using UnityEngine;

// 自定义检查器：根据中立敌人类型（站岗/巡逻）切换字段显示
[CustomEditor(typeof(NeutralEnemy))]
public class NeutralEnemyEditor : Editor
{
    private SerializedProperty healthProp;
    private SerializedProperty isHostileProp;
    private SerializedProperty onHurtProp;
    private SerializedProperty onDeathProp;
    private SerializedProperty onBecomeHostileProp;

    private SerializedProperty isGuardProp;
    private SerializedProperty neutralTypeProp;

    private SerializedProperty hostileIndicatorProp;
    private SerializedProperty hostileIndicatorDurationProp;

    private SerializedProperty eyeProp;
    private SerializedProperty hearingRadiusProp;
    private SerializedProperty playerLayerProp;
    private SerializedProperty defaultFovFacingProp;

    private SerializedProperty enableGuardScanProp;
    private SerializedProperty scanAngleMinProp;
    private SerializedProperty scanAngleMaxProp;
    private SerializedProperty scanSpeedHzProp;

    private SerializedProperty weaponProp;
    private SerializedProperty weaponSpriteProp;
    private SerializedProperty weaponControllerProp;
    private SerializedProperty firePointProp;
    private SerializedProperty enemyBulletPoolProp;

    private SerializedProperty moveSpeedProp;
    private SerializedProperty patrolPointsProp;
    private SerializedProperty pathUpdateIntervalProp;
    private SerializedProperty reachDistanceProp;
    private SerializedProperty idleAfterPointTimeProp;

    private GUIStyle headerStyle;

    private void OnEnable()
    {
        healthProp = serializedObject.FindProperty("Health");
        isHostileProp = serializedObject.FindProperty("isHostile");
        onHurtProp = serializedObject.FindProperty("OnHurt");
        onDeathProp = serializedObject.FindProperty("OnDeath");
        onBecomeHostileProp = serializedObject.FindProperty("OnBecomeHostile");

        isGuardProp = serializedObject.FindProperty("isGuard");
        neutralTypeProp = serializedObject.FindProperty("neutralType");

        hostileIndicatorProp = serializedObject.FindProperty("hostileIndicator");
        hostileIndicatorDurationProp = serializedObject.FindProperty("hostileIndicatorDuration");

        eyeProp = serializedObject.FindProperty("eye");
        hearingRadiusProp = serializedObject.FindProperty("hearingRadius");
        playerLayerProp = serializedObject.FindProperty("playerLayer");
        defaultFovFacingProp = serializedObject.FindProperty("defaultFovFacing");

        enableGuardScanProp = serializedObject.FindProperty("enableGuardScan");
        scanAngleMinProp = serializedObject.FindProperty("scanAngleMin");
        scanAngleMaxProp = serializedObject.FindProperty("scanAngleMax");
        scanSpeedHzProp = serializedObject.FindProperty("scanSpeedHz");

        weaponProp = serializedObject.FindProperty("weapon");
        weaponSpriteProp = serializedObject.FindProperty("weaponSprite");
        weaponControllerProp = serializedObject.FindProperty("weaponController");
        firePointProp = serializedObject.FindProperty("firePoint");
        enemyBulletPoolProp = serializedObject.FindProperty("enemyBulletPool");

        moveSpeedProp = serializedObject.FindProperty("MoveSpeed");
        patrolPointsProp = serializedObject.FindProperty("patrolPoints");
        pathUpdateIntervalProp = serializedObject.FindProperty("pathUpdateInterval");
        reachDistanceProp = serializedObject.FindProperty("reachDistance");
        idleAfterPointTimeProp = serializedObject.FindProperty("idleAfterPointTime");

        // 不在 OnEnable 里初始化，避免 EditorStyles 在某些导入阶段为 null
        headerStyle = null;
        // 小队模式已移除
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 延迟初始化标题样式，避免 EditorStyles 在 OnEnable 阶段为 null
        if (headerStyle == null)
        {
            var baseStyle = EditorStyles.boldLabel != null ? EditorStyles.boldLabel : EditorStyles.label;
            headerStyle = new GUIStyle(baseStyle) { fontSize = 12 };
        }

        // 基础
        DrawHeader("基础 Base");
        EditorGUILayout.PropertyField(healthProp);
        EditorGUILayout.PropertyField(isHostileProp);

        // 类型
        EditorGUILayout.Space();
        DrawHeader("类型判定 Type");
        EditorGUILayout.PropertyField(neutralTypeProp, new GUIContent("中立敌人类型"));
        // 同步 bool 与 enum（在编辑器层面做一次保护）
        var typeEnum = (NeutralEnemy.NeutralType)neutralTypeProp.enumValueIndex;
        bool guard = typeEnum == NeutralEnemy.NeutralType.Guard;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(isGuardProp, new GUIContent("是否站岗型"));
        }

        // 敌对提示
        EditorGUILayout.Space();
        DrawHeader("敌对提示 Hostile Indicator");
        EditorGUILayout.PropertyField(hostileIndicatorProp);
        EditorGUILayout.PropertyField(hostileIndicatorDurationProp);

        // 感知
        EditorGUILayout.Space();
        DrawHeader("感知 Perception");
        EditorGUILayout.PropertyField(eyeProp);
        EditorGUILayout.PropertyField(hearingRadiusProp);
        EditorGUILayout.PropertyField(playerLayerProp);
        EditorGUILayout.PropertyField(defaultFovFacingProp);

        // 小队模式已移除

        // 站岗扫视参数（仅站岗型显示）
        if (guard)
        {
            EditorGUILayout.Space();
            DrawHeader("站岗扫视 Guard Scan");
            EditorGUILayout.PropertyField(enableGuardScanProp);
            using (new EditorGUI.DisabledScope(!enableGuardScanProp.boolValue))
            {
                EditorGUILayout.PropertyField(scanAngleMinProp);
                EditorGUILayout.PropertyField(scanAngleMaxProp);
                EditorGUILayout.PropertyField(scanSpeedHzProp);
            }
        }

        // 武器
        EditorGUILayout.Space();
        DrawHeader("武器 Weapon");
        EditorGUILayout.PropertyField(weaponControllerProp);
        EditorGUILayout.PropertyField(weaponProp);
        EditorGUILayout.PropertyField(weaponSpriteProp);
        EditorGUILayout.PropertyField(firePointProp);
        EditorGUILayout.PropertyField(enemyBulletPoolProp);

        // 移动/巡逻（站岗型隐藏）
        if (!guard)
        {
            EditorGUILayout.Space();
            DrawHeader("移动/巡逻 Movement & Patrol");
            EditorGUILayout.PropertyField(moveSpeedProp);
            EditorGUILayout.PropertyField(patrolPointsProp, true);
            EditorGUILayout.PropertyField(pathUpdateIntervalProp);
            EditorGUILayout.PropertyField(reachDistanceProp);
            EditorGUILayout.PropertyField(idleAfterPointTimeProp);
        }
        else
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("站岗型：移动/巡逻设置已隐藏。切换为巡逻型后可见。", MessageType.Info);
        }

        // 事件
        EditorGUILayout.Space();
        DrawHeader("事件 Events");
        EditorGUILayout.PropertyField(onHurtProp);
        EditorGUILayout.PropertyField(onDeathProp);
        EditorGUILayout.PropertyField(onBecomeHostileProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.LabelField(text, headerStyle);
        var rect = GUILayoutUtility.GetRect(1, 2);
        EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.15f));
    }
}


