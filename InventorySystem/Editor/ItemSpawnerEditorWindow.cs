using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InventorySystem;

public class ItemSpawnerEditorWindow : EditorWindow
{
    private Vector2 _scroll;
    private List<ItemGrid> _grids = new List<ItemGrid>();
    private int _selectedGridIndex = -1;
    private List<ItemDataSO> _allItems = new List<ItemDataSO>();
    private string _search = string.Empty;
    private float _cellBaseSize = 32f; // 每个网格单元的基准像素

    [MenuItem("Tools/物品生成器")]
    public static void ShowWindow()
    {
        var win = GetWindow<ItemSpawnerEditorWindow>("物品生成器");
        win.minSize = new Vector2(700, 480);
        win.RefreshContext();
    }

    private void OnEnable()
    {
        RefreshContext();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnHierarchyChanged()
    {
        // 场景网格变化时自动刷新网格列表
        RefreshGridsOnly();
        Repaint();
    }

    private void RefreshContext()
    {
        RefreshGridsOnly();
        // 加载所有 ItemDataSO（全项目）
        _allItems = LoadAllItemDataAssets();
        // 按ID排序，若无则按名称
        _allItems = _allItems
            .OrderBy(i => i != null ? i.id : int.MaxValue)
            .ThenBy(i => i != null ? i.itemName : string.Empty)
            .ToList();
    }

    private void RefreshGridsOnly()
    {
        _grids = FindObjectsOfType<ItemGrid>(true).ToList();
        // 保持选择
        if (_grids.Count == 0) _selectedGridIndex = -1;
        else if (_selectedGridIndex < 0 || _selectedGridIndex >= _grids.Count) _selectedGridIndex = 0;
    }

    private List<ItemDataSO> LoadAllItemDataAssets()
    {
        var list = new List<ItemDataSO>();
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(ItemDataSO)}");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
            if (so != null) list.Add(so);
        }
        return list;
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_grids == null || _grids.Count == 0)
        {
            EditorGUILayout.HelpBox("未检测到场景中的任何 ItemGrid。请打开背包/装备界面或确保相应网格已实例化。", MessageType.Info);
            return;
        }

        if (_selectedGridIndex < 0 || _selectedGridIndex >= _grids.Count)
        {
            EditorGUILayout.HelpBox("请选择一个目标网格。", MessageType.Warning);
            return;
        }

        // 过滤与排序
        IEnumerable<ItemDataSO> items = _allItems;
        if (!string.IsNullOrEmpty(_search))
        {
            string s = _search.ToLower();
            items = items.Where(i => i != null && (
                (i.itemName != null && i.itemName.ToLower().Contains(s)) ||
                (i.shortName != null && i.shortName.ToLower().Contains(s)) ||
                i.id.ToString().Contains(s)
            ));
        }

        // 网格展示
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawItemGrid(items.ToList());
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("生成物品需要在运行模式(Play)下执行。当前为编辑模式，仅预览。", MessageType.Info);
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("目标网格:", GUILayout.Width(60));
        string[] gridNames = _grids.Select(g => FormatGridDisplayName(g)).ToArray();
        int newIdx = EditorGUILayout.Popup(_selectedGridIndex, gridNames, EditorStyles.toolbarPopup, GUILayout.Width(320));
        if (newIdx != _selectedGridIndex)
        {
            _selectedGridIndex = newIdx;
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("搜索:", GUILayout.Width(40));
        _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarTextField, GUILayout.Width(220));

        GUILayout.FlexibleSpace();
        GUILayout.Label("单元像素:", GUILayout.Width(60));
        _cellBaseSize = EditorGUILayout.Slider(_cellBaseSize, 20f, 64f, GUILayout.Width(200));

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshContext();
        }
        EditorGUILayout.EndHorizontal();
    }

    private string FormatGridDisplayName(ItemGrid g)
    {
        if (g == null) return "(null)";
        string name = !string.IsNullOrEmpty(g.GridName) ? g.GridName : g.name;
        return $"{name}  [{g.GridType}]  {g.gridSizeWidth}x{g.gridSizeHeight}";
    }

    private void DrawItemGrid(List<ItemDataSO> items)
    {
        if (items == null || items.Count == 0)
        {
            EditorGUILayout.HelpBox("没有可显示的物品。", MessageType.Info);
            return;
        }

        float viewWidth = position.width - 30f;
        float x = 0f;
        float y = 0f;
        float lineHeight = 0f;

        Rect contentRect = GUILayoutUtility.GetRect(viewWidth, 0f);
        contentRect.height = 0f; // 我们将逐项绘制并扩展高度

        foreach (var data in items)
        {
            if (data == null) continue;
            // 计算展示尺寸：按物品宽高以单元像素缩放
            float w = Mathf.Max(1, data.width) * _cellBaseSize;
            float h = Mathf.Max(1, data.height) * _cellBaseSize;

            // 自动换行
            if (x + w > viewWidth)
            {
                x = 0f;
                y += lineHeight + 10f;
                lineHeight = 0f;
            }

            Rect r = new Rect(10 + x, 10 + y, w, h);
            DrawItemCard(r, data);

            x += w + 10f;
            lineHeight = Mathf.Max(lineHeight, h);
        }

        // 占位以撑开滚动区域
        GUILayout.Space(y + lineHeight + 30f);
    }

    private void DrawItemCard(Rect r, ItemDataSO data)
    {
        // 背景
        EditorGUI.DrawRect(r, data != null ? data.backgroundColor : new Color(0.2f, 0.2f, 0.2f, 1f));

        // 图标
        if (data != null && data.itemIcon != null)
        {
            Rect iconRect = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 24);
            GUI.DrawTexture(iconRect, data.itemIcon.texture, ScaleMode.ScaleToFit);
        }

        // 底部按钮
        Rect btn = new Rect(r.x, r.yMax - 20, r.width, 20);
        string label = data != null ? $"[{data.id}] {data.GetDisplayName()}" : "(null)";
        if (GUI.Button(btn, label))
        {
            TrySpawnIntoSelectedGrid(data);
        }
    }

    private void TrySpawnIntoSelectedGrid(ItemDataSO data)
    {
        if (data == null) return;
        if (_selectedGridIndex < 0 || _selectedGridIndex >= _grids.Count)
        {
            ShowNotification(new GUIContent("请选择目标网格"));
            return;
        }

        var grid = _grids[_selectedGridIndex];
        if (grid == null)
        {
            ShowNotification(new GUIContent("目标网格无效"));
            return;
        }

        if (!Application.isPlaying)
        {
            ShowNotification(new GUIContent("请在运行模式下生成物品"));
            return;
        }

        // 生成实例并放入网格
        SpawnItemInGridRuntime(grid, data);
    }

    private void SpawnItemInGridRuntime(ItemGrid grid, ItemDataSO data)
    {
        try
        {
            // 创建与 ItemPrefabGenerator 一致的结构（精简版）
            float gridSize = 64f;
            Vector2 itemSize = new Vector2(Mathf.Max(1, data.width) * gridSize, Mathf.Max(1, data.height) * gridSize);

            GameObject root = new GameObject(data.itemName);
            root.layer = 5; // UI
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = itemSize;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            // 背景
            GameObject bg = new GameObject("ItemBackground");
            bg.transform.SetParent(root.transform);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.sizeDelta = itemSize;
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            var bgImg = bg.AddComponent<Image>();
            var bgColor = data.backgroundColor; bgColor.a = 0.8f; bgImg.color = bgColor;

            // 图标
            GameObject icon = new GameObject("ItemIcon");
            icon.transform.SetParent(root.transform);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.sizeDelta = itemSize;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            var iconImg = icon.AddComponent<Image>();
            iconImg.sprite = data.itemIcon;
            iconImg.color = Color.white;

            // 文本
            GameObject textGO = new GameObject("ItemText");
            textGO.transform.SetParent(root.transform);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(Mathf.Max(itemSize.x * 0.4f, 24f), Mathf.Max(itemSize.y * 0.3f, 16f));
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(1f, 0f);
            textRect.anchoredPosition = new Vector2(itemSize.x / 2f - 3f, -itemSize.y / 2f + 3f);
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = string.Empty; // 初始让 ItemDataReader 再决定
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.BottomRight;
            tmp.raycastTarget = false;

            // 逻辑组件
            var reader = root.AddComponent<ItemDataReader>();
            var item = root.AddComponent<Item>();
            root.AddComponent<DraggableItem>();
            root.AddComponent<ItemHighlight>();
            root.AddComponent<InventoryItemRightClickHandler>();

            // 通过反射设置私有UI引用
            var bgField = typeof(ItemDataReader).GetField("backgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconField = typeof(ItemDataReader).GetField("iconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var textField = typeof(ItemDataReader).GetField("displayText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bgField != null) bgField.SetValue(reader, bgImg);
            if (iconField != null) iconField.SetValue(reader, iconImg);
            if (textField != null) textField.SetValue(reader, tmp);

            // 绑定数据（堆叠数量稍后在放置成功后设置并保存）
            reader.SetItemData(data);

            // 将物体作为待放置的UI元素，父节点暂时设为目标网格
            root.transform.SetParent(grid.transform, false);

            // 尝试在网格内找到可放置位置
            bool placed = TryPlaceInGrid(grid, item);
            if (!placed)
            {
                // 放置失败则销毁
                Debug.LogWarning($"[ItemSpawner] 网格 {grid.GridName} 无可用空间放置 {data.itemName}");
                DestroyImmediate(root);
                return;
            }

            // 放置成功后，若支持堆叠则设置为最大堆叠并立即持久化（避免Start重置为1）
            if (data.IsStackable())
            {
                reader.SetStack(data.maxStack);
                reader.SaveRuntimeToES3();
            }

            Debug.Log($"[ItemSpawner] 已生成 {data.itemName} 到 {grid.GridName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemSpawner] 生成失败: {e.Message}\n{e}");
        }
    }

    private bool TryPlaceInGrid(ItemGrid grid, Item item)
    {
        if (grid == null || item == null) return false;
        for (int x = 0; x < grid.gridSizeWidth; x++)
        {
            for (int y = 0; y < grid.gridSizeHeight; y++)
            {
                if (grid.PlaceItem(item, x, y))
                {
                    return true;
                }
            }
        }
        return false;
    }
}


