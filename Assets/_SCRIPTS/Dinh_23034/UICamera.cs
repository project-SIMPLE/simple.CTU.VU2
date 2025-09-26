using UnityEngine;
using UnityEngine.XR;

public class UICamera : MonoBehaviour
{
    [SerializeField] private GameObject UISetting;
    private bool isUIOpen = false;

    private bool secondaryButtonPrevState = false;

    private void Update()
    {
        // Toggle bằng phím M (keyboard test)
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleUI();
        }

        // Toggle bằng nút Secondary VR
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonPressed))
        {
            if (secondaryButtonPressed && !secondaryButtonPrevState)
            {
                ToggleUI();
            }
            secondaryButtonPrevState = secondaryButtonPressed;
        }
    }

    public void ToggleUI()
    {
        isUIOpen = !isUIOpen;
        UISetting.SetActive(isUIOpen);
        Debug.Log((isUIOpen ? "Show" : "Hide") + " UI cho plot: " + gameObject.name);
    }
}
