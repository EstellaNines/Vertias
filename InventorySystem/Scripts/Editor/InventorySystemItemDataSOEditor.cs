using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(InventorySystemItemDataSO))]
public class InventorySystemItemDataSOEditor : Editor
{
    private const float CELL_SIZE = 30f;
    private const float GRID_SPACING = 2f;
    
    public override void OnInspectorGUI()
    {
        InventorySystemItemDataSO itemData = (InventorySystemItemDataSO)target;
        
        serializedObject.Update();
        
        // 绘制基本信息
        DrawBasicInfo(itemData);
        
        // 绘制类别相关的字段
        DrawCategorySpecificFields(itemData);
        
        // 绘制网格可视化
        DrawGridVisualization(itemData);
        
        // 如果是容器类型，绘制内部网格
        if (itemData.IsContainer())
        {
            DrawContainerVisualization(itemData);
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // 如果有修改，标记为脏数据
        if (GUI.changed)
        {
            EditorUtility.SetDirty(itemData);
        }
    }
    
    private void DrawBasicInfo(InventorySystemItemDataSO itemData)
    {
        EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
        
        // ID
        itemData.id = EditorGUILayout.IntField("ID", itemData.id);
        
        // 物品名称
        itemData.itemName = EditorGUILayout.TextField("物品名称", itemData.itemName);
        
        // 物品类别下拉菜单
        itemData.itemCategory = (InventorySystemItemCategory)EditorGUILayout.EnumPopup("物品类别", itemData.itemCategory);
        
        EditorGUILayout.Space(5);
        
        // 网格尺寸
        EditorGUILayout.LabelField("网格尺寸", EditorStyles.boldLabel);
        itemData.height = EditorGUILayout.IntField("高度", itemData.height);
        itemData.width = EditorGUILayout.IntField("宽度", itemData.width);
        
        EditorGUILayout.Space(5);
        
        // 珍贵程度
        EditorGUILayout.LabelField("珍贵程度", EditorStyles.boldLabel);
        itemData.rarity = EditorGUILayout.TextField("稀有度", itemData.rarity);
        
        // 背景颜色
        itemData.backgroundColor = EditorGUILayout.TextField("背景颜色", itemData.backgroundColor);
        
        // 物品图标
        itemData.itemIcon = (Sprite)EditorGUILayout.ObjectField("物品图标", itemData.itemIcon, typeof(Sprite), false);
        
        EditorGUILayout.Space(10);
    }
    
    private void DrawCategorySpecificFields(InventorySystemItemDataSO itemData)
    {
        EditorGUILayout.LabelField("类别特定信息", EditorStyles.boldLabel);
        
        switch (itemData.itemCategory)
        {
            case InventorySystemItemCategory.Backpack:
            case InventorySystemItemCategory.TacticalRig:
                EditorGUILayout.LabelField("容量信息", EditorStyles.miniBoldLabel);
                itemData.CellH = EditorGUILayout.IntField("内部宽度", itemData.CellH);
                itemData.CellV = EditorGUILayout.IntField("内部高度", itemData.CellV);
                break;
                
            case InventorySystemItemCategory.Weapon:
            case InventorySystemItemCategory.Ammunition:
                EditorGUILayout.LabelField("武器/弹药信息", EditorStyles.miniBoldLabel);
                itemData.BulletType = EditorGUILayout.TextField("子弹类型", itemData.BulletType);
                break;
                
            case InventorySystemItemCategory.Helmet:
            case InventorySystemItemCategory.Armor:
                EditorGUILayout.LabelField("防护装备信息", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("防护装备的特殊属性可以在这里添加", MessageType.Info);
                break;
                
            case InventorySystemItemCategory.Food:
            case InventorySystemItemCategory.Drink:
            case InventorySystemItemCategory.Healing:
            case InventorySystemItemCategory.Hemostatic:
            case InventorySystemItemCategory.Sedative:
                EditorGUILayout.LabelField("消耗品信息", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("消耗品的特殊属性可以在这里添加", MessageType.Info);
                break;
                
            case InventorySystemItemCategory.Intelligence:
            case InventorySystemItemCategory.Currency:
                EditorGUILayout.LabelField("特殊物品信息", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("特殊物品的属性可以在这里添加", MessageType.Info);
                break;
        }
        
        EditorGUILayout.Space(10);
    }
    
    private void DrawGridVisualization(InventorySystemItemDataSO itemData)
    {
        // 添加分隔线
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // 绘制网格可视化标题
        EditorGUILayout.LabelField("物品网格可视化", EditorStyles.boldLabel);
        
        // 显示网格信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"尺寸: {itemData.width} × {itemData.height}", GUILayout.Width(120));
        EditorGUILayout.LabelField($"总面积: {itemData.GetGridArea()} 格", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 验证网格尺寸
        if (!itemData.IsValidGridSize())
        {
            EditorGUILayout.HelpBox("网格尺寸必须大于0！", MessageType.Error);
            return;
        }
        
        // 限制最大显示尺寸以避免界面过大
        int maxDisplayWidth = Mathf.Min(itemData.width, 10);
        int maxDisplayHeight = Mathf.Min(itemData.height, 10);
        
        if (itemData.width > 10 || itemData.height > 10)
        {
            EditorGUILayout.HelpBox($"网格过大，仅显示 {maxDisplayWidth} × {maxDisplayHeight} 的预览", MessageType.Info);
        }
        
        // 计算网格总尺寸
        float totalWidth = maxDisplayWidth * CELL_SIZE + (maxDisplayWidth - 1) * GRID_SPACING;
        float totalHeight = maxDisplayHeight * CELL_SIZE + (maxDisplayHeight - 1) * GRID_SPACING;
        
        // 创建网格绘制区域
        Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        
        // 居中网格
        float startX = gridRect.x + (gridRect.width - totalWidth) * 0.5f;
        float startY = gridRect.y;
        
        // 绘制网格
        DrawGrid(startX, startY, maxDisplayWidth, maxDisplayHeight, itemData, false);
        
        // 显示颜色信息
        if (!string.IsNullOrEmpty(itemData.backgroundColor))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("背景颜色:", GUILayout.Width(80));
            Color bgColor = GetColorFromString(itemData.backgroundColor);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20)), bgColor);
            EditorGUILayout.LabelField(itemData.backgroundColor);
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawContainerVisualization(InventorySystemItemDataSO itemData)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // 绘制容器内部网格标题
        EditorGUILayout.LabelField("容器内部网格", EditorStyles.boldLabel);
        
        // 显示容器信息
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"内部尺寸: {itemData.CellH} × {itemData.CellV}", GUILayout.Width(150));
        EditorGUILayout.LabelField($"容量: {itemData.GetContainerArea()} 格", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 验证容器尺寸
        if (!itemData.IsValidContainerSize())
        {
            EditorGUILayout.HelpBox("容器尺寸必须大于0！", MessageType.Error);
            return;
        }
        
        // 限制最大显示尺寸
        int maxDisplayWidth = Mathf.Min(itemData.CellH, 12);
        int maxDisplayHeight = Mathf.Min(itemData.CellV, 12);
        
        if (itemData.CellH > 12 || itemData.CellV > 12)
        {
            EditorGUILayout.HelpBox($"容器过大，仅显示 {maxDisplayWidth} × {maxDisplayHeight} 的预览", MessageType.Info);
        }
        
        // 计算容器网格总尺寸
        float totalWidth = maxDisplayWidth * CELL_SIZE + (maxDisplayWidth - 1) * GRID_SPACING;
        float totalHeight = maxDisplayHeight * CELL_SIZE + (maxDisplayHeight - 1) * GRID_SPACING;
        
        // 创建容器网格绘制区域
        Rect containerRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
        
        // 居中网格
        float startX = containerRect.x + (containerRect.width - totalWidth) * 0.5f;
        float startY = containerRect.y;
        
        // 绘制容器内部网格（使用白色背景和黑色边框）
        DrawContainerGrid(startX, startY, maxDisplayWidth, maxDisplayHeight);
    }
    
    private void DrawGrid(float startX, float startY, int width, int height, InventorySystemItemDataSO itemData, bool isContainer)
    {
        // 获取背景颜色
        Color backgroundColor = isContainer ? Color.white : GetColorFromString(itemData.backgroundColor);
        Color borderColor = Color.black;
        
        // 绘制每个网格单元
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cellX = startX + x * (CELL_SIZE + GRID_SPACING);
                float cellY = startY + y * (CELL_SIZE + GRID_SPACING);
                
                Rect cellRect = new Rect(cellX, cellY, CELL_SIZE, CELL_SIZE);
                
                // 绘制单元格背景
                EditorGUI.DrawRect(cellRect, backgroundColor);
                
                // 绘制单元格边框
                DrawRectBorder(cellRect, borderColor, 1f);
                
                // 在中心单元格显示坐标（仅对物品网格）
                if (!isContainer && x == width / 2 && y == height / 2)
                {
                    GUI.Label(cellRect, $"{x},{y}", GetCenteredStyle());
                }
            }
        }
        
        // 绘制整体边框
        Color outerBorderColor = isContainer ? Color.black : Color.red;
        Rect totalRect = new Rect(startX - 1, startY - 1, 
            width * (CELL_SIZE + GRID_SPACING) - GRID_SPACING + 2, 
            height * (CELL_SIZE + GRID_SPACING) - GRID_SPACING + 2);
        DrawRectBorder(totalRect, outerBorderColor, 2f);
    }
    
