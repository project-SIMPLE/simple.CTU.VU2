using UnityEngine;
using UnityEngine.EventSystems;

public class PlantButton : MonoBehaviour, IPointerEnterHandler
{
    public int plantId;  // gán trong Inspector
    public PlantInfoUI plantInfoUI; // tham chi?u t?i UI hi?n th?

    // G?i khi hover vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (plantInfoUI != null)
        {
            plantInfoUI.ShowInfo(plantId);
        }
    }
}
