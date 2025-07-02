using UnityEngine;

public class PlayerStateChange : MonoBehaviour
{
    public GameObject player;
    private Animator AIMTOR;
    void Awake()
    {
        AIMTOR = GetComponent<Animator>();
    }
    void Update()
    {
        // 检查 Hand 子对象是否存在
        GameObject hand = player.transform.Find("Hand").gameObject;
        if (hand != null)
        {
            // 遍历 Hand 子对象的所有子对象
            foreach (Transform child in hand.transform)
            {
                // 检查子对象是否具有 Weapon 标签
                if (child.CompareTag("Weapons"))
                {
                    AIMTOR.SetBool("isHaveWeapon", true);
                    AIMTOR.SetBool("isDodging with weapons?", true);
                    return; // 找到一个后就可以退出循环
                }
            }
        }
    }
}
