using UnityEngine;
using UnityEngine.UI;

public class FillLine : MonoBehaviour
{
    [Header("UI引用")]
    public Image FillHealthLine; // 血条
    public Image FillHungerLine; // 吃饭条
    public Image FillMentalLine; // 瞌睡条

    [Header("玩家引用")]
    public Player player; // 玩家引用

    [Header("数值")]
    public float Health; // 血量
    public float Hunger; // 吃东西
    public float Mental; // 瞌睡
    
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
}
