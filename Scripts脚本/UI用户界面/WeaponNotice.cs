using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponNotice : MonoBehaviour
{
    [Header("UI���")]
    [FieldLabel("����ͼ����ʾ")] public Image weaponIcon; // ����ͼ��UI�ؼ�
    [FieldLabel("��ҩ��Ϣ��ʾ")] public TextMeshProUGUI ammoInfoText; // ��ҩ��Ϣ�ı�

    [Header("�������")]
    [FieldLabel("��Ҷ���")] public Player player; // �������
    [FieldLabel("���������")] public PlayerInputController inputController; // �������������

    [Header("����")]
    [FieldLabel("Ĭ��͸����")][Range(0f, 1f)] public float defaultAlpha = 1f; // ������ʱ��͸����
    [FieldLabel("������ʱ����")] public bool hideWhenNoWeapon = true; // ������ʱ�Ƿ�����ͼ��

    // ˽�б���
    private WeaponManager currentWeapon;
    private SpriteRenderer currentWeaponSprite;
    private bool isAmmoInfoVisible = false; // ��ҩ��Ϣ�Ƿ�ɼ�

    void Start()
    {
        // ���û���ֶ�����������ã������Զ�����
        if (player == null)
        {
            player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.LogError("WeaponNotice: �Ҳ���Player�������ֶ�����Player���á�");
                return;
            }
        }

        // ���û���ֶ���������������������Զ�����
        if (inputController == null)
        {
            inputController = FindObjectOfType<PlayerInputController>();
            if (inputController == null)
            {
                Debug.LogError("WeaponNotice: �Ҳ���PlayerInputController�������ֶ�����������������á�");
                return;
            }
        }

        // ��֤UI���
        if (weaponIcon == null)
        {
            Debug.LogError("WeaponNotice: ���������ͼ��UI�����");
            return;
        }

        if (ammoInfoText == null)
        {
            Debug.LogError("WeaponNotice: ����䵯ҩ��Ϣ�ı������");
            return;
        }

        // ��ʼ��UI״̬
        InitializeUI();

        // �����������������¼�
        SubscribeToWeaponInspectionInput();
    }

    void Update()
    {
        if (player != null)
        {
            UpdateWeaponIcon();

            // �����ҩ��Ϣ�ɼ���ʵʱ��������
            if (isAmmoInfoVisible)
            {
                UpdateAmmoInfoDisplay();
            }
        }
    }

    void OnDestroy()
    {
        // ȡ�����������¼�
        UnsubscribeFromWeaponInspectionInput();
    }

    // �����������������¼�
    private void SubscribeToWeaponInspectionInput()
    {
        if (inputController != null)
        {
            inputController.onWeaponInspection += OnWeaponInspection;
        }
    }

    // ȡ�������������������¼�
    private void UnsubscribeFromWeaponInspectionInput()
    {
        if (inputController != null)
        {
            inputController.onWeaponInspection -= OnWeaponInspection;
        }
    }

    // �������Ӵ���
    private void OnWeaponInspection()
    {
        // ֱ�Ӹ��µ�ҩ��Ϣ��ʾ�����л���ʾ״̬
        UpdateAmmoInfoDisplay();

        // ȷ����ҩ��Ϣʼ�տɼ�
        if (ammoInfoText != null && !ammoInfoText.gameObject.activeInHierarchy)
        {
            ammoInfoText.gameObject.SetActive(true);
        }

        Debug.Log("��������: ���µ�ҩ��Ϣ");
    }

    // ��ʼ��UI״̬
    private void InitializeUI()
    {
        if (weaponIcon != null)
        {
            // ��ʼ״̬������ͼ��
            if (hideWhenNoWeapon)
            {
                weaponIcon.gameObject.SetActive(false);
            }
            else
            {
                Color iconColor = weaponIcon.color;
                iconColor.a = 0f;
                weaponIcon.color = iconColor;
            }
        }

        // ��ʼ����ҩ��Ϣ�ı� - ����Ϊʼ����ʾ
        if (ammoInfoText != null)
        {
            ammoInfoText.gameObject.SetActive(true);
            UpdateAmmoInfoDisplay(); // ��ʼ��ʱ����һ����ʾ����
        }

        isAmmoInfoVisible = true; // ����Ϊʼ�տɼ�
    }

    // �����������ֶ���������ͼ��
    public void RefreshWeaponIcon()
    {
        if (player != null)
        {
            currentWeapon = null; // ǿ��ˢ��
            UpdateWeaponIcon();
        }
    }

    // ��������������ͼ��͸����
    public void SetIconAlpha(float alpha)
    {
        defaultAlpha = Mathf.Clamp01(alpha);

        if (weaponIcon != null && currentWeapon != null)
        {
            Color iconColor = weaponIcon.color;
            iconColor.a = defaultAlpha;
            weaponIcon.color = iconColor;
        }
    }

    // ������������ȡ��ǰ������Ϣ
    public string GetCurrentWeaponName()
    {
        if (currentWeapon != null)
        {
            return currentWeapon.GetWeaponName();
        }
        return "������";
    }

    // �����������ֶ�������������
    public void TriggerWeaponInspection()
    {
        OnWeaponInspection();
    }

    // �����������������ص�ҩ��Ϣ
    public void HideAmmoInfo()
    {
        if (ammoInfoText != null)
        {
            ammoInfoText.gameObject.SetActive(false);
        }

        isAmmoInfoVisible = false;
    }

    // ����������ǿ����ʾ��ҩ��Ϣ
    public void ForceShowAmmoInfo()
    {
        isAmmoInfoVisible = true;
        ShowAmmoInfo();
    }



    // ���µ�ҩ��Ϣ��ʾ����
    private void UpdateAmmoInfoDisplay()
    {
        if (ammoInfoText == null) return;

        if (currentWeapon != null)
        {
            int currentAmmo = currentWeapon.GetCurrentAmmo();
            string ammoStatusText = GetAmmoStatusText(currentAmmo);

            // ��ʾ��ҩ��Ϣ
            ammoInfoText.text = $"{ammoStatusText}";
        }
        else
        {
            ammoInfoText.text = "��ǰû�г�������";
        }
    }

    // ���ݵ�ҩ������ȡ״̬����
    private string GetAmmoStatusText(int ammoCount)
    {
        if (ammoCount == 0)
        {
            return "�޵�ҩ";
        }
        else if (ammoCount > 25)
        {
            return "���кܶ�";
        }
        else if (ammoCount > 20)
        {
            return "�ӵ��϶�";
        }
        else if (ammoCount >= 14 && ammoCount <= 16)
        {
            return "��ʣһ��";
        }
        else if (ammoCount < 10)
        {
            return "��ʣ�޼�";
        }
        else
        {
            return "��ҩ����";
        }
    }

    // ��������ͼ��
    private void UpdateWeaponIcon()
    {
        // �������Ƿ��������
        if (player.isWeaponInHand && player.Hand != null && player.Hand.childCount > 0)
        {
            // ��ȡHand�Ӷ����е�����
            Transform weaponTransform = player.Hand.GetChild(0);
            WeaponManager weaponManager = weaponTransform.GetComponent<WeaponManager>();

            // ������������仯������ͼ��
            if (weaponManager != currentWeapon)
            {
                currentWeapon = weaponManager;
                UpdateWeaponSprite(weaponTransform);
            }
        }
        else
        {
            // û������ʱ����ͼ��
            if (currentWeapon != null)
            {
                currentWeapon = null;
                currentWeaponSprite = null;
                HideWeaponIcon();
            }
        }
    }

    // ������������
    private void UpdateWeaponSprite(Transform weaponTransform)
    {
        if (weaponTransform == null || weaponIcon == null) return;

        // ��ȡ������SpriteRenderer���
        SpriteRenderer weaponSpriteRenderer = weaponTransform.GetComponent<SpriteRenderer>();

        if (weaponSpriteRenderer != null && weaponSpriteRenderer.sprite != null)
        {
            currentWeaponSprite = weaponSpriteRenderer;

            // �������ľ���ͼƬͬ����UIͼ��
            weaponIcon.sprite = weaponSpriteRenderer.sprite;

            // ��ʾͼ��
            ShowWeaponIcon();

            Debug.Log($"WeaponNotice: ��������ͼ�� - {weaponTransform.name}");
        }
        else
        {
            Debug.LogWarning($"WeaponNotice: ���� {weaponTransform.name} û��SpriteRenderer�������ͼƬ��");
            HideWeaponIcon();
        }
    }

    // ��ʾ����ͼ��
    private void ShowWeaponIcon()
    {
        if (weaponIcon == null) return;

        // ����ͼ��GameObject
        if (!weaponIcon.gameObject.activeInHierarchy)
        {
            weaponIcon.gameObject.SetActive(true);
        }

        // ����͸����
        Color iconColor = weaponIcon.color;
        iconColor.a = defaultAlpha;
        weaponIcon.color = iconColor;
    }

    // ��������ͼ��
    private void HideWeaponIcon()
    {
        if (weaponIcon == null) return;

        if (hideWhenNoWeapon)
        {
            // ��ȫ����GameObject
            weaponIcon.gameObject.SetActive(false);
        }
        else
        {
            // ֻ����Ϊ͸��
            Color iconColor = weaponIcon.color;
            iconColor.a = 0f;
            weaponIcon.color = iconColor;
        }

        // ��վ���
        weaponIcon.sprite = null;
    }

    // ��ʾ��ҩ��Ϣ
    private void ShowAmmoInfo()
    {
        if (ammoInfoText == null) return;

        UpdateAmmoInfoDisplay();
        ammoInfoText.gameObject.SetActive(true);

        Debug.Log("��������: ��ʾ��ҩ��Ϣ");
    }
}