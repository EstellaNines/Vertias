using UnityEngine;
using InventorySystem;

/// <summary>
/// 防护装备（护甲+头盔）与玩家生命联动：
/// - 上限 = baseHealth + armorMaxDur + helmetMaxDur
/// - 当前生命与两件装备的 currentDurability 互相映射：
///   Health = base + armorCur + helmetCur
/// - 装备/卸下护甲或头盔时做相同映射处理；受伤时把 (Health-base) 同步分配回当前两件装备。
/// - 持久化依赖现有 PlayerVitalStats 与 ItemDataReader/装备持久化。
/// 说明：分配策略采用“保持比例、若都为0则优先护甲”的简单规则，便于理解与可预期。
/// </summary>
public class ProtectiveGearHealthIntegration : MonoBehaviour
{
    [Header("引用")]
    [FieldLabel("玩家数值组件")] public PlayerVitalStats vitalStats;

    [Header("基础参数")]
    [FieldLabel("基础生命值"), Min(1)] public float baseHealth = 100f;
    [FieldLabel("装备时填充到上限")] public bool fillHealthOnEquip = false;

    // 运行时缓存
    private ItemDataReader armorReader;   // 护甲
    private ItemDataReader helmetReader;  // 头盔
    private bool suppressLoop;
    private int lastArmorCur = 0;
    private int lastHelmetCur = 0;
    private float basePartRef = 100f; // 基础生命部分参考值：currentHealth - (armorCur+helmetCur)

    private void Awake()
    {
        if (vitalStats == null) vitalStats = FindObjectOfType<PlayerVitalStats>();
        // 初始化基础部分参考为当前生命（未穿戴装备时用于基准）
        if (vitalStats != null)
        {
            basePartRef = Mathf.Max(0f, vitalStats.currentHealth);
        }
    }

    private void OnEnable()
    {
        EquipmentSlot.OnItemEquipped += OnEquipped;
        EquipmentSlot.OnItemUnequipped += OnUnequipped;
        if (vitalStats != null) vitalStats.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        EquipmentSlot.OnItemEquipped -= OnEquipped;
        EquipmentSlot.OnItemUnequipped -= OnUnequipped;
        if (vitalStats != null) vitalStats.OnHealthChanged -= OnHealthChanged;
    }

    private void OnEquipped(EquipmentSlotType slot, ItemDataReader item)
    {
        // 基础部分 = 当前生命 - 现有两件装备贡献（使用上次记录值，避免把基础部分抬到100）
        float basePart = Mathf.Max(0f, vitalStats != null ? (vitalStats.currentHealth - (lastArmorCur + lastHelmetCur)) : baseHealth);

        if (slot == EquipmentSlotType.Armor) armorReader = item;
        else if (slot == EquipmentSlotType.Helmet) helmetReader = item;
        else return;

        // 新总耐久与上限
        int aMax = GetMax(armorReader);
        int hMax = GetMax(helmetReader);
        int aCur = GetCurInt(armorReader);
        int hCur = GetCurInt(helmetReader);
        float newSum = aCur + hCur;
        float newMax = baseHealth + aMax + hMax;

        // 设置上限但不填充，随后把生命目标设为 basePart + newSum
        suppressLoop = true;
        vitalStats.SetHealthMax(newMax, false);
        float target = Mathf.Clamp(basePart + newSum, 0f, newMax);
        vitalStats.Heal(target - vitalStats.currentHealth);
        suppressLoop = false;

        // 更新记录
        lastArmorCur = aCur;
        lastHelmetCur = hCur;
        basePartRef = basePart;
    }

    private void OnUnequipped(EquipmentSlotType slot, ItemDataReader item)
    {
        if (slot != EquipmentSlotType.Armor && slot != EquipmentSlotType.Helmet) return;

        // 先计算当前基础部分（按上次记录的装备贡献扣除）
        int oldA = lastArmorCur;
        int oldH = lastHelmetCur;
        float basePart = Mathf.Max(0f, vitalStats.currentHealth - (oldA + oldH));

        // 为被卸下物品设置其当前耐久（确保展示一致）
        if (slot == EquipmentSlotType.Armor && item != null)
        {
            item.SetDurability(oldA);
        }
        else if (slot == EquipmentSlotType.Helmet && item != null)
        {
            item.SetDurability(oldH);
        }

        // 移除引用并更新记录
        if (slot == EquipmentSlotType.Armor && armorReader == item) { armorReader = null; lastArmorCur = 0; }
        if (slot == EquipmentSlotType.Helmet && helmetReader == item) { helmetReader = null; lastHelmetCur = 0; }

        // 计算移除后的合计与上限，并设置当前生命为 basePart + 剩余合计
        int aMax = GetMax(armorReader);
        int hMax = GetMax(helmetReader);
        int aCur = GetCurInt(armorReader);
        int hCur = GetCurInt(helmetReader);
        float newSum = aCur + hCur;
        float newMax = baseHealth + aMax + hMax;

        suppressLoop = true;
        vitalStats.SetHealthMax(newMax, false);
        float target = Mathf.Clamp(basePart + newSum, 0f, newMax);
        vitalStats.Heal(target - vitalStats.currentHealth);
        suppressLoop = false;

        lastArmorCur = aCur;
        lastHelmetCur = hCur;
        basePartRef = basePart;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (suppressLoop) return;
        // 如果未穿防护装备，更新基础部分参考为当前生命值
        if (armorReader == null && helmetReader == null)
        {
            basePartRef = Mathf.Max(0f, current);
            return;
        }

        // 穿着至少一件防护装备时，把 (Health - basePartRef) 同步回耐久
        if (armorReader != null || helmetReader != null)
        {
            WriteBackDurabilityFromHealth();
        }
    }

