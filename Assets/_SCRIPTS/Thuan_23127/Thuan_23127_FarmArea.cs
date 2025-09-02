using System.Collections;
using UnityEngine;

public class Thuan_23127_FarmArea : MonoBehaviour
{
    [Header("Setup")]
    public Transform[] plotPoints;

    private bool[] isPlanted;
    public float spawnYOffset = 0.5f;
    private void Start()
    {
        isPlanted = new bool[plotPoints.Length];
    }

    public void Plant(GameObject plantPrefab, Plant plantData)
    {
        if (plantPrefab == null) return;

        for (int i = 0; i < plotPoints.Length; i++)
        {
            if (!isPlanted[i] && plotPoints[i] != null)
            {
                // Sinh làm con của plot point để tránh lệch tọa độ / tỉ lệ
                var planted = Instantiate(plantPrefab, plotPoints[i]);
                planted.transform.localPosition = new Vector3(0, spawnYOffset, 0);
                planted.transform.localRotation = Quaternion.identity;
                planted.transform.localScale    = Vector3.one;

                isPlanted[i] = true;
                
                if (plantData != null)
                {
                    var info = planted.AddComponent<PlantInfo>();
                    info.data = plantData;

                    // planted.name = $"{plantData.tag_name}(Clone)";
                }
                
                float dieTime =
                    plantData != null &&
                    (plantData.tag_name.ToLower().Contains("sầu riêng") ||
                     plantData.tag_name.ToLower().Contains("durian"))
                        ? 40f : 20f;

                StartCoroutine(KillAfterSeconds(planted, dieTime,i));
                // break; // trồng 1 ô mỗi lần
            }
        }
    }

    private IEnumerator KillAfterSeconds(GameObject go, float seconds, int plotIndex)
    {
        yield return new WaitForSeconds(seconds);
        if (go) Destroy(go);
        
        // if (plotIndex >= 0 && plotIndex < isPlanted.Length)
        // {
        //     isPlanted[plotIndex] = false;
        // } // 
    }
}