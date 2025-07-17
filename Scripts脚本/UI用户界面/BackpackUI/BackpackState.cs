using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackState : MonoBehaviour
{
    [Header("����UI���")]
    [SerializeField] private Canvas backpackCanvas; // ����Canvas���
    [SerializeField] private PlayerInputController playerInputController; // ���������������
    [SerializeField] private ButtonOpenPlatform buttonOpenPlatform; // ��ťƽ̨������
    [SerializeField] private TopNavigationTransform topNav; // TopNavigationTransform������

    [Header("״̬����")]
    private bool isBackpackOpen = false; // �����Ƿ��
    private bool isInitialized = false; // �Ƿ��ѳ�ʼ��

    private void Start()
    {
        InitializeBackpack();
    }

    // �������������������������������ڿ糡����
    public void SetPlayerInputController(PlayerInputController controller)
    {
        // �����ɵ��¼�����
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
            Debug.Log("BackpackState: �������ɵ��¼�����");
        }

        playerInputController = controller;

        // ���³�ʼ��
        isInitialized = false; // ���ó�ʼ����־
        InitializeBackpack();
    }

    // ��ʼ������ϵͳ
    private void InitializeBackpack()
    {
        if (isInitialized)
        {
            Debug.Log("BackpackState: �Ѿ���ʼ���������ظ���ʼ��");
            return;
        }

        // ��ʼ��ʱ�رձ�������
        if (backpackCanvas != null)
        {
            backpackCanvas.gameObject.SetActive(false);
            isBackpackOpen = false;
        }
        else
        {
            Debug.LogError("BackpackState: ����Canvasδ���ã�����Inspector����קCanvas���");
            return;
        }

        // �Ƴ��رհ�ť��س�ʼ��
        // if (closeButton != null) { ... }

        // �����������������¼�����
        if (playerInputController != null)
        {
            // ȷ�������ظ������¼�����
            playerInputController.onBackPack -= ToggleBackpack; // ���Ƴ�
            playerInputController.onBackPack += ToggleBackpack;  // ������
            playerInputController.EnabledUIInput();

            isInitialized = true;
            Debug.Log("BackpackState: ����ϵͳ��ʼ�����");
        }
        else
        {
            Debug.LogWarning("BackpackState: PlayerInputControllerδ���ã��ȴ��糡������������");
        }
    }

    // �л���������״̬
    private void ToggleBackpack()
    {
        if (topNav != null)
        {
            topNav.ToggleBackpack(); // ����Ǩ�ƺ��ToggleBackpack
        }
    }

    // �򿪱���
    private void OpenBackpack()
    {
        // �򿪱���UIʱ������Ϸ���룬����UI����
        if (playerInputController != null)
        {
            playerInputController.DisableGameplayInput();
            playerInputController.EnabledUIInput();
        }

        // ��ʾ�����
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ��ʾĬ�����
        ShowDefaultPanel();
    }

    // �رձ���
    private void CloseBackpack()
    {
        // �رձ���ʱ�ָ���Ϸ���룬����UI���������Ա���ӦTab��
        if (playerInputController != null)
        {
            playerInputController.EnabledGameplayInput();
            // ����UI���������Ա���ӦTab��
        }

        // ���������ɼ������������״̬
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // �����������
        HideAllPanels();
    }

    // ��ʾĬ�����
    private void ShowDefaultPanel()
    {
        if (buttonOpenPlatform != null)
        {
            // Ĭ��ѡ�е�0����ť����ʾ��Ӧ�ĵ�0��RawImage
            buttonOpenPlatform.SelectButton(0);
        }
    }

    // �����������
    private void HideAllPanels()
    {
        if (buttonOpenPlatform != null)
        {
            buttonOpenPlatform.ClearSelection();
        }
    }

    // ����������ǿ�ƹرձ���
    public void ForceCloseBackpack()
    {
        if (isBackpackOpen)
        {
            isBackpackOpen = false;
            if (backpackCanvas != null)
            {
                backpackCanvas.gameObject.SetActive(false);
            }
            CloseBackpack();
        }
    }

    // ����������ǿ�ƴ򿪱���
    public void ForceOpenBackpack()
    {
        if (!isBackpackOpen)
        {
            isBackpackOpen = true;
            if (backpackCanvas != null)
            {
                backpackCanvas.gameObject.SetActive(true);
            }
            OpenBackpack();
        }
    }

    // ������������ȡ��������״̬
    public bool IsBackpackOpen()
    {
        return isBackpackOpen;
    }

    private void OnDestroy()
    {
        // �����¼��������Է�ֹ�ڴ�й©
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
        }
    }

        // �������������³�ʼ�������ڿ糡����
        public void ReInitialize()
    {
        isInitialized = false;
        InitializeBackpack();
    }
}

