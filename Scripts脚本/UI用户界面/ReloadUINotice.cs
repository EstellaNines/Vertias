using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReloadUINotice : MonoBehaviour
{
    [Header("UI引用")]
    [FieldLabel("填充圆环")] public Image FillCircle; // 填充圆环
    [FieldLabel("填充圆环背景")] public Image FillCircularBackground; // 填充圆环背景
    [FieldLabel("动画图片")] public Image AnimationImage; // 用于播放帧动画的图片
    
    [Header("帧动画设置")]
    [FieldLabel("动画精灵帧")] public Sprite[] animationSprites; // 动画精灵帧数组
    [FieldLabel("动画帧率")] public float animationFrameRate = 5f; // 动画帧率（每秒帧数）
    
    // 玩家引用
    private Player player;
    
    // 动画相关变量
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private bool isPlayingAnimation = false;
    
    private void Start()
    {
        // 获取玩家引用
        player = FindObjectOfType<Player>();
        
        // 确保UI初始状态正确
        if (FillCircle != null)
        {
            FillCircle.fillAmount = 0f;
            FillCircle.gameObject.SetActive(false);
        }
        if (FillCircularBackground != null)
        {
            FillCircularBackground.gameObject.SetActive(false);
        }
        if (AnimationImage != null)
        {
            AnimationImage.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 检查玩家是否存在且有武器
        if (player == null || !player.isWeaponInHand || player.currentWeaponController == null)
        {
            // 没有武器时隐藏UI
            HideReloadUI();
            return;
        }

        WeaponManager weapon = player.currentWeaponController;
        
        // 检查武器是否正在换弹
        if (weapon.IsReloading())
        {
            // 显示UI
            ShowReloadUI();
            
            // 直接从武器获取换弹进度
            float progress = weapon.GetReloadProgress();
            
            if (FillCircle != null)
            {
                FillCircle.fillAmount = progress;
            }
            
            // 播放帧动画
            PlayFrameAnimation();
            
            // 调试信息
            Debug.Log($"武器换弹进度: {progress:F2} - 武器: {weapon.GetWeaponName()}");
        }
        else
        {
            // 不在换弹时隐藏UI
            HideReloadUI();
        }
    }
    
    // 播放帧动画
    private void PlayFrameAnimation()
    {
        // 检查是否有动画精灵和动画图片组件
        if (animationSprites == null || animationSprites.Length == 0 || AnimationImage == null)
            return;
            
        // 启动动画
        if (!isPlayingAnimation)
        {
            isPlayingAnimation = true;
            currentFrameIndex = 0;
            frameTimer = 0f;
        }
        
        // 更新帧计时器
        frameTimer += Time.deltaTime;
        
        // 计算每帧的时间间隔
        float frameInterval = 1f / animationFrameRate;
        
        // 检查是否需要切换到下一帧
        if (frameTimer >= frameInterval)
        {
            frameTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % animationSprites.Length; // 循环播放
            
            // 更新动画图片的精灵
            AnimationImage.sprite = animationSprites[currentFrameIndex];
        }
    }
    
    // 停止帧动画
    private void StopFrameAnimation()
    {
        isPlayingAnimation = false;
        currentFrameIndex = 0;
        frameTimer = 0f;
        
        // 重置到第一帧或隐藏动画图片
        if (AnimationImage != null && animationSprites != null && animationSprites.Length > 0)
        {
            AnimationImage.sprite = animationSprites[0];
        }
    }
    
    // 显示换弹UI
    private void ShowReloadUI()
    {
        if (FillCircle != null && !FillCircle.gameObject.activeInHierarchy)
        {
            FillCircle.gameObject.SetActive(true);
        }
        if (FillCircularBackground != null && !FillCircularBackground.gameObject.activeInHierarchy)
        {
            FillCircularBackground.gameObject.SetActive(true);
        }
        if (AnimationImage != null && !AnimationImage.gameObject.activeInHierarchy)
        {
            AnimationImage.gameObject.SetActive(true);
        }
    }
    
    // 隐藏换弹UI
    private void HideReloadUI()
    {
        if (FillCircle != null && FillCircle.gameObject.activeInHierarchy)
        {
            FillCircle.gameObject.SetActive(false);
            FillCircle.fillAmount = 0f;
        }
        if (FillCircularBackground != null && FillCircularBackground.gameObject.activeInHierarchy)
        {
            FillCircularBackground.gameObject.SetActive(false);
        }
        if (AnimationImage != null && AnimationImage.gameObject.activeInHierarchy)
        {
            AnimationImage.gameObject.SetActive(false);
            StopFrameAnimation(); // 停止动画
        }
    }
}