using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBackground : MonoBehaviour
{
    [Header("����ͼƬ����")]
    [Tooltip("������ʾ����ͼƬ��Image���")]
    public Image backgroundImage;

    [Tooltip("Resources�ļ����б���ͼƬ��·�����������ļ���չ��")]
    public string backgroundFolderPath = "LoadingBackground";

    [Tooltip("�Ƿ����������ʱ�Զ������������")]
    public bool setRandomOnEnable = true;

    [Tooltip("���뵭������ʱ�䣨�룩")]
    public float fadeTransitionTime = 0.5f;

    [Tooltip("֧�ֵ��ļ�����׺�����ڼ��ض������ͼƬ����")]
    public string[] supportedExtensions = { "", "_1", "_2", "_3", "_4", "_5", "_6", "_7", "_8", "_9" };

    private List<Sprite> loadedSprites = new List<Sprite>();

    private void Awake()
    {
        LoadBackgroundSprites();
    }

    [Header("����Ч������")]
    [Tooltip("�Ƿ����õ��뵭��Ч��")]
    public bool enableFadeEffect = true;

    [Tooltip("����ʱ�䣨�룩")]
    public float fadeInTime = 0.5f;

    [Tooltip("����ʱ�䣨�룩")]
    public float fadeOutTime = 0.3f;

    private Coroutine fadeCoroutine;

    // �����뵭��Ч���������������
    public void SetRandomBackgroundWithFade()
    {
        if (enableFadeEffect)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeToRandomBackground());
        }
        else
        {
            SetRandomBackground();
        }
    }

    private IEnumerator FadeToRandomBackground()
    {
        if (backgroundImage == null) yield break;

        // ������ǰ����
        yield return StartCoroutine(FadeOut());

        // �����µ��������
        SetRandomBackground();

        // �����±���
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        Color startColor = backgroundImage.color;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / fadeOutTime);
            backgroundImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        backgroundImage.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        Color startColor = backgroundImage.color;

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInTime);
            backgroundImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        backgroundImage.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }

    // ��OnEnableʱ���ô�����Ч���ķ���
    private void OnEnable()
    {
        if (setRandomOnEnable)
        {
            if (enableFadeEffect)
            {
                SetRandomBackgroundWithFade();
            }
            else
            {
                SetRandomBackground();
            }
        }
    }

    void Start()
    {
        // ���û����OnEnable�����ã�����Start������
        if (!setRandomOnEnable)
        {
            SetRandomBackground();
        }
    }

    // ��Resources�ļ��м��ر�������ͼƬ
    private void LoadBackgroundSprites()
    {
        loadedSprites.Clear();

        // ���Լ���Resources/LoadingBackground�ļ����е����о���ͼƬ
        Sprite[] sprites = Resources.LoadAll<Sprite>(backgroundFolderPath);

        if (sprites != null && sprites.Length > 0)
        {
            loadedSprites.AddRange(sprites);
            Debug.Log($"RandomBackground: �ɹ��� Resources/{backgroundFolderPath} �ļ��м����� {sprites.Length} ����������ͼƬ��");
        }
        else
        {
            Debug.LogWarning($"RandomBackground: �޷��� Resources/{backgroundFolderPath} �ļ��м��ؾ���ͼƬ�����Ե������ء�");

            // �����������ʧ�ܣ����԰����������򵥶����أ�background, background_1, background_2��
            for (int i = 0; i < supportedExtensions.Length; i++)
            {
                string spriteName = $"{backgroundFolderPath}/background{supportedExtensions[i]}";
                Sprite sprite = Resources.Load<Sprite>(spriteName);
                if (sprite != null)
                {
                    loadedSprites.Add(sprite);
                    Debug.Log($"RandomBackground: �ɹ����ص�������ͼƬ - {spriteName}");
                }
            }
        }

        if (loadedSprites.Count == 0)
        {
            Debug.LogError($"RandomBackground: �޷������κα�������ͼƬ�� Resources/{backgroundFolderPath} ·���������ļ��Ƿ������·����ȷ��");
        }
    }

    // �����������
    public void SetRandomBackground()
    {
        if (loadedSprites.Count == 0)
        {
            Debug.LogWarning("RandomBackground: û�п��õı�������ͼƬ��");
            return;
        }

        if (backgroundImage == null)
        {
            Debug.LogError("RandomBackground: ����Image���δ���ã�");
            return;
        }

        // ���ѡ��һ������ͼƬ
        int randomIndex = Random.Range(0, loadedSprites.Count);
        Sprite selectedSprite = loadedSprites[randomIndex];

        // ���ñ���ͼƬ
        backgroundImage.sprite = selectedSprite;

        Debug.Log($"RandomBackground: ����������� - ����: {randomIndex}, ��������: {selectedSprite.name}");
    }

    // ����ָ�������ı���
    public void SetBackgroundByIndex(int index)
    {
        if (loadedSprites.Count == 0)
        {
            Debug.LogWarning("RandomBackground: û�п��õı�������ͼƬ��");
            return;
        }

        if (backgroundImage == null)
        {
            Debug.LogError("RandomBackground: ����Image���δ���ã�");
            return;
        }

        if (index < 0 || index >= loadedSprites.Count)
        {
            Debug.LogWarning($"RandomBackground: ���� {index} ������Χ�����÷�Χ: 0-{loadedSprites.Count - 1}");
            return;
        }

        backgroundImage.sprite = loadedSprites[index];
        Debug.Log($"RandomBackground: ���ñ������� {index} - ��������: {loadedSprites[index].name}");
    }

    // ��ȡ���صľ���ͼƬ����
    public int GetLoadedSpritesCount()
    {
        return loadedSprites.Count;
    }

    // ���¼��ر�������ͼƬ
    public void ReloadBackgroundSprites()
    {
        LoadBackgroundSprites();
    }
}