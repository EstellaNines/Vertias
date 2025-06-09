using UnityEngine;
public class AimTest : MonoBehaviour
{
    private Transform handTransform;
    private Vector3 mousePos;
    private Vector2 handDirection;

    void Awake()
    {
        handTransform = transform.Find("Hand");
        if (handTransform == null)
        {
            Debug.LogError("Hand transform not found!");
        }
    }

    void Update()
    {
        if (handTransform == null)
        {
            return;
        }

        // 获取鼠标在屏幕上的位置，并将其转换为世界坐标
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        handDirection = (mousePos - handTransform.position).normalized;

        // 计算角度并旋转Hand对象
        float angle = Mathf.Atan2(handDirection.y, handDirection.x) * Mathf.Rad2Deg;
        handTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
