using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackpackState : MonoBehaviour
{
    [Header("ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77")]
    [SerializeField] private Canvas backpackCanvas; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Canvasÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    [SerializeField] private PlayerInputController playerInputController; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    [SerializeField] private Button closeButton; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค79ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72
    [SerializeField] private ButtonOpenPlatform buttonOpenPlatform; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77

    [Header("ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77")]
    private bool isBackpackOpen = false; // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77

    private void Start()
    {
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค79ÿ1ค71ÿ1ค77
        if (backpackCanvas != null)
        {
            backpackCanvas.gameObject.SetActive(false);
            isBackpackOpen = false;
        }
        else
        {
            Debug.LogError("BackpackState: ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Canvasÿ1ค7ÿ0ÿ11ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Inspectorÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75Canvasÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77");
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ค7ÿ1ค71ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseBackpackByButton);
        }
        else
        {
            Debug.LogWarning("BackpackState: ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค79ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค7ÿ0ÿ11ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Inspectorÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75Buttonÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77");
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77
        if (playerInputController != null)
        {
            playerInputController.onBackPack += ToggleBackpack;

            // ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
            playerInputController.EnabledUIInput();
        }
        else
        {
            Debug.LogError("BackpackState: PlayerInputControllerÿ1ค7ÿ0ÿ11ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Inspectorÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75PlayerInputControllerÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค76");
        }
    }

    private void OnDestroy()
    {
        // ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ÿ30ÿ1ค78
        if (playerInputController != null)
        {
            playerInputController.onBackPack -= ToggleBackpack;
        }

        // ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค74ÿ1ค71ÿ1ค77
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseBackpackByButton);
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ÿ41ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70
    private void ToggleBackpack()
    {
        if (backpackCanvas == null)
        {
            Debug.LogWarning("BackpackState: ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Canvasÿ1ค7ÿ0ÿ11ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77");
            return;
        }

        isBackpackOpen = !isBackpackOpen;
        backpackCanvas.gameObject.SetActive(isBackpackOpen);

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค79ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ÿ41ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค70ÿ1ค74
        if (isBackpackOpen)
        {
            OpenBackpack();
        }
        else
        {
            CloseBackpack();
        }
    }

    // ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค79ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    private void CloseBackpackByButton()
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

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    private void OpenBackpack()
    {
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        if (playerInputController != null)
        {
            playerInputController.DisableGameplayInput();
            playerInputController.EnabledUIInput();
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค7
        ShowDefaultPanel();
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค79ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
    private void CloseBackpack()
    {
        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค79ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        if (playerInputController != null)
        {
            playerInputController.EnabledGameplayInput();
            // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77UIÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค73ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77Tabÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        }

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7งๆ1ÿ1ค77ÿ1ค71ÿ1ค77
        HideAllPanels();
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค78ÿ1ค71ÿ1ค77ÿ1ค7ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค7
    private void ShowDefaultPanel()
    {
        if (buttonOpenPlatform != null)
        {
            // ÿ1ค70ÿ1ค71ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค770ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค750ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77RawImage
            buttonOpenPlatform.SelectButton(0);
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค7งๆ1ÿ1ค77ÿ1ค71ÿ1ค77
    private void HideAllPanels()
    {
        if (buttonOpenPlatform != null)
        {
            buttonOpenPlatform.ClearSelection();
        }
    }

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค75ÿ1ค71ÿ1ค79ÿ1ค70ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
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

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค76ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77
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

    // ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค70ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค71ÿ1ค77ÿ1ค70ÿ1ค72ÿ1ค70ÿ1ค78ÿ1ค70ÿ1ค70
    public bool IsBackpackOpen()
    {
        return isBackpackOpen;
    }
}
