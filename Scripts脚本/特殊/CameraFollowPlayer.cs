using UnityEngine;
using Cinemachine;

public class CameraFollowPlayer : MonoBehaviour
{
    Player player; // 玩家

    private void Awake()
    {
        CinemachineVirtualCamera camera = GetComponent<Cinemachine.CinemachineVirtualCamera>();
        player = FindObjectOfType<Player>(); // 获取玩家
        if (player != null)
        {
            camera.Follow = player.transform; // 设置摄像机跟随玩家
        }
    }
}
