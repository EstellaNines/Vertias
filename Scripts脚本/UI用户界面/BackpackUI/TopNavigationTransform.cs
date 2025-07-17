using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TopNavigationTransform : MonoBehaviour
{
    [Header("Image����")]
    [Header("Spriteһһ��Ӧ")]
    [SerializeField][FieldLabel("����������ť���")] private Image[] navigationImages; // ����Image����
    [SerializeField][FieldLabel("����״̬����ͼƬ")] private Sprite[] normalSprites; // ����״̬Sprite
    [SerializeField][FieldLabel("�������ͼƬ")] private Sprite[] clickedSprites; // �����Sprite

    [Header("�ر�Image����")]
    [SerializeField][FieldLabel("�رհ�ťImage���")] private Image closeImage; // �ر�Image���
    [SerializeField][FieldLabel("�رհ�ť����״̬����ͼƬ")] private Sprite closeNormalSprite; // �ر�����״̬Sprite
    [SerializeField][FieldLabel("�رհ�ť���״̬����ͼƬ")] private Sprite closeClickedSprite; // �رյ��״̬Sprite

    [Header("�������")]
    [SerializeField] private RawImage[] panels; // ��Ӧ���
    [SerializeField] private Canvas backpackCanvas; // ����Canvas

    private int currentSelectedIndex = -1;
    private bool isBackpackOpen = false;

    private void Start()
    {
        InitializeNavigation();
        InitializeCloseImage();
    }

    private void InitializeNavigation()
    {
        for (int i = 0; i < navigationImages.Length; i++)
        {
            int index = i;
            EventTrigger trigger = navigationImages[i].gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => OnNavigationClicked(index));
            trigger.triggers.Add(entry);

            navigationImages[i].sprite = normalSprites[i];
        }
        // Ĭ����ʾ��һ�����
        if (panels.Length > 0) panels[0].gameObject.SetActive(true);
    }

    private void InitializeCloseImage()
    {
        if (closeImage != null)
        {
            closeImage.sprite = closeNormalSprite;
            EventTrigger trigger = closeImage.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => OnCloseClicked());
            trigger.triggers.Add(entry);
        }
    }

    private void OnNavigationClicked(int index)
    {
        if (currentSelectedIndex == index) return;

        if (currentSelectedIndex >= 0)
        {
            panels[currentSelectedIndex].gameObject.SetActive(false);
            navigationImages[currentSelectedIndex].sprite = normalSprites[currentSelectedIndex];
        }

        panels[index].gameObject.SetActive(true);
        navigationImages[index].sprite = clickedSprites[index];

        currentSelectedIndex = index;
    }

    private void OnCloseClicked()
    {
        if (closeImage != null)
        {
            closeImage.sprite = closeClickedSprite; // �����л������״̬Sprite��Ϊ����
            StartCoroutine(ResetCloseSprite()); // �ӳٻָ�
        }
        CloseBackpack(); // ֱ�ӹرձ���
    }

    private void CloseBackpack()
    {
        if (isBackpackOpen)
        {
            isBackpackOpen = false;
            backpackCanvas.gameObject.SetActive(false);
            // �ָ������
        }
    }

    private IEnumerator ResetCloseSprite()
    {
        yield return new WaitForSeconds(0.2f); // �����ӳ�
        if (closeImage != null)
        {
            closeImage.sprite = closeNormalSprite;
        }
    }

    public void ToggleBackpack()
    {
        isBackpackOpen = !isBackpackOpen;
        backpackCanvas.gameObject.SetActive(isBackpackOpen);
        if (isBackpackOpen)
        {
            // Ĭ��ѡ�е�һ�������backpack���棩
            OnNavigationClicked(0);
            // Open�߼���������Ϸ���룬����UI���룬��ʾ����
            // �ɴ�BackpackStateǨ�ƶ���
        }
        else
        {
            // Close�߼���������Ϸ�����
        }
    }
}
