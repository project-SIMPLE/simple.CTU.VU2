using UnityEngine;
using UnityEngine.XR;

public class PlantArea : MonoBehaviour
{
    [Header("Setup")]
    public Transform plantPoint;
    public int panelIndex;
    private GameObject currentPlant;

    private bool playerInside = false;
    private bool isUIOpen = false;

    private bool primaryButtonPrevState = false;

    public static PlantArea currentActivePlot;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            currentActivePlot = this;
            Debug.Log("Player vào plot: " + gameObject.name + " -> Panel " + panelIndex);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (currentActivePlot == this) currentActivePlot = null;
            HideUI();
            Debug.Log("Player rời plot: " + gameObject.name);
        }
    }

    void Update()
    {
        if (!playerInside) return;

        // Keyboard test
        if (Input.GetKeyDown(KeyCode.X))
        {
            ToggleUI();
        }

        // VR controller
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonPressed))
        {
            // Chỉ toggle khi nút vừa được nhấn (rising edge)
            if (primaryButtonPressed && !primaryButtonPrevState)
            {
                ToggleUI();
            }

            primaryButtonPrevState = primaryButtonPressed;
        }
    }

    private void ToggleUI()
    {
        if (!isUIOpen)
        {
            ShowUI();
        }
        else
        {
            HideUI();
        }
        isUIOpen = !isUIOpen;
    }

    private void ShowUI()
    {
        PlantUIManager.Instance.ShowUI(panelIndex);
        Debug.Log("Show UI cho plot: " + gameObject.name + " -> Panel " + panelIndex);
    }

    private void HideUI()
    {
        PlantUIManager.Instance.HideUI();
        Debug.Log("Hide UI cho plot: " + gameObject.name);
    }

    // Gọi khi chọn cây
    public void Plant(GameObject plantPrefab)
    {
        if (currentPlant != null) Destroy(currentPlant);
        currentPlant = Instantiate(plantPrefab, plantPoint.position, plantPoint.rotation, plantPoint);
        Debug.Log("Đã trồng " + plantPrefab.name + " vào plot " + gameObject.name);
    }
}