    private void RecomputeMaxAndClampOnEquip()
    {
        // 依据当前两件装备的“最大耐久”设置生命上限
        // 只有当总“当前耐久”>0 时才允许填充到上限，避免 0 耐久装备导致无故加血
        float sumCur = GetCur(armorReader) + GetCur(helmetReader);
        bool allowFill = fillHealthOnEquip && sumCur > 0f;
        RecomputeMaxAndClamp(fillToMax: allowFill);

        // 装备存在但其当前耐久较低时，保证 Health 不超过 base+sumCurDur
        float cap = baseHealth + sumCur;
        if (vitalStats.currentHealth > cap)
        {
            suppressLoop = true;
            vitalStats.Heal(cap - vitalStats.currentHealth);
            suppressLoop = false;
        }
        // 不在这里回写，回写由 OnHealthChanged 驱动
    }

    private void RecomputeMaxAndClamp(bool fillToMax)
    {
        float newMax = baseHealth + GetMax(armorReader) + GetMax(helmetReader);
        suppressLoop = true;
        vitalStats.SetHealthMax(newMax, fillToMax);
        if (!fillToMax)
        {
            float clamped = Mathf.Clamp(vitalStats.currentHealth, 0f, newMax);
            if (!Mathf.Approximately(clamped, vitalStats.currentHealth))
            {
                vitalStats.Heal(clamped - vitalStats.currentHealth);
            }
        }
        suppressLoop = false;
    }

    private void WriteBackDurabilityFromHealth()
    {
        // 使用 basePartRef 确保在基础生命不足100时，耐久合计=当前生命-基础部分
        float totalCur = Mathf.Max(0f, vitalStats.currentHealth - basePartRef);
        int aMax = GetMax(armorReader);
        int hMax = GetMax(helmetReader);
        int aCur = Mathf.Clamp(Mathf.RoundToInt(GetCur(armorReader)), 0, aMax);
        int hCur = Mathf.Clamp(Mathf.RoundToInt(GetCur(helmetReader)), 0, hMax);

        int totalMax = Mathf.Max(0, aMax + hMax);
        if (totalMax == 0)
        {
            // 没有装备，直接返回
            return;
        }

        // 目标分配：尽量保持比例；若两者都是0且 totalCur>0，优先给护甲
        float targetA = 0f, targetH = 0f;
        if (aCur > 0 || hCur > 0)
        {
            float sumCur = Mathf.Max(1f, aCur + hCur);
            float pA = aCur / sumCur;
            float pH = hCur / sumCur;
            targetA = Mathf.Min(aMax, totalCur * pA);
            targetH = Mathf.Min(hMax, totalCur * pH);
        }
        else
        {
            // 都耗尽时，优先护甲
            targetA = Mathf.Min(aMax, totalCur);
            targetH = Mathf.Min(hMax, totalCur - targetA);
        }

        int newA = Mathf.Clamp(Mathf.RoundToInt(targetA), 0, aMax);
        int newH = Mathf.Clamp(Mathf.RoundToInt(targetH), 0, hMax);

        SetCur(armorReader, newA);
        SetCur(helmetReader, newH);
        // 更新记录
        lastArmorCur = newA;
        lastHelmetCur = newH;
    }

    private int GetMax(ItemDataReader reader)
    {
        if (reader == null || reader.ItemData == null) return 0;
        return Mathf.Max(0, reader.ItemData.durability);
    }

    private float GetCur(ItemDataReader reader)
    {
        if (reader == null) return 0f;
        return Mathf.Max(0, reader.currentDurability);
    }

    private int GetCurInt(ItemDataReader reader)
    {
        if (reader == null) return 0;
        return Mathf.Max(0, reader.currentDurability);
    }

    private void SetCur(ItemDataReader reader, int value)
    {
        if (reader == null) return;
        reader.SetDurability(value);
        reader.SaveRuntimeToES3();
    }
}


