using UnityEngine;

public class PlantUIManager : MonoBehaviour
{
    public static PlantUIManager Instance; // Singleton để gọi từ PlantArea
    public GameObject[] uiPanels; // chứa 4 UI panel
    private int currentUI = -1;

    void Awake()
    {
        Instance = this;
        HideAllUI();
    }

    public void ShowUI(int index)
    {
        // Nếu UI này đang mở -> bấm lại sẽ tắt
        if (currentUI == index)
        {
            HideUI();
            return;
        }

        HideAllUI(); // ẩn hết trước
        if (index >= 0 && index < uiPanels.Length)
        {
            GameObject panel = uiPanels[index];
            panel.SetActive(true);
            currentUI = index;

            // --- Đặt UI trước mặt player ---
            Transform cam = Camera.main.transform;
            float distance = 2f; // khoảng cách trước mặt
            Vector3 targetPos = cam.position + cam.forward * distance;
            targetPos.y = cam.position.y; // ngang tầm mắt

            panel.transform.position = targetPos;

            // quay panel nhìn về phía player
            panel.transform.LookAt(cam);
            panel.transform.Rotate(0, 180f, 0); // lật lại vì canvas mặc định ngược
        }
    }

    public void HideUI()
    {
        if (currentUI >= 0 && currentUI < uiPanels.Length)
        {
            uiPanels[currentUI].SetActive(false);
            currentUI = -1;
        }
    }

    private void HideAllUI()
    {
        foreach (var panel in uiPanels)
        {
            panel.SetActive(false);
        }
        currentUI = -1;
    }
}
