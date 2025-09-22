// This script is Editor-only. Place under an 'Editor' folder.
#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InventorySystem;

public class ItemViewerEditorWindow : EditorWindow
{
	private Vector2 _scroll;
	private float _cellSize = 96f;
	private float _cellPadding = 8f;
	private int _itemsPerRow = 8;
	private List<ItemDataSO> _items;
	private GUIStyle _idStyle;

	[MenuItem("Tools/Inventory/Item Viewer")] 
	public static void ShowWindow()
	{
		var win = GetWindow<ItemViewerEditorWindow>("Item Viewer");
		win.minSize = new Vector2(600, 400);
		win.RefreshItems();
	}

	private void OnEnable()
	{
		RefreshItems();
		_idStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
	}

	private void RefreshItems()
	{
		// 优先使用 AssetDatabase 搜索全部 ItemDataSO
		var guids = AssetDatabase.FindAssets("t:ItemDataSO");
		_items = new List<ItemDataSO>(guids.Length);
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var obj = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
			if (obj != null) _items.Add(obj);
		}
		// 兼容 Resources 路径（如果项目使用了 Resources）
		if (_items.Count == 0)
		{
			_items = Resources.LoadAll<ItemDataSO>(string.Empty).ToList();
		}
		// 按 id 排序
		_items = _items.OrderBy(i => i != null ? i.id : int.MaxValue).ToList();
		Repaint();
	}

	private void OnGUI()
	{
		using (new EditorGUILayout.HorizontalScope())
		{
			_itemsPerRow = Mathf.Clamp(EditorGUILayout.IntField("Items Per Row", _itemsPerRow, GUILayout.Width(220)), 2, 20);
			_cellSize = Mathf.Clamp(EditorGUILayout.FloatField("Cell Size", _cellSize, GUILayout.Width(200)), 48f, 160f);
			_cellPadding = Mathf.Clamp(EditorGUILayout.FloatField("Padding", _cellPadding, GUILayout.Width(180)), 0f, 24f);
			if (GUILayout.Button("Refresh", GUILayout.Width(100))) RefreshItems();
		}

		EditorGUILayout.Space(4);
		if (_items == null || _items.Count == 0)
		{
			EditorGUILayout.HelpBox("No ItemDataSO found.", MessageType.Info);
			return;
		}

		float totalWidth = position.width - 20f; // padding for scroll bar
		float cellFull = _cellSize + _cellPadding;
		int cols = Mathf.Max(2, _itemsPerRow);
		int rows = Mathf.CeilToInt(_items.Count / (float)cols);
		float gridHeight = rows * cellFull + _cellPadding;

		_scroll = EditorGUILayout.BeginScrollView(_scroll);
		Rect contentRect = GUILayoutUtility.GetRect(totalWidth, gridHeight);

		for (int index = 0; index < _items.Count; index++)
		{
			int row = index / cols;
			int col = index % cols;
			Rect cell = new Rect(
				contentRect.x + _cellPadding + col * cellFull,
				contentRect.y + _cellPadding + row * cellFull,
				_cellSize,
				_cellSize
			);

			DrawItemCell(cell, _items[index]);
		}

		EditorGUILayout.EndScrollView();
	}

	private void DrawItemCell(Rect rect, ItemDataSO item)
	{
		if (item == null)
		{
			EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
			return;
		}

		// 背景色
		Color bg = item.backgroundColor;
		if (bg.a <= 0f) bg.a = 1f;
		EditorGUI.DrawRect(rect, bg);

		// 绘制 ICON（保持居中与等比，且不超过单元格）
		if (item.itemIcon != null && item.itemIcon.texture != null)
		{
			Texture tex = item.itemIcon.texture;
			float pad = 6f;
			Rect iconRect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);
			GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit, true);
		}

		// 左上角显示 ID
		if (_idStyle != null)
		{
			GUI.Label(new Rect(rect.x + 4, rect.y + 2, rect.width - 8, 18), item.id.ToString(), _idStyle);
		}

		// 点击打开到 CheckInterfacePanel
		if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
		{
			Event.current.Use();
			var panel = FindObjectOfType<CheckInterfacePanelController>();
			if (panel != null)
			{
				panel.ShowForItem(item);
				Selection.activeObject = panel.gameObject;
				EditorGUIUtility.PingObject(panel.gameObject);
			}
			else
			{
				EditorUtility.DisplayDialog("Check Interface", "No CheckInterfacePanelController found in the open scene.", "OK");
			}
		}
	}
}
#endif


