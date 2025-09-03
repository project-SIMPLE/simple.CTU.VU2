using UnityEngine;
using UnityEngine.XR;

public class PlantArea : MonoBehaviour
{
    [Header("Setup")]
    public Transform plantPoint;   // Điểm trồng cây
    public int panelIndex;         // panel UI nào (0-3)
    private GameObject currentPlant;

    private bool playerInside = false;
    private bool isUIOpen = false;

    // static để biết plot nào đang active
    public static PlantArea currentActivePlot;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            currentActivePlot = this;   // ghi nhớ plot hiện tại
            Debug.Log("Player vào plot: " + gameObject.name + " -> Panel " + panelIndex);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            if (currentActivePlot == this) currentActivePlot = null;
            PlantUIManager.Instance.HideUI();
            Debug.Log("Player rời plot: " + gameObject.name);
        }
    }

    void Update()
    {
        if (playerInside)
        {
            // Bấm bàn phím test
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (!isUIOpen)
                {
                    PlantUIManager.Instance.ShowUI(panelIndex);
                    Debug.Log("Show UI cho plot: " + gameObject.name + " -> Panel " + panelIndex);
                }
                else
                {
                    PlantUIManager.Instance.HideUI();
                    Debug.Log("Hide UI cho plot: " + gameObject.name);
                }

                isUIOpen = !isUIOpen; // đảo trạng thái
            }

            // VR controller
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool buttonPressed) && buttonPressed)
            {
                PlantUIManager.Instance.ShowUI(panelIndex);
                Debug.Log("Show UI cho plot: " + gameObject.name + " -> Panel " + panelIndex);
            }
        }
    }

    // Gọi khi chọn cây
    public void Plant(GameObject plantPrefab)
    {
        if (currentPlant != null) Destroy(currentPlant);
        currentPlant = Instantiate(plantPrefab, plantPoint.position, plantPoint.rotation, plantPoint);
        Debug.Log("Đã trồng " + plantPrefab.name + " vào plot " + gameObject.name);
    }
}
