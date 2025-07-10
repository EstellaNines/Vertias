using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponNotice : MonoBehaviour
{
    [Header("UI组件")]
    [FieldLabel("武器图标")] public Image weaponIcon; // 武器图标UI组件
    [FieldLabel("弹药信息文本")] public TextMeshProUGUI ammoInfoText; // 弹药信息文本

    [Header("引用组件")]
    [FieldLabel("玩家")] public Player player; // 玩家引用
    [FieldLabel("输入控制器")] public PlayerInputController inputController; // 玩家输入控制器

    [Header("设置")]
    [FieldLabel("默认透明度")][Range(0f, 1f)] public float defaultAlpha = 1f; // 武器图标默认透明度
    [FieldLabel("无武器时隐藏")] public bool hideWhenNoWeapon = true; // 无武器时隐藏武器图标

    // 内部状态
    private WeaponManager currentWeapon;
    private SpriteRenderer currentWeaponSprite;
    private bool isAmmoInfoVisible = false; // 弹药信息是否可见

    void Start()
    {
        // 自动查找玩家组件（如果未手动分配）
        if (player == null)
        {
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError("WeaponNotice: 未找到Player组件，请确保场景中有Player对象");
                return;
            }
        }

        // 自动查找输入控制器组件（如果未手动分配）
        if (inputController == null)
        {
            inputController = FindObjectOfType<PlayerInputController>();
            if (inputController == null)
            {
                Debug.LogError("WeaponNotice: 未找到PlayerInputController组件，请确保场景中有输入控制器对象");
                return;
            }
        }

        // 验证UI组件
        if (weaponIcon == null)
        {
            Debug.LogError("WeaponNotice: 武器图标UI组件未分配");
            return;
        }

        if (ammoInfoText == null)
        {
            Debug.LogError("WeaponNotice: 弹药信息文本组件未分配");
            return;
        }

        // 初始化UI状态
        InitializeUI();

        // 订阅武器检查输入事件
        SubscribeToWeaponInspectionInput();
    }

    void Update()
    {
        if (player != null)
        {
            UpdateWeaponIcon();

            // 如果弹药信息可见，则更新显示
            if (isAmmoInfoVisible)
            {
                UpdateAmmoInfoDisplay();
            }
        }
    }

    void OnDestroy()
    {
        // 取消订阅武器检查输入事件
        UnsubscribeFromWeaponInspectionInput();
    }

    // 订阅武器检查输入事件
    private void SubscribeToWeaponInspectionInput()
    {
        if (inputController != null)
        {
            inputController.onWeaponInspection += OnWeaponInspection;
        }
    }

    // 取消订阅武器检查输入事件
    private void UnsubscribeFromWeaponInspectionInput()
    {
        if (inputController != null)
        {
            inputController.onWeaponInspection -= OnWeaponInspection;
        }
    }

    // 武器检查回调
    private void OnWeaponInspection()
    {
        // 立即更新弹药信息显示状态
        UpdateAmmoInfoDisplay();

        // 确保弹药信息文本处于激活状态
        if (ammoInfoText != null && !ammoInfoText.gameObject.activeInHierarchy)
        {
            ammoInfoText.gameObject.SetActive(true);
        }

        Debug.Log("武器通知: 显示弹药信息");
    }

    // 初始化UI状态
    private void InitializeUI()
    {
        if (weaponIcon != null)
        {
            // 初始状态隐藏武器图标
            if (hideWhenNoWeapon)
            {
                weaponIcon.gameObject.SetActive(false);
            }
            else
            {
                Color iconColor = weaponIcon.color;
                iconColor.a = 0f;
                weaponIcon.color = iconColor;
            }
        }

        // 初始化弹药信息文本 - 默认激活显示
        if (ammoInfoText != null)
        {
            ammoInfoText.gameObject.SetActive(true);
            UpdateAmmoInfoDisplay(); // 初始化时更新一次显示内容
        }

        isAmmoInfoVisible = true; // 默认激活可见
    }

    // 刷新武器图标（外部调用）
    public void RefreshWeaponIcon()
    {
        if (player != null)
        {
            currentWeapon = null; // 重置缓存
            UpdateWeaponIcon();
        }
    }

    // 设置武器图标透明度
    public void SetIconAlpha(float alpha)
    {
        defaultAlpha = Mathf.Clamp01(alpha);

        if (weaponIcon != null && currentWeapon != null)
        {
            Color iconColor = weaponIcon.color;
            iconColor.a = defaultAlpha;
            weaponIcon.color = iconColor;
        }
    }

    // 获取当前武器名称
    public string GetCurrentWeaponName()
    {
        if (currentWeapon != null)
        {
            return currentWeapon.GetWeaponName();
        }
        return "无武器";
    }

    // 触发武器检查（外部调用）
    public void TriggerWeaponInspection()
    {
        OnWeaponInspection();
    }

    // 隐藏弹药信息
    public void HideAmmoInfo()
    {
        if (ammoInfoText != null)
        {
            ammoInfoText.gameObject.SetActive(false);
        }

        isAmmoInfoVisible = false;
    }

    // 强制显示弹药信息
    public void ForceShowAmmoInfo()
    {
        isAmmoInfoVisible = true;
        ShowAmmoInfo();
    }

    // 更新弹药信息显示
    private void UpdateAmmoInfoDisplay()
    {
        if (ammoInfoText == null) return;

        if (currentWeapon != null)
        {
            int currentAmmo = currentWeapon.GetCurrentAmmo();
            string ammoStatusText = GetAmmoStatusText(currentAmmo);

            // 设置弹药信息
            ammoInfoText.text = $"{ammoStatusText}";
        }
        else
        {
            ammoInfoText.text = "未装备武器";
        }
    }

    // 根据弹药数量获取状态文本
    private string GetAmmoStatusText(int ammoCount)
    {
        // 获取当前武器的最大弹夹容量
        int maxCapacity = currentWeapon != null ? currentWeapon.GetMagazineCapacity() : 30;
        
        if (ammoCount == 0)
        {
            return "Empty";
        }
        else if (ammoCount == maxCapacity)
        {
            return "Full";
        }
        else if (ammoCount > maxCapacity * 0.83f) // 大于83%容量
        {
            return "Nearly Full";
        }
        else if (ammoCount > maxCapacity * 0.5f && ammoCount < maxCapacity * 0.83f) // 50%-83%
        {
            return "About Half";
        }
        else if (ammoCount < maxCapacity * 0.5f && ammoCount >= maxCapacity * 0.17f) // 17%-50%
        {
            return "less than half";
        }
        else if (ammoCount < maxCapacity * 0.17f) // 小于17%容量
        {
            return "Almost Empty";
        }
        
        return ""; // 默认不显示任何文本
    }

    // 更新武器图标
    private void UpdateWeaponIcon()
    {
        // 检查玩家是否持有武器
        if (player.isWeaponInHand && player.Hand != null && player.Hand.childCount > 0)
        {
            // 获取Hand子对象中的武器
            Transform weaponTransform = player.Hand.GetChild(0);
            WeaponManager weaponManager = weaponTransform.GetComponent<WeaponManager>();

            // 如果武器发生变化，更新图标
            if (weaponManager != currentWeapon)
            {
                currentWeapon = weaponManager;
                UpdateWeaponSprite(weaponTransform);
            }
        }
        else
        {
            // 玩家没有武器时隐藏图标
            if (currentWeapon != null)
            {
                currentWeapon = null;
                currentWeaponSprite = null;
                HideWeaponIcon();
            }
        }
    }

    // 更新武器精灵图像
    private void UpdateWeaponSprite(Transform weaponTransform)
    {
        if (weaponTransform == null || weaponIcon == null) return;

        // 获取武器的SpriteRenderer组件
        SpriteRenderer weaponSpriteRenderer = weaponTransform.GetComponent<SpriteRenderer>();

        if (weaponSpriteRenderer != null && weaponSpriteRenderer.sprite != null)
        {
            currentWeaponSprite = weaponSpriteRenderer;

            // 将武器精灵赋值给UI图标
            weaponIcon.sprite = weaponSpriteRenderer.sprite;

            // 显示图标
            ShowWeaponIcon();

            Debug.Log($"WeaponNotice: 更新武器图标 - {weaponTransform.name}");
        }
        else
        {
            Debug.LogWarning($"WeaponNotice: 武器 {weaponTransform.name} 没有SpriteRenderer或精灵图像");
            HideWeaponIcon();
        }
    }

    // 显示武器图标
    private void ShowWeaponIcon()
    {
        if (weaponIcon == null) return;

        // 激活图标GameObject
        if (!weaponIcon.gameObject.activeInHierarchy)
        {
            weaponIcon.gameObject.SetActive(true);
        }

        // 设置透明度
        Color iconColor = weaponIcon.color;
        iconColor.a = defaultAlpha;
        weaponIcon.color = iconColor;
    }

    // 隐藏武器图标
    private void HideWeaponIcon()
    {
        if (weaponIcon == null) return;

        if (hideWhenNoWeapon)
        {
            // 禁用GameObject
            weaponIcon.gameObject.SetActive(false);
        }
        else
        {
            // 仅设置透明度为0
            Color iconColor = weaponIcon.color;
            iconColor.a = 0f;
            weaponIcon.color = iconColor;
        }

        // 清空精灵
        weaponIcon.sprite = null;
    }

    // 显示弹药信息
    private void ShowAmmoInfo()
    {
        if (ammoInfoText == null) return;

        UpdateAmmoInfoDisplay();
        ammoInfoText.gameObject.SetActive(true);

        Debug.Log("武器通知: 显示弹药信息");
    }
}