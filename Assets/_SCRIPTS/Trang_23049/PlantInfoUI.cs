using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   // dùng cho Legacy Text & ScrollView

public class PlantInfoUI : MonoBehaviour
{
    [Header("UI References")]
    public Text titleText;        // Legacy Text để hiển thị tag_name
    public Text infoText;         // Text bên trong ScrollView (Content)

    [Header("Data")]
    public LoaderData loader;     // Script đã load dữ liệu JSON
    private List<PlantData> plants;

    void Start()
    {
        if (loader == null)
        {
            loader = FindObjectOfType<LoaderData>();
        }

        if (loader != null)
        {
            // Load dữ liệu một lần
            plants = loader.LoadJson();
        }
    }

    /// <summary>
    /// Gọi hàm này khi OnClick button, truyền vào id
    /// </summary>
    public void ShowInfo(int id)
    {
        if (plants == null || plants.Count == 0)
        {
            Debug.LogWarning("Chưa có dữ liệu plant!");
            return;
        }

        PlantData plant = plants.Find(p => p.id == id);

        if (plant != null)
        {
            // Cập nhật UI
            titleText.text = plant.tag_name;
            infoText.text = plant.information;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy cây có id = " + id);
        }
    }
}
