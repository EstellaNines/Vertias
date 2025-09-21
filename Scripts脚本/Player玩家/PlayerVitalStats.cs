using System;
using UnityEngine;

/// <summary>
/// 玩家数值状态：集中管理生命值、饱食度、精神值，并提供变更事件。
/// 保持与 Player 旧字段的同步由 Player 自身负责（订阅本组件事件）。
/// </summary>
public class PlayerVitalStats : MonoBehaviour
{
    [Header("玩家数值上限")]
    [FieldLabel("生命值上限")]public float maxHealth = 100f;
    [FieldLabel("饱食度上限")]public float maxHunger = 100f;
    [FieldLabel("精神值上限")]public float maxMental = 100f;

    [Header("玩家当前数值")]
    [FieldLabel("生命值")]public float currentHealth = 100f;
    [FieldLabel("饱食度")]public float currentHunger = 100f;
    [FieldLabel("精神值")]public float currentMental = 100f;
    public enum ConsumptionState { Off, Running, Paused }

    [Tooltip("数值随时间消耗/状态机")]
    public ConsumptionState consumptionState = ConsumptionState.Off;

    [Tooltip("是否随时间消耗生命值")]
    [FieldLabel("是否随时间消耗生命值")]public bool enableHealthDecay = false;
    [Tooltip("每秒消耗生命值的速度(>=0)")]
    [FieldLabel("每秒消耗生命值的速度(>=0)")]public float healthDecayPerSecond = 0f;

    [Tooltip("是否随时间消耗饱食度")]
    [FieldLabel("是否随时间消耗饱食度")]public bool enableHungerDecay = false;
    [Tooltip("每秒消耗饱食度的速度(>=0)")]
    [FieldLabel("每秒消耗饱食度的速度(>=0)")]public float hungerDecayPerSecond = 1f;

    [Tooltip("是否随时间消耗精神值")]
    [FieldLabel("是否随时间消耗精神值")]public bool enableMentalDecay = false;
    [Tooltip("每秒消耗精神值的速度(>=0)")]
    [FieldLabel("每秒消耗精神值的速度(>=0)")]public float mentalDecayPerSecond = 0.5f;

    // 数值变更事件（值、上限）
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnHungerChanged;
    public event Action<float, float> OnMentalChanged;

    public bool IsDead => currentHealth <= 0f;

    [Header("ES3 持久化设置")]
    [FieldLabel("启用ES3持久化")] public bool enablePersistence = true;
    [FieldLabel("ES3文件名")] public string es3File = "PlayerVitalStats.es3";
    [FieldLabel("键前缀")] public string keyPrefix = "PlayerVitalStats";
    [FieldLabel("生命周期自动保存")] public bool autoSaveOnLifecycle = true;
    [FieldLabel("变更时自动保存")] public bool autoSaveOnChange = true;
    [FieldLabel("自动保存最小间隔(秒)")] public float autoSaveMinInterval = 2f;

    private bool hasLoadedFromES3 = false;
    private float lastAutoSaveTime = 0f;

    public void InitializeFromDefaults(float maxHealthDefault, float maxHungerDefault, float maxMentalDefault)
    {
        if (maxHealth <= 0f) maxHealth = Mathf.Max(1f, maxHealthDefault);
        if (maxHunger <= 0f) maxHunger = Mathf.Max(1f, maxHungerDefault);
        if (maxMental <= 0f) maxMental = Mathf.Max(1f, maxMentalDefault);

        if (currentHealth <= 0f || currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentHunger <= 0f || currentHunger > maxHunger) currentHunger = maxHunger;
        if (currentMental <= 0f || currentMental > maxMental) currentMental = maxMental;

        // 若存在 ES3 数据，加载以覆盖默认值
        TryLoadFromES3();
        RaiseAllChanged();
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        MaybeAutoSave();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        MaybeAutoSave();
    }

    public void SetHealthMax(float newMax, bool fillToMax)
    {
        maxHealth = Mathf.Max(1f, newMax);
        if (fillToMax) currentHealth = maxHealth;
        else currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        MaybeAutoSave();
    }

