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
            // Toggle UI **chỉ khi nút vừa được nhấn** (rising edge)
            if (secondaryButtonPressed && !secondaryButtonPrevState)
            {
                ToggleUI();
            }

            // Cập nhật trạng thái nút cho frame tiếp theo
            secondaryButtonPrevState = secondaryButtonPressed;
        }
    }

    private void ToggleUI()
    {
        isUIOpen = !isUIOpen;  // đổi trạng thái
        UISetting.SetActive(isUIOpen);
        Debug.Log((isUIOpen ? "Show" : "Hide") + " UI cho plot: " + gameObject.name);
    }
}
