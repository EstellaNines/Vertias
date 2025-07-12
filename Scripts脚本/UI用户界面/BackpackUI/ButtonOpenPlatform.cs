using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonOpenPlatform : MonoBehaviour
{
    [Header("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77")]
    [FieldLabel("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค7ÿ0ุ01ÿ1ค77")] public Button[] buttons; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ8ท11ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Inspectorÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77

    [Header("RawImageÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77")]
    [FieldLabel("RawImageÿ1ค71ÿ1ค77ÿ1ค7ÿ0ุ01ÿ1ค77")] public RawImage[] rawImages; // RawImageÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ8ท11ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78

    [Header("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77")]
    [FieldLabel("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72")] public Color normalColor = Color.white; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
    [FieldLabel("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72")] public Color pressedColor = Color.gray; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
    [FieldLabel("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72")] public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f); // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    [FieldLabel("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72")] public Color disabledColor = Color.gray; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7ÿ0๑30ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    private int currentSelectedIndex = -1;

    // ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ค7ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    private ColorBlock[] originalColorBlocks;

    private void Start()
    {
        rawImages[0].gameObject.SetActive(true); // ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
        InitializeButtons();
        InitializeRawImages();
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
    private void InitializeButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77");
            return;
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        originalColorBlocks = new ColorBlock[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
                originalColorBlocks[i] = buttons[i].colors;

                // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
                SetButtonColors(i, false);

                // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
                int buttonIndex = i; // ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
                buttons[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
            }
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
    private void InitializeRawImages()
    {
        if (rawImages == null || rawImages.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImageÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77");
            return;
        }
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
        for (int i = 0; i < rawImages.Length; i++)
        {
            if (rawImages[i] != null)
            {
                rawImages[i].gameObject.SetActive(false);
            }
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    private void OnButtonClicked(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length || buttons[buttonIndex] == null)
            return;

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7ÿ0๑30ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7ÿ0ด1ÿ0ÿ41ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        if (currentSelectedIndex == buttonIndex)
        {
            Debug.Log("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72 " + buttonIndex + " ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ค7ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70");
            return;
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7งๆ1ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
        if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Length)
        {
            HideRawImage(currentSelectedIndex);
            SetButtonColors(currentSelectedIndex, false);
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7งๆ1ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
        ShowRawImage(buttonIndex);
        SetButtonColors(buttonIndex, true);

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        currentSelectedIndex = buttonIndex;

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77
        OnButtonSelected(buttonIndex);

        Debug.Log("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72 " + buttonIndex + " ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ค7ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage");
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
    private void ShowRawImage(int index)
    {
        if (rawImages != null && index >= 0 && index < rawImages.Length && rawImages[index] != null)
        {
            rawImages[index].gameObject.SetActive(true);
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
    private void HideRawImage(int index)
    {
        if (rawImages != null && index >= 0 && index < rawImages.Length && rawImages[index] != null)
        {
            rawImages[index].gameObject.SetActive(false);
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
    private void HideAllRawImages()
    {
        if (rawImages != null)
        {
            for (int i = 0; i < rawImages.Length; i++)
            {
                HideRawImage(i);
            }
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
    private void SetButtonColors(int buttonIndex, bool isSelected)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length || buttons[buttonIndex] == null)
            return;

        ColorBlock colorBlock = buttons[buttonIndex].colors;

        if (isSelected)
        {
            // ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
            colorBlock.normalColor = pressedColor;
            colorBlock.highlightedColor = pressedColor;
            colorBlock.pressedColor = pressedColor;
            colorBlock.selectedColor = pressedColor;
        }
        else
        {
            // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
            colorBlock.normalColor = normalColor;
            colorBlock.highlightedColor = hoverColor;
            colorBlock.pressedColor = pressedColor;
            colorBlock.selectedColor = normalColor;
        }

        colorBlock.disabledColor = disabledColor;
        colorBlock.colorMultiplier = 1f;
        colorBlock.fadeDuration = 0.1f;

        buttons[buttonIndex].colors = colorBlock;
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7ÿ0๋21ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    protected virtual void OnButtonSelected(int buttonIndex)
    {
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ÿ01ÿ1ค77ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค78ÿ1ค71ÿ1ค77
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7ÿ0ÿ61ÿ1ค77ÿ1ค7ÿ1ÿ41ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค73ÿ1ค71ÿ1ค77

        switch (buttonIndex)
        {
            case 0:
                Debug.Log("ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ8ฃ91ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage");
                break;
            case 1:
                Debug.Log("ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage");
                break;
            case 2:
                Debug.Log("ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage");
                break;
            default:
                Debug.Log("ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77 " + (buttonIndex + 1) + " ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77 " + (buttonIndex + 1) + " ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage");
                break;
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7งๆ1ÿ1ค77ÿ1ค70ÿ1ค72
    public void SelectButton(int buttonIndex)
    {
        OnButtonClicked(buttonIndex);
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค7ÿ0๑30ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    public int GetSelectedButtonIndex()
    {
        return currentSelectedIndex;
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7งๆ1ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70
    public void ClearSelection()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Length)
        {
            SetButtonColors(currentSelectedIndex, false);
            HideRawImage(currentSelectedIndex);
        }
        currentSelectedIndex = -1;
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    public void SetButtonInteractable(int buttonIndex, bool interactable)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Length && buttons[buttonIndex] != null)
        {
            buttons[buttonIndex].interactable = interactable;
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
    public RawImage GetRawImage(int buttonIndex)
    {
        if (rawImages != null && buttonIndex >= 0 && buttonIndex < rawImages.Length)
        {
            return rawImages[buttonIndex];
        }
        return null;
    }

    private void OnDestroy()
    {
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].onClick.RemoveAllListeners();
                }
            }
        }
    }
}