    public void SetHunger(float value)
    {
        currentHunger = Mathf.Clamp(value, 0f, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        MaybeAutoSave();
    }

    public void SetMental(float value)
    {
        currentMental = Mathf.Clamp(value, 0f, maxMental);
        OnMentalChanged?.Invoke(currentMental, maxMental);
        MaybeAutoSave();
    }

    public void SetHungerMax(float newMax, bool fillToMax)
    {
        maxHunger = Mathf.Max(1f, newMax);
        if (fillToMax) currentHunger = maxHunger;
        else currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        MaybeAutoSave();
    }

    public void SetMentalMax(float newMax, bool fillToMax)
    {
        maxMental = Mathf.Max(1f, newMax);
        if (fillToMax) currentMental = maxMental;
        else currentMental = Mathf.Clamp(currentMental, 0f, maxMental);
        OnMentalChanged?.Invoke(currentMental, maxMental);
        MaybeAutoSave();
    }

    private void Update()
    {
        if (consumptionState != ConsumptionState.Running) return;
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;
        ApplyDecay(deltaTime);
    }

    public void StartConsumption()
    {
        consumptionState = ConsumptionState.Running;
    }

    public void PauseConsumption()
    {
        consumptionState = ConsumptionState.Paused;
    }

    public void StopConsumption()
    {
        consumptionState = ConsumptionState.Off;
    }

    public void SetConsumptionState(ConsumptionState state)
    {
        consumptionState = state;
    }

    public void SetDecayEnabled(bool health, bool hunger, bool mental)
    {
        enableHealthDecay = health;
        enableHungerDecay = hunger;
        enableMentalDecay = mental;
    }

    public void SetDecayRates(float healthPerSecond, float hungerPerSecond, float mentalPerSecond)
    {
        healthDecayPerSecond = Mathf.Max(0f, healthPerSecond);
        hungerDecayPerSecond = Mathf.Max(0f, hungerPerSecond);
        mentalDecayPerSecond = Mathf.Max(0f, mentalPerSecond);
    }

    private void ApplyDecay(float deltaTime)
    {
        // 生命值衰减
        if (enableHealthDecay && healthDecayPerSecond > 0f && currentHealth > 0f)
        {
            float old = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - healthDecayPerSecond * deltaTime);
            if (!Mathf.Approximately(old, currentHealth))
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                MaybeAutoSave();
            }
        }

        // 饱食度衰减
        if (enableHungerDecay && hungerDecayPerSecond > 0f && currentHunger > 0f)
        {
            float old = currentHunger;
            currentHunger = Mathf.Max(0f, currentHunger - hungerDecayPerSecond * deltaTime);
            if (!Mathf.Approximately(old, currentHunger))
            {
                OnHungerChanged?.Invoke(currentHunger, maxHunger);
                MaybeAutoSave();
            }
        }

        // 精神值衰减
        if (enableMentalDecay && mentalDecayPerSecond > 0f && currentMental > 0f)
        {
            float old = currentMental;
            currentMental = Mathf.Max(0f, currentMental - mentalDecayPerSecond * deltaTime);
            if (!Mathf.Approximately(old, currentMental))
            {
                OnMentalChanged?.Invoke(currentMental, maxMental);
                MaybeAutoSave();
            }
        }
    }

    private void Awake()
    {
        // 若未通过 InitializeFromDefaults 加载过 ES3 数据，唤醒时再尝试一次
        TryLoadFromES3();
    }

    private void OnDisable()
    {
        if (autoSaveOnLifecycle) SaveToES3();
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnLifecycle) SaveToES3();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && autoSaveOnLifecycle) SaveToES3();
    }

    private void MaybeAutoSave()
    {
        if (!enablePersistence) return;
        if (!autoSaveOnChange) return;
        float t = Time.unscaledTime;
        if (t - lastAutoSaveTime >= Mathf.Max(0.1f, autoSaveMinInterval))
        {
            SaveToES3();
            lastAutoSaveTime = t;
        }
    }

    public void SaveToES3()
    {
        if (!enablePersistence) return;
        string prefix = keyPrefix;
        ES3.Save(prefix + ".maxHealth", maxHealth, es3File);
        ES3.Save(prefix + ".maxHunger", maxHunger, es3File);
        ES3.Save(prefix + ".maxMental", maxMental, es3File);
        ES3.Save(prefix + ".currentHealth", currentHealth, es3File);
        ES3.Save(prefix + ".currentHunger", currentHunger, es3File);
        ES3.Save(prefix + ".currentMental", currentMental, es3File);
    }

    public bool LoadFromES3(bool raiseEvents)
    {
        if (!enablePersistence) return false;
        string prefix = keyPrefix;
        if (!ES3.KeyExists(prefix + ".currentHealth", es3File)) return false;

        float savedMaxHealth = ES3.Load(prefix + ".maxHealth", es3File, maxHealth);
        float savedMaxHunger = ES3.Load(prefix + ".maxHunger", es3File, maxHunger);
        float savedMaxMental = ES3.Load(prefix + ".maxMental", es3File, maxMental);
        float savedHealth = ES3.Load(prefix + ".currentHealth", es3File, currentHealth);
        float savedHunger = ES3.Load(prefix + ".currentHunger", es3File, currentHunger);
        float savedMental = ES3.Load(prefix + ".currentMental", es3File, currentMental);

        maxHealth = Mathf.Max(1f, savedMaxHealth);
        maxHunger = Mathf.Max(1f, savedMaxHunger);
        maxMental = Mathf.Max(1f, savedMaxMental);
        currentHealth = Mathf.Clamp(savedHealth, 0f, maxHealth);
        currentHunger = Mathf.Clamp(savedHunger, 0f, maxHunger);
        currentMental = Mathf.Clamp(savedMental, 0f, maxMental);

        if (raiseEvents) RaiseAllChanged();
        return true;
    }

    private void TryLoadFromES3()
    {
        if (hasLoadedFromES3) return;
        if (LoadFromES3(false))
        {
            hasLoadedFromES3 = true;
        }
    }

    private void RaiseAllChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnMentalChanged?.Invoke(currentMental, maxMental);
    }
}


