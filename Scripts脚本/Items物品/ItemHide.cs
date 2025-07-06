using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemHide : MonoBehaviour
{
    private SpriteRenderer spriteRenderer; // 精灵图片
    private Color color; // 颜色
    [Header("透明度")]
    [Range(0, 1)]
    [FieldLabel("透明度参数")] public float alpha = 0.5f;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // 获取精灵渲染器组件
        color = spriteRenderer.color; // 获取精灵颜色
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player" && spriteRenderer.sprite != null)
        {
            Color Newcolor = new Color(color.r, color.g, color.b, alpha);
            spriteRenderer.color = Newcolor;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        Color Newcolor = new Color(color.r, color.g, color.b, 1f);
        spriteRenderer.color = Newcolor;
    }
}
