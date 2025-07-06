using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FillLine : MonoBehaviour
{
    [Header("UI引用")]
    public Image FillHealthLine; // 血条
    public Image FillHungerLine; // 吃饭条
    public Image FillMentalLine; // 瞌睡条
    
    [Header("换弹UI")]
    public Image ReloadCircleBackground; // 换弹圆环底层（黑色）
    public Image ReloadCircle; // 换弹圆环上层（可填充）
    public CanvasGroup ReloadCanvasGroup; // 用于控制透明度的CanvasGroup
    [Range(0.5f, 3f)] public float fadeOutDuration = 1f; // 淡出持续时间
    [Range(0.5f, 2f)] public float showDuration = 1f; // 显示持续时间

    [Header("玩家引用")]
    public Player player; // 玩家引用

    [Header("数值")]
    public float Health; // 血量
    public float Hunger; // 吃东西
    public float Mental; // 瞌睡
    
    // 换弹相关私有变量
    private bool isReloadUIActive = false;
    private Coroutine fadeOutCoroutine;
    
    private void Start()
    {
        // 如果没有手动分配玩家引用，尝试自动查找
        if (player == null)
        {
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError("FillLine: 找不到Player对象！请手动分配Player引用。");
            }
        }
        
        // 初始化换弹UI
        InitializeReloadUI();
    }
    
    private void Update()
    {
        if (player != null)
        {
            // 获取玩家当前数值
            Health = player.CurrentHealth;
            Hunger = player.CurrentHunger;
            Mental = player.CurrentMental;
            
            // 更新UI填充
            BarFiller();
            
            // 更新换弹UI
            UpdateReloadUI();
        }
    }

    private void BarFiller()
    {
        // 更新生命值条
        if (FillHealthLine != null && player != null)
        {
            float healthRatio = Health / player.MaxHealth;
            FillHealthLine.fillAmount = Mathf.Clamp01(healthRatio);
        }
        
        // 更新饱食度条
        if (FillHungerLine != null && player != null)
        {
            float hungerRatio = Hunger / player.MaxHunger;
            FillHungerLine.fillAmount = Mathf.Clamp01(hungerRatio);
        }
        
        // 更新精神值条
        if (FillMentalLine != null && player != null)
        {
            float mentalRatio = Mental / player.MaxMental;
            FillMentalLine.fillAmount = Mathf.Clamp01(mentalRatio);
        }
    }
    
    // 初始化换弹UI
    private void InitializeReloadUI()
    {
        // 初始化底层圆环
        if (ReloadCircleBackground != null)
        {
            ReloadCircleBackground.gameObject.SetActive(false);
        }
        
        // 初始化上层圆环
        if (ReloadCircle != null)
        {
            ReloadCircle.fillAmount = 0f;
            ReloadCircle.gameObject.SetActive(false);
        }
        
        if (ReloadCanvasGroup != null)
        {
            ReloadCanvasGroup.alpha = 0f;
        }
        
        isReloadUIActive = false;
    }
    
    // 更新换弹UI
    private void UpdateReloadUI()
    {
        if (player.currentWeaponController == null) return;
        
        bool needsReload = player.currentWeaponController.NeedsReload();
        bool isReloading = player.currentWeaponController.IsReloading();
        
        // 检查是否需要显示换弹UI
        if (needsReload || isReloading)
        {
            ShowReloadUI();
            
            if (isReloading)
            {
                // 更新换弹进度
                float reloadProgress = player.currentWeaponController.GetReloadProgress();
                UpdateReloadProgress(reloadProgress);
            }
            else if (needsReload)
            {
                // 需要换弹但还没开始，显示空圆环
                UpdateReloadProgress(0f);
            }
        }
        else if (isReloadUIActive && !isReloading)
        {
            // 换弹完成，开始淡出
            StartFadeOut();
        }
    }
    
    // 显示换弹UI
    private void ShowReloadUI()
    {
        if (!isReloadUIActive)
        {
            isReloadUIActive = true;
            
            // 显示底层圆环
            if (ReloadCircleBackground != null)
            {
                ReloadCircleBackground.gameObject.SetActive(true);
            }
            
            // 显示上层圆环
            if (ReloadCircle != null)
            {
                ReloadCircle.gameObject.SetActive(true);
            }
            
            if (ReloadCanvasGroup != null)
            {
                ReloadCanvasGroup.alpha = 1f;
            }
            
            // 停止任何正在进行的淡出协程
            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine);
                fadeOutCoroutine = null;
            }
        }
    }
    
    // 更新换弹进度
    private void UpdateReloadProgress(float progress)
    {
        if (ReloadCircle != null)
        {
            ReloadCircle.fillAmount = Mathf.Clamp01(progress);
        }
    }
    
    // 开始淡出
    private void StartFadeOut()
    {
        if (fadeOutCoroutine == null)
        {
            fadeOutCoroutine = StartCoroutine(FadeOutCoroutine());
        }
    }
    
    // 淡出协程
    private IEnumerator FadeOutCoroutine()
    {
        // 先显示完整圆环一段时间
        UpdateReloadProgress(1f);
        yield return new WaitForSeconds(showDuration);
        
        // 开始淡出
        float elapsedTime = 0f;
        float startAlpha = ReloadCanvasGroup != null ? ReloadCanvasGroup.alpha : 1f;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeOutDuration);
            
            if (ReloadCanvasGroup != null)
            {
                ReloadCanvasGroup.alpha = alpha;
            }
            
            yield return null;
        }
        
        // 淡出完成，隐藏UI
        if (ReloadCanvasGroup != null)
        {
            ReloadCanvasGroup.alpha = 0f;
        }
        
        // 隐藏底层圆环
        if (ReloadCircleBackground != null)
        {
            ReloadCircleBackground.gameObject.SetActive(false);
        }
        
        // 隐藏上层圆环
        if (ReloadCircle != null)
        {
            ReloadCircle.gameObject.SetActive(false);
        }
        
        isReloadUIActive = false;
        fadeOutCoroutine = null;
    }
    
    // 可选：手动更新UI的公共方法
    public void UpdateUI()
    {
        if (player != null)
        {
            Health = player.CurrentHealth;
            Hunger = player.CurrentHunger;
            Mental = player.CurrentMental;
            BarFiller();
        }
    }
    
    // 强制隐藏换弹UI（可选，用于特殊情况）
    public void ForceHideReloadUI()
    {
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }
        
        InitializeReloadUI();
    }
}
