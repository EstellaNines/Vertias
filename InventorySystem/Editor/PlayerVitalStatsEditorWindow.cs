using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 玩家状态（生命/饱食度/精神值）调节窗口。
/// Tools/Player Vital Stats 调出，支持 +1/+5/+10 与 -1/-5/-10，并动态显示进度。
/// </summary>
#if UNITY_EDITOR
public class PlayerVitalStatsEditorWindow : EditorWindow
{
    private PlayerVitalStats targetStats;
    private Color healthColor = new Color(0.8f, 0.2f, 0.2f);
    private Color hungerColor = new Color(0.9f, 0.7f, 0.2f);
    private Color mentalColor = new Color(0.3f, 0.6f, 0.9f);

    [MenuItem("Tools/Player Vital Stats")] 
    public static void Open()
    {
        var window = GetWindow<PlayerVitalStatsEditorWindow>(false, "Player Vital Stats", true);
        window.minSize = new Vector2(420, 320);
        window.Show();
    }

    private void OnEnable()
    {
        // 自动寻找一个 PlayerVitalStats
        if (targetStats == null)
        {
            targetStats = FindObjectOfType<PlayerVitalStats>();
        }
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        targetStats = (PlayerVitalStats)EditorGUILayout.ObjectField("Target Player", targetStats, typeof(PlayerVitalStats), true);

        if (targetStats == null)
        {
            EditorGUILayout.HelpBox("场景中未找到 PlayerVitalStats，请指定目标对象。", MessageType.Warning);
            if (GUILayout.Button("尝试查找 PlayerVitalStats"))
            {
                targetStats = FindObjectOfType<PlayerVitalStats>();
            }
            return;
        }

        DrawHealthSection("Health", healthColor);
        DrawClampedSection(
            "Hunger",
            () => targetStats.currentHunger,
            () => targetStats.maxHunger,
            v => { targetStats.SetHunger(v); MarkDirty(targetStats); },
            hungerColor
        );

        DrawClampedSection(
            "Mental",
            () => targetStats.currentMental,
            () => targetStats.maxMental,
            v => { targetStats.SetMental(v); MarkDirty(targetStats); },
            mentalColor
        );

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("提示：该窗口只在编辑器中可用。可在播放模式下实时调节玩家状态。", MessageType.Info);
    }

    private void DrawHealthSection(string label, Color barColor)
    {
        float current = targetStats.currentHealth;
        float max = targetStats.maxHealth;

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        float pct = Mathf.Clamp01(max > 0f ? current / max : 0f);

        Rect r = GUILayoutUtility.GetRect(1, 20);
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));
        Rect fill = new Rect(r.x, r.y, r.width * pct, r.height);
        EditorGUI.DrawRect(fill, barColor);
        GUI.Label(r, $"{current:0}/{max:0}  ({pct * 100f:0}%)", EditorStyles.whiteLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(60))) { targetStats.ApplyDamage(10f); MarkDirty(targetStats); }
        if (GUILayout.Button("-5", GUILayout.Width(60)))  { targetStats.ApplyDamage(5f); MarkDirty(targetStats); }
        if (GUILayout.Button("-1", GUILayout.Width(60)))  { targetStats.ApplyDamage(1f); MarkDirty(targetStats); }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+1", GUILayout.Width(60)))  { targetStats.Heal(1f); MarkDirty(targetStats); }
        if (GUILayout.Button("+5", GUILayout.Width(60)))  { targetStats.Heal(5f); MarkDirty(targetStats); }
        if (GUILayout.Button("+10", GUILayout.Width(60))) { targetStats.Heal(10f); MarkDirty(targetStats); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void DrawClampedSection(string label, System.Func<float> getCurrent, System.Func<float> getMax, System.Action<float> setValue, Color barColor)
    {
        float current = getCurrent();
        float max = getMax();

        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        float pct = Mathf.Clamp01(max > 0f ? current / max : 0f);

        Rect r = GUILayoutUtility.GetRect(1, 20);
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));
        Rect fill = new Rect(r.x, r.y, r.width * pct, r.height);
        EditorGUI.DrawRect(fill, barColor);
        GUI.Label(r, $"{current:0}/{max:0}  ({pct * 100f:0}%)", EditorStyles.whiteLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(60))) { setValue(Mathf.Clamp(current - 10f, 0f, max)); }
        if (GUILayout.Button("-5", GUILayout.Width(60)))  { setValue(Mathf.Clamp(current - 5f, 0f, max)); }
        if (GUILayout.Button("-1", GUILayout.Width(60)))  { setValue(Mathf.Clamp(current - 1f, 0f, max)); }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+1", GUILayout.Width(60)))  { setValue(Mathf.Clamp(current + 1f, 0f, max)); }
        if (GUILayout.Button("+5", GUILayout.Width(60)))  { setValue(Mathf.Clamp(current + 5f, 0f, max)); }
        if (GUILayout.Button("+10", GUILayout.Width(60))) { setValue(Mathf.Clamp(current + 10f, 0f, max)); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void MarkDirty(Object obj)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(obj);
        }
    }
}
#endif


