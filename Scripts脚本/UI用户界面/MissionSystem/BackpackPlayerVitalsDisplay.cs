using UnityEngine;
using TMPro;

/// <summary>
/// 背包面板左侧 StateLine 数值展示：显示玩家 生命/饱食/精神 的 "当前/最大"。
/// 将此脚本挂到 BackpackPanel -> BackPackLeft -> StateLine 上，无需手动拖引用。
/// </summary>
public class BackpackPlayerVitalsDisplay : MonoBehaviour
{
    [Header("可选：手动指定玩家/数值组件（留空则自动查找）")]
    public Player player;
    public PlayerVitalStats vitalStats;

    private TextMeshProUGUI _healthText;
    private TextMeshProUGUI _hungerText;
    private TextMeshProUGUI _mentalText;

    // 缓存上次显示的值（用于无事件来源时的动态刷新）
    private float _lastHealth = -1f, _lastMaxHealth = -1f;
    private float _lastHunger = -1f, _lastMaxHunger = -1f;
    private float _lastMental = -1f, _lastMaxMental = -1f;
    private float _nextFindTime;

    private void Awake()
    {
        // 自动查找文本：在 StateLine 下按命名查找
        _healthText = transform.Find("HealthValue")?.GetComponent<TextMeshProUGUI>();
        _hungerText = transform.Find("SatietyValue")?.GetComponent<TextMeshProUGUI>();
        _mentalText = transform.Find("MentalValue")?.GetComponent<TextMeshProUGUI>();

        if (player == null)
        {
            player = FindObjectOfType<Player>();
        }
        if (vitalStats == null && player != null)
        {
            vitalStats = player.GetComponent<PlayerVitalStats>();
        }

        _nextFindTime = Time.unscaledTime + 0.1f;
    }

    private void OnEnable()
    {
        if (vitalStats != null)
        {
            vitalStats.OnHealthChanged += HandleHealthChanged;
            vitalStats.OnHungerChanged += HandleHungerChanged;
            vitalStats.OnMentalChanged += HandleMentalChanged;
        }

        // 初次刷新（兼容无事件推送的场景）
        _lastHealth = _lastMaxHealth = _lastHunger = _lastMaxHunger = _lastMental = _lastMaxMental = -1f;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (vitalStats != null)
        {
            vitalStats.OnHealthChanged -= HandleHealthChanged;
            vitalStats.OnHungerChanged -= HandleHungerChanged;
            vitalStats.OnMentalChanged -= HandleMentalChanged;
        }
    }

    private void Update()
    {
        // 轮询刷新：当外部未通过 PlayerVitalStats 触发事件时，仍可动态更新
        PollAndRefreshIfChanged();

        // 运行时丢失或尚未找到时，定期重试绑定
        if ((player == null || vitalStats == null) && Time.unscaledTime >= _nextFindTime)
        {
            var newPlayer = player != null ? player : FindObjectOfType<Player>();
            var newVital = vitalStats != null ? vitalStats : (newPlayer != null ? newPlayer.GetComponent<PlayerVitalStats>() : null);
            bool boundChanged = (newPlayer != player) || (newVital != vitalStats);
            player = newPlayer;
            vitalStats = newVital;
            _nextFindTime = Time.unscaledTime + 0.5f;
            if (boundChanged)
            {
                RefreshAll();
                // 重新订阅事件（避免重复订阅：先全部退订再订阅一次）
                if (vitalStats != null)
                {
                    vitalStats.OnHealthChanged -= HandleHealthChanged;
                    vitalStats.OnHungerChanged -= HandleHungerChanged;
                    vitalStats.OnMentalChanged -= HandleMentalChanged;
                    vitalStats.OnHealthChanged += HandleHealthChanged;
                    vitalStats.OnHungerChanged += HandleHungerChanged;
                    vitalStats.OnMentalChanged += HandleMentalChanged;
                }
            }
        }
    }

    private void RefreshAll()
    {
        if (vitalStats != null)
        {
            HandleHealthChanged(vitalStats.currentHealth, vitalStats.maxHealth);
            HandleHungerChanged(vitalStats.currentHunger, vitalStats.maxHunger);
            HandleMentalChanged(vitalStats.currentMental, vitalStats.maxMental);
        }
        else if (player != null)
        {
            // 退化兼容：直接从 Player 旧字段读取
            HandleHealthChanged(player.CurrentHealth, player.MaxHealth);
            HandleHungerChanged(player.CurrentHunger, player.MaxHunger);
            HandleMentalChanged(player.CurrentMental, player.MaxMental);
        }
    }

    private void PollAndRefreshIfChanged()
    {
        if (vitalStats != null)
        {
            TryUpdateHealth(vitalStats.currentHealth, vitalStats.maxHealth);
            TryUpdateHunger(vitalStats.currentHunger, vitalStats.maxHunger);
            TryUpdateMental(vitalStats.currentMental, vitalStats.maxMental);
            return;
        }
        if (player != null)
        {
            TryUpdateHealth(player.CurrentHealth, player.MaxHealth);
            TryUpdateHunger(player.CurrentHunger, player.MaxHunger);
            TryUpdateMental(player.CurrentMental, player.MaxMental);
        }
    }

    private static string FormatPair(float current, float max)
    {
        // 显示为整数比值：当前/最大
        int c = Mathf.RoundToInt(Mathf.Max(0f, current));
        int m = Mathf.RoundToInt(Mathf.Max(1f, max));
        return $"{c}/{m}";
    }

    private void TryUpdateHealth(float value, float max)
    {
        if (!Approximately(value, _lastHealth) || !Approximately(max, _lastMaxHealth))
        {
            HandleHealthChanged(value, max);
        }
    }

    private void TryUpdateHunger(float value, float max)
    {
        if (!Approximately(value, _lastHunger) || !Approximately(max, _lastMaxHunger))
        {
            HandleHungerChanged(value, max);
        }
    }

    private void TryUpdateMental(float value, float max)
    {
        if (!Approximately(value, _lastMental) || !Approximately(max, _lastMaxMental))
        {
            HandleMentalChanged(value, max);
        }
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) <= 0.001f;
    }

    private void HandleHealthChanged(float value, float max)
    {
        if (_healthText != null)
        {
            _healthText.text = FormatPair(value, max);
        }
        _lastHealth = value; _lastMaxHealth = max;
    }

    private void HandleHungerChanged(float value, float max)
    {
        if (_hungerText != null)
        {
            _hungerText.text = FormatPair(value, max);
        }
        _lastHunger = value; _lastMaxHunger = max;
    }

    private void HandleMentalChanged(float value, float max)
    {
        if (_mentalText != null)
        {
            _mentalText.text = FormatPair(value, max);
        }
        _lastMental = value; _lastMaxMental = max;
    }
}


