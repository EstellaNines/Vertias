using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponNotice : MonoBehaviour
{
    [Header("UI组件")]
    [FieldLabel("武器图标显示")] public Image weaponIcon; // 武器图标UI控件
    [FieldLabel("弹药信息显示")] public TextMeshProUGUI ammoInfoText; // 弹药信息文本

    [Header("玩家引用")]
    [FieldLabel("玩家对象")] public Player player; // 玩家引用
    [FieldLabel("输入控制器")] public PlayerInputController inputController; // 输入控制器引用

    [Header("设置")]
    [FieldLabel("默认透明度")][Range(0f, 1f)] public float defaultAlpha = 1f; // 有武器时的透明度
    [FieldLabel("无武器时隐藏")] public bool hideWhenNoWeapon = true; // 无武器时是否隐藏图标

    // 私有变量
    private WeaponManager currentWeapon;
    private SpriteRenderer currentWeaponSprite;
    private bool isAmmoInfoVisible = false; // 弹药信息是否可见

    void Start()
    {
        // 如果没有手动分配玩家引用，尝试自动查找
        if (player == null)
        {
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError("WeaponNotice: 找不到Player对象！请手动分配Player引用。");
                return;
            }
        }

        // 如果没有手动分配输入控制器，尝试自动查找
        if (inputController == null)
        {
            inputController = FindObjectOfType<PlayerInputController>();
            if (inputController == null)
            {
                Debug.LogError("WeaponNotice: 找不到PlayerInputController对象！请手动分配输入控制器引用。");
                return;
            }
        }

        // 验证UI组件
        if (weaponIcon == null)
        {
            Debug.LogError("WeaponNotice: 请分配武器图标UI组件！");
            return;
        }

        if (ammoInfoText == null)
        {
            Debug.LogError("WeaponNotice: 请分配弹药信息文本组件！");
            return;
        }

        // 初始化UI状态
        InitializeUI();

        // 订阅武器检视输入事件
        SubscribeToWeaponInspectionInput();
    }

    void Update()
    {
        if (player != null)
        {
            UpdateWeaponIcon();

            // 如果弹药信息可见，实时更新内容
            if (isAmmoInfoVisible)
            {
                UpdateAmmoInfoDisplay();
            }
        }
    }

    void OnDestroy()
    {
        // 取消订阅输入事件
        UnsubscribeFromWeaponInspectionInput();
    }

    // 订阅武器检视输入事件
    private void SubscribeToWeaponInspectionInput()
    {
        if (inputController != null)
        {
            inputController.onWeaponInspection += OnWeaponInspection;
        }
    }

    // 取消订阅武器检视输入事件
    private void UnsubscribeFromWeaponInspectionInput()
    {
        if (inputController != null)
        {
            inputController.onWeaponInspection -= OnWeaponInspection;
        }
    }

    // 武器检视处理
    private void OnWeaponInspection()
    {
        // 直接更新弹药信息显示，不切换显示状态
        UpdateAmmoInfoDisplay();

        // 确保弹药信息始终可见
        if (ammoInfoText != null && !ammoInfoText.gameObject.activeInHierarchy)
        {
            ammoInfoText.gameObject.SetActive(true);
        }

        Debug.Log("武器检视: 更新弹药信息");
    }

    // 初始化UI状态
    private void InitializeUI()
    {
        if (weaponIcon != null)
        {
            // 初始状态下隐藏图标
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

        // 初始化弹药信息文本 - 设置为始终显示
        if (ammoInfoText != null)
        {
            ammoInfoText.gameObject.SetActive(true);
            UpdateAmmoInfoDisplay(); // 初始化时更新一次显示内容
        }

        isAmmoInfoVisible = true; // 设置为始终可见
    }

    // 公共方法：手动更新武器图标
    public void RefreshWeaponIcon()
    {
        if (player != null)
        {
            currentWeapon = null; // 强制刷新
            UpdateWeaponIcon();
        }
    }

    // 公共方法：设置图标透明度
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

    // 公共方法：获取当前武器信息
    public string GetCurrentWeaponName()
    {
        if (currentWeapon != null)
        {
            return currentWeapon.GetWeaponName();
        }
        return "无武器";
    }

    // 公共方法：手动触发武器检视
    public void TriggerWeaponInspection()
    {
        OnWeaponInspection();
    }

    // 公共方法：立即隐藏弹药信息
    public void HideAmmoInfo()
    {
        if (ammoInfoText != null)
        {
            ammoInfoText.gameObject.SetActive(false);
        }

        isAmmoInfoVisible = false;
    }

    // 公共方法：强制显示弹药信息
    public void ForceShowAmmoInfo()
    {
        isAmmoInfoVisible = true;
        ShowAmmoInfo();
    }



    // 更新弹药信息显示内容
    private void UpdateAmmoInfoDisplay()
    {
        if (ammoInfoText == null) return;

        if (currentWeapon != null)
        {
            int currentAmmo = currentWeapon.GetCurrentAmmo();
            string ammoStatusText = GetAmmoStatusText(currentAmmo);

            // 显示弹药信息
            ammoInfoText.text = $"{ammoStatusText}";
        }
        else
        {
            ammoInfoText.text = "当前没有持有武器";
        }
    }

    // 根据弹药数量获取状态文字
    private string GetAmmoStatusText(int ammoCount)
    {
        if (ammoCount == 0)
        {
            return "无弹药";
        }
        else if (ammoCount > 25)
        {
            return "还有很多";
        }
        else if (ammoCount > 20)
        {
            return "子弹较多";
        }
        else if (ammoCount >= 14 && ammoCount <= 16)
        {
            return "还剩一半";
        }
        else if (ammoCount < 10)
        {
            return "所剩无几";
        }
        else
        {
            return "弹药适中";
        }
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
            // 没有武器时隐藏图标
            if (currentWeapon != null)
            {
                currentWeapon = null;
                currentWeaponSprite = null;
                HideWeaponIcon();
            }
        }
    }

    // 更新武器精灵
    private void UpdateWeaponSprite(Transform weaponTransform)
    {
        if (weaponTransform == null || weaponIcon == null) return;

        // 获取武器的SpriteRenderer组件
        SpriteRenderer weaponSpriteRenderer = weaponTransform.GetComponent<SpriteRenderer>();

        if (weaponSpriteRenderer != null && weaponSpriteRenderer.sprite != null)
        {
            currentWeaponSprite = weaponSpriteRenderer;

            // 将武器的精灵图片同步到UI图标
            weaponIcon.sprite = weaponSpriteRenderer.sprite;

            // 显示图标
            ShowWeaponIcon();

            Debug.Log($"WeaponNotice: 更新武器图标 - {weaponTransform.name}");
        }
        else
        {
            Debug.LogWarning($"WeaponNotice: 武器 {weaponTransform.name} 没有SpriteRenderer组件或精灵图片！");
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
            // 完全隐藏GameObject
            weaponIcon.gameObject.SetActive(false);
        }
        else
        {
            // 只设置为透明
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

        Debug.Log("武器检视: 显示弹药信息");
    }
}