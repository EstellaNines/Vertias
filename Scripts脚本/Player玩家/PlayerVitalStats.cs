using System;
using UnityEngine;

/// <summary>
/// 玩家数值状态：集中管理生命值、饱食度、精神值，并提供变更事件。
/// 保持与 Player 旧字段的同步由 Player 自身负责（订阅本组件事件）。
/// </summary>
public class PlayerVitalStats : MonoBehaviour
{
    [Header("玩家数值上限")]
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxMental = 100f;

    [Header("玩家当前数值")]
    public float currentHealth = 100f;
    public float currentHunger = 100f;
    public float currentMental = 100f;

    // 数值变更事件（值、上限）
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnHungerChanged;
    public event Action<float, float> OnMentalChanged;

    public bool IsDead => currentHealth <= 0f;

    public void InitializeFromDefaults(float maxHealthDefault, float maxHungerDefault, float maxMentalDefault)
    {
        if (maxHealth <= 0f) maxHealth = Mathf.Max(1f, maxHealthDefault);
        if (maxHunger <= 0f) maxHunger = Mathf.Max(1f, maxHungerDefault);
        if (maxMental <= 0f) maxMental = Mathf.Max(1f, maxMentalDefault);

        if (currentHealth <= 0f || currentHealth > maxHealth) currentHealth = maxHealth;
        if (currentHunger <= 0f || currentHunger > maxHunger) currentHunger = maxHunger;
        if (currentMental <= 0f || currentMental > maxMental) currentMental = maxMental;

        RaiseAllChanged();
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHealthMax(float newMax, bool fillToMax)
    {
        maxHealth = Mathf.Max(1f, newMax);
        if (fillToMax) currentHealth = maxHealth;
        else currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetHunger(float value)
    {
        currentHunger = Mathf.Clamp(value, 0f, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void SetMental(float value)
    {
        currentMental = Mathf.Clamp(value, 0f, maxMental);
        OnMentalChanged?.Invoke(currentMental, maxMental);
    }

    public void SetHungerMax(float newMax, bool fillToMax)
    {
        maxHunger = Mathf.Max(1f, newMax);
        if (fillToMax) currentHunger = maxHunger;
        else currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void SetMentalMax(float newMax, bool fillToMax)
    {
        maxMental = Mathf.Max(1f, newMax);
        if (fillToMax) currentMental = maxMental;
        else currentMental = Mathf.Clamp(currentMental, 0f, maxMental);
        OnMentalChanged?.Invoke(currentMental, maxMental);
    }

    private void RaiseAllChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnMentalChanged?.Invoke(currentMental, maxMental);
    }
}


