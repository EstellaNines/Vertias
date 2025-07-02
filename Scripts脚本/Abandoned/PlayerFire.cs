using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFire : MonoBehaviour
{
    private PlayerInputAction PInputAction;

    private PlayerController playerController; // 新增引用

    void Awake()
    {
        PInputAction = new PlayerInputAction();
        playerController = GetComponent<PlayerController>(); // 获取控制器
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        if (playerController != null && playerController.IsDodging())
        {
            return; // 如果正在闪避，禁止射击
        }

        if (playerController != null)
        {
            playerController.SetIsFiring(!context.canceled); // 更新射击状态
        }

        transform.BroadcastMessage("FireInput", context, SendMessageOptions.DontRequireReceiver);
    }

    void OnEnable()
    {
        PInputAction.Enable();
        PInputAction.GamePlay.Fire.performed += OnFire;
        PInputAction.GamePlay.Fire.started += OnFire;
        PInputAction.GamePlay.Fire.canceled += OnFire;
    }

    void OnDisable()
    {
        PInputAction.Disable();
        PInputAction.GamePlay.Fire.performed -= OnFire;
        PInputAction.GamePlay.Fire.started -= OnFire;
        PInputAction.GamePlay.Fire.canceled -= OnFire;

        if (playerController != null)
        {
            playerController.SetIsFiring(false); // 确保状态重置
        }
    }
}
