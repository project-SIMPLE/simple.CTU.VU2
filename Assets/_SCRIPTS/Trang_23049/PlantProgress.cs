using UnityEngine;
using UnityEngine.UI;

public class PlantProgress : MonoBehaviour
{
    [Header("UI References")]
    public Text nameText;
    public Text statusText;

    private LoaderData loader;

    void Start()
    {
        loader = FindObjectOfType<LoaderData>();
    }

    public void ShowPlantInfo(PlantGrowth plant)
    {
        if (loader == null || plant == null) return;

        PlantData data = loader.LoadJson().Find(p => p.id == plant.plantData.id);

        if (data != null)
        {
            // Gán tên cây
            nameText.text = data.tag_name;

            // Xác ??nh tr?ng thái
            if (plant.Stage == -1)
                statusText.text = data.status[2]; // Dead
            else if (plant.IsSick)
                statusText.text = data.status[1]; // Sick
            else
                statusText.text = data.status[0]; // Good
        }
    }
}
