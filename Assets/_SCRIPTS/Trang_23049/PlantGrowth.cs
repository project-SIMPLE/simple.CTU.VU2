using System.Collections;
using UnityEngine;

public class PlantGrowth : MonoBehaviour
{
    public PlantDialogue plantData;

    private GameObject currentPlant;
    private int stage = 0;  // 0 = small, 1 = medium, 2 = large, -1 = dead
    private float timer;
    private Coroutine sickRoutine;

    private PlantProgress progressUI; // UI để hiển thị trạng thái

    public int Stage => stage;
    public bool IsSick => sickRoutine != null;

    void Start()
    {
        if (plantData == null) return;

        // Spawn smallPrefab lúc đầu
        currentPlant = Instantiate(plantData.smallPrefab, transform.position, Quaternion.identity, transform);
        stage = 0;
        timer = plantData.timeStage1;

        // Tìm UI
        progressUI = FindObjectOfType<PlantProgress>();
        UpdateUI();
    }

    void Update()
    {
        // Nếu cây đã chết thì bỏ qua
        if (stage == -1) return;

        if (stage < 2)
        {
            if (GameVariables.currentSalt > plantData.saltTolerance)
            {
                if (sickRoutine == null)
                {
                    sickRoutine = StartCoroutine(HandleSickness());
                    UpdateUI(); // gọi ngay khi bắt đầu bệnh
                }
            }

            if (sickRoutine == null)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                    NextStage();
            }
        }
    }

    void NextStage()
    {
        if (stage == 0)
        {
            stage = 1;
            ReplacePlant(plantData.mediumPrefab);
            timer = plantData.timeStage2;
        }
        else if (stage == 1)
        {
            stage = 2;
            ReplacePlant(plantData.largePrefab);
        }

        UpdateUI();
    }

    void ReplacePlant(GameObject prefab)
    {
        if (prefab == null) return;

        if (currentPlant != null && currentPlant != this.gameObject)
            Destroy(currentPlant);

        currentPlant = Instantiate(prefab, transform.position, Quaternion.identity, transform);
    }

    IEnumerator HandleSickness()
    {
        ReplacePlant(plantData.sickPrefab);
        UpdateUI(); // cập nhật sang Sick

        float sickTime = 5f;

        while (sickTime > 0f)
        {
            if (GameVariables.currentSalt <= plantData.saltTolerance)
            {
                // Nếu muối giảm -> hồi phục
                sickRoutine = null;
                UpdateUI(); // quay lại Good
                yield break;
            }
            sickTime -= Time.deltaTime;
            yield return null;
        }

        // Sau 5s không giảm muối -> chết
        ReplacePlant(plantData.deadPrefab);
        stage = -1;
        sickRoutine = null;
        UpdateUI(); // cập nhật sang Dead
    }

    private void UpdateUI()
    {
        if (progressUI != null)
            progressUI.ShowPlantInfo(this);
    }
}
