using UnityEngine;
using UnityEngine.XR;

public class UICamera : MonoBehaviour
{
    [Header("UI điều khiển bằng tay phải (Secondary button)")]
    [SerializeField] private GameObject UISetting;

    [Header("UI điều khiển bằng tay trái (Primary button = X)")]
    [SerializeField] private GameObject startGameMenuUI;

    private bool isUISettingOpen = false;
    private bool isStartGameMenuOpen = false;

    private bool rightSecondaryPrev = false;
    private bool leftPrimaryPrev = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleUISetting();
        if (Input.GetKeyDown(KeyCode.O))
            ToggleStartGameMenu();

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid &&
            rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightPressed))
        {
            if (rightPressed && !rightSecondaryPrev)
                ToggleUISetting();
            rightSecondaryPrev = rightPressed;
        }

        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid &&
            leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPressed))
        {
            if (leftPressed && !leftPrimaryPrev)
                ToggleStartGameMenu();
            leftPrimaryPrev = leftPressed;
        }
    }

    public void ToggleUISetting()
    {
        if (!UISetting) return;

        isUISettingOpen = !isUISettingOpen;
        UISetting.SetActive(isUISettingOpen);
            Debug.Log((isUISettingOpen ? "Show" : "Hide") + " UISetting");
    }

    public void ToggleStartGameMenu()
    {
        if (!startGameMenuUI) return;

        isStartGameMenuOpen = !isStartGameMenuOpen;
        startGameMenuUI.SetActive(isStartGameMenuOpen);
        Debug.Log((isStartGameMenuOpen ? "Show" : "Hide") + " StartGameMenuUI");
    }
}