using UnityEngine;

public class Thuan_23127_SeedTag : MonoBehaviour
{
    [Tooltip("ID Plant")]
    public int plantId = -1;
    [Tooltip("ID Animal")]
    public int animalId = -1; 
    [Tooltip("ID Fish")]
    public int fishId = -1;

    [Header("HUD visuals")]
    public Sprite hudIcon;
    
    public bool IsPlant => plantId > 0;
    public bool IsAnimal => animalId > 0;
    public bool IsFish => fishId > 0;
}
