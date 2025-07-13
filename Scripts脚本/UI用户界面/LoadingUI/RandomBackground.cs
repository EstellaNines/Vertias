using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomBackground : MonoBehaviour
{
    [Header("随机背景设置")]
    [Tooltip("背景图片组件")]
    public Image backgroundImage;

    [Tooltip("Resources中背景图片文件夹路径")]
    public string backgroundFolderPath = "LoadingBackground";

    [Tooltip("是否在启用时自动设置随机背景")]
    public bool setRandomOnEnable = true;

    [Tooltip("背景切换动画时间")]
    public float fadeTransitionTime = 0.5f;

    [Tooltip("支持的图片格式后缀")]
    public string[] supportedExtensions = { "", "_1", "_2", "_3", "_4", "_5", "_6", "_7", "_8", "_9" };

    private List<Sprite> loadedSprites = new List<Sprite>();

    private void Awake()
    {
        LoadBackgroundSprites();
    }

    private void OnEnable()
    {
        if (setRandomOnEnable)
        {
            SetRandomBackground();
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

    // 从Resources文件夹加载背景图片
    private void LoadBackgroundSprites()
    {
        loadedSprites.Clear();

        // 尝试加载Resources/LoadingBackground文件夹中的所有图片
        Sprite[] sprites = Resources.LoadAll<Sprite>(backgroundFolderPath);

        if (sprites != null && sprites.Length > 0)
        {
            loadedSprites.AddRange(sprites);
            Debug.Log($"RandomBackground: 从 Resources/{backgroundFolderPath} 加载了 {sprites.Length} 张背景图片");
        }
        else
        {
            Debug.LogWarning($"RandomBackground: 在 Resources/{backgroundFolderPath} 中没有找到背景图片");

            // 尝试加载单个命名的图片（如background, background_1, background_2等）
            for (int i = 0; i < supportedExtensions.Length; i++)
            {
                string spriteName = $"{backgroundFolderPath}/background{supportedExtensions[i]}";
                Sprite sprite = Resources.Load<Sprite>(spriteName);
                if (sprite != null)
                {
                    loadedSprites.Add(sprite);
                    Debug.Log($"RandomBackground: 加载背景图片 - {spriteName}");
                }
            }
        }

        if (loadedSprites.Count == 0)
        {
            Debug.LogError($"RandomBackground: 无法在 Resources/{backgroundFolderPath} 中找到任何背景图片！");
        }
    }

    // 设置随机背景图片
    public void SetRandomBackground()
    {
        if (backgroundImage == null)
        {
            Debug.LogWarning("RandomBackground: 背景图片组件未设置");
            return;
        }

        if (loadedSprites.Count == 0)
        {
            Debug.LogWarning("RandomBackground: 没有可用的背景图片，尝试重新加载");
            LoadBackgroundSprites();

            if (loadedSprites.Count == 0)
            {
                Debug.LogError("RandomBackground: 重新加载后仍然没有可用的背景图片");
                return;
            }
        }

        // 随机选择一张背景图片
        int randomIndex = Random.Range(0, loadedSprites.Count);
        Sprite selectedSprite = loadedSprites[randomIndex];

        if (selectedSprite != null)
        {
            // 如果启用了淡入淡出效果
            if (fadeTransitionTime > 0)
            {
                StartCoroutine(FadeToNewBackground(selectedSprite));
            }
            else
            {
                // 直接设置背景
                backgroundImage.sprite = selectedSprite;
                Debug.Log($"RandomBackground: 设置随机背景图片 - {selectedSprite.name}");
            }
        }
        else
        {
            Debug.LogWarning($"RandomBackground: 背景图片索引 {randomIndex} 为空");
        }
    }

    // 淡入淡出切换背景
    private IEnumerator FadeToNewBackground(Sprite newSprite)
    {
        Color originalColor = backgroundImage.color;

        // 淡出当前背景
        float elapsedTime = 0;
        while (elapsedTime < fadeTransitionTime / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, 0, elapsedTime / (fadeTransitionTime / 2));
            backgroundImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // 更换背景图片
        backgroundImage.sprite = newSprite;
        Debug.Log($"RandomBackground: 设置随机背景图片 - {newSprite.name}");

        // 淡入新背景
        elapsedTime = 0;
        while (elapsedTime < fadeTransitionTime / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0, originalColor.a, elapsedTime / (fadeTransitionTime / 2));
            backgroundImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // 确保最终透明度正确
        backgroundImage.color = originalColor;
    }

    // 获取当前加载的背景图片数量
    public int GetLoadedSpritesCount()
    {
        return loadedSprites.Count;
    }
}
