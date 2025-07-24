using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBackground : MonoBehaviour
{
    [Header("背景图片设置")]
    [Tooltip("用于显示背景图片的Image组件")]
    public Image backgroundImage;

    [Tooltip("Resources文件夹中背景图片的路径，不包含文件扩展名")]
    public string backgroundFolderPath = "LoadingBackground";

    [Tooltip("是否在组件启用时自动设置随机背景")]
    public bool setRandomOnEnable = true;

    [Tooltip("淡入淡出过渡时间（秒）")]
    public float fadeTransitionTime = 0.5f;

    [Tooltip("支持的文件名后缀，用于加载多个背景图片变体")]
    public string[] supportedExtensions = { "", "_1", "_2", "_3", "_4", "_5", "_6", "_7", "_8", "_9" };

    private List<Sprite> loadedSprites = new List<Sprite>();

    private void Awake()
    {
        LoadBackgroundSprites();
    }

    [Header("动画效果设置")]
    [Tooltip("是否启用淡入淡出效果")]
    public bool enableFadeEffect = true;

    [Tooltip("淡入时间（秒）")]
    public float fadeInTime = 0.5f;

    [Tooltip("淡出时间（秒）")]
    public float fadeOutTime = 0.3f;

    private Coroutine fadeCoroutine;

    // 带淡入淡出效果的设置随机背景
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

        // 淡出当前背景
        yield return StartCoroutine(FadeOut());

        // 设置新的随机背景
        SetRandomBackground();

        // 淡入新背景
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

    // 在OnEnable时调用带淡入效果的方法
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
        // 如果没有在OnEnable中设置，则在Start中设置
        if (!setRandomOnEnable)
        {
            SetRandomBackground();
        }
    }

    // 从Resources文件夹加载背景精灵图片
    private void LoadBackgroundSprites()
    {
        loadedSprites.Clear();

        // 尝试加载Resources/LoadingBackground文件夹中的所有精灵图片
        Sprite[] sprites = Resources.LoadAll<Sprite>(backgroundFolderPath);

        if (sprites != null && sprites.Length > 0)
        {
            loadedSprites.AddRange(sprites);
            Debug.Log($"RandomBackground: 成功从 Resources/{backgroundFolderPath} 文件夹加载了 {sprites.Length} 个背景精灵图片。");
        }
        else
        {
            Debug.LogWarning($"RandomBackground: 无法从 Resources/{backgroundFolderPath} 文件夹加载精灵图片，尝试单独加载。");

            // 如果批量加载失败，尝试按照命名规则单独加载：background, background_1, background_2等
            for (int i = 0; i < supportedExtensions.Length; i++)
            {
                string spriteName = $"{backgroundFolderPath}/background{supportedExtensions[i]}";
                Sprite sprite = Resources.Load<Sprite>(spriteName);
                if (sprite != null)
                {
                    loadedSprites.Add(sprite);
                    Debug.Log($"RandomBackground: 成功加载单个精灵图片 - {spriteName}");
                }
            }
        }

        if (loadedSprites.Count == 0)
        {
            Debug.LogError($"RandomBackground: 无法加载任何背景精灵图片从 Resources/{backgroundFolderPath} 路径。请检查文件是否存在且路径正确。");
        }
    }

    // 设置随机背景
    public void SetRandomBackground()
    {
        if (loadedSprites.Count == 0)
        {
            Debug.LogWarning("RandomBackground: 没有可用的背景精灵图片。");
            return;
        }

        if (backgroundImage == null)
        {
            Debug.LogError("RandomBackground: 背景Image组件未设置！");
            return;
        }

        // 随机选择一个精灵图片
        int randomIndex = Random.Range(0, loadedSprites.Count);
        Sprite selectedSprite = loadedSprites[randomIndex];

        // 设置背景图片
        backgroundImage.sprite = selectedSprite;

        Debug.Log($"RandomBackground: 设置随机背景 - 索引: {randomIndex}, 精灵名称: {selectedSprite.name}");
    }

    // 设置指定索引的背景
    public void SetBackgroundByIndex(int index)
    {
        if (loadedSprites.Count == 0)
        {
            Debug.LogWarning("RandomBackground: 没有可用的背景精灵图片。");
            return;
        }

        if (backgroundImage == null)
        {
            Debug.LogError("RandomBackground: 背景Image组件未设置！");
            return;
        }

        if (index < 0 || index >= loadedSprites.Count)
        {
            Debug.LogWarning($"RandomBackground: 索引 {index} 超出范围。可用范围: 0-{loadedSprites.Count - 1}");
            return;
        }

        backgroundImage.sprite = loadedSprites[index];
        Debug.Log($"RandomBackground: 设置背景索引 {index} - 精灵名称: {loadedSprites[index].name}");
    }

    // 获取加载的精灵图片数量
    public int GetLoadedSpritesCount()
    {
        return loadedSprites.Count;
    }

    // 重新加载背景精灵图片
    public void ReloadBackgroundSprites()
    {
        LoadBackgroundSprites();
    }
}