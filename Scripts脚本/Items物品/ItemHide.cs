using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHide : MonoBehaviour
{
    private SpriteRenderer spriteRenderer; // 精灵图片
    private Color color; // 颜色
    [Header("透明度")]
    [Range(0, 1)]
    [FieldLabel("进入范围时的透明度参数")] public float alpha = 0.8f;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // 获取精灵渲染器组件
        color = spriteRenderer.color; // 获取精灵颜色
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞体是否是BoxCollider2D类型且属于玩家
        if (collision is BoxCollider2D && collision.gameObject.tag == "Player" && spriteRenderer.sprite != null)
        {
            Color Newcolor = new Color(color.r, color.g, color.b, alpha); // 进入范围时设置为0.8透明度
            spriteRenderer.color = Newcolor;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // 检查碰撞体是否是BoxCollider2D类型且属于玩家
        if (collision is BoxCollider2D && collision.gameObject.tag == "Player")
        {
            Color Newcolor = new Color(color.r, color.g, color.b, 1.0f); // 离开范围时恢复为完全不透明
            spriteRenderer.color = Newcolor;
        }
    }
}