    private void DrawContainerGrid(float startX, float startY, int width, int height)
    {
        Color backgroundColor = Color.white;
        Color borderColor = Color.black;
        
        // 绘制每个网格单元
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cellX = startX + x * (CELL_SIZE + GRID_SPACING);
                float cellY = startY + y * (CELL_SIZE + GRID_SPACING);
                
                Rect cellRect = new Rect(cellX, cellY, CELL_SIZE, CELL_SIZE);
                
                // 绘制单元格背景（白色）
                EditorGUI.DrawRect(cellRect, backgroundColor);
                
                // 绘制单元格边框（黑色）
                DrawRectBorder(cellRect, borderColor, 1f);
            }
        }
        
        // 绘制整体边框（黑色，较粗）
        Rect totalRect = new Rect(startX - 2, startY - 2, 
            width * (CELL_SIZE + GRID_SPACING) - GRID_SPACING + 4, 
            height * (CELL_SIZE + GRID_SPACING) - GRID_SPACING + 4);
        DrawRectBorder(totalRect, borderColor, 3f);
    }
    
    private void DrawRectBorder(Rect rect, Color color, float thickness)
    {
        // 上边
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        // 下边
        EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
        // 左边
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        // 右边
        EditorGUI.DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
    }
    
    private GUIStyle GetCenteredStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 8;
        style.normal.textColor = Color.white;
        return style;
    }
    
    private Color GetColorFromString(string colorName)
    {
        if (string.IsNullOrEmpty(colorName))
            return new Color(0.8f, 0.8f, 0.8f, 0.7f);
            
        switch (colorName.ToLower())
        {
            case "blue":
                ColorUtility.TryParseHtmlString("#2d3c4b", out Color blue);
                return new Color(blue.r, blue.g, blue.b, 0.7f);
            case "violet":
            case "purple":
                ColorUtility.TryParseHtmlString("#583b80", out Color violet);
                return new Color(violet.r, violet.g, violet.b, 0.7f);
            case "yellow":
                ColorUtility.TryParseHtmlString("#80550d", out Color yellow);
                return new Color(yellow.r, yellow.g, yellow.b, 0.7f);
            case "red":
                ColorUtility.TryParseHtmlString("#350000", out Color red);
                return new Color(red.r, red.g, red.b, 0.7f);
            default:
                return new Color(0.8f, 0.8f, 0.8f, 0.7f);
        }
    }
}
#endif