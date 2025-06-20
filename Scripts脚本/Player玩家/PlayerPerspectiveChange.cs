using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPerspectiveChange : MonoBehaviour
{
    [SerializeField] private Transform hand;
    private float screenCenterX;

    void Start()
    {
        screenCenterX = Screen.width / 2f;
    }

    void Update()
    {
        if (hand == null) return;
        if (hand.childCount >= 1)
        {
            var mousePos = Mouse.current.position.ReadValue();
            Vector3 scale = hand.GetChild(0).localScale;

            if (mousePos.x < screenCenterX)
            {
                // 左侧翻转Y轴
                if (scale.y > 0) scale.y = -scale.y;
            }
            else
            {
                // 右侧保持正常
                if (scale.y < 0) scale.y = -scale.y;
            }

            hand.GetChild(0).localScale = scale;
        }
    }
}