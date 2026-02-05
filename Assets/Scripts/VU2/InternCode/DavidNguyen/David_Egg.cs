using UnityEngine;

// Egg collectible configuration - works with David_Fruit for instant grab.
// Cấu hình trứng thu hoạch - hoạt động với David_Fruit cho instant grab.
// 
// SETUP INSTRUCTIONS:
// 1. Add this component to egg prefab
// 2. Add David_Fruit component (set fruitType = Egg)
// 3. Add Rigidbody (kinematic initially)
// 4. Add Collider (solid, not trigger)
// 5. Add XRGrabInteractable
// 
// HƯỚNG DẪN SETUP:
// 1. Thêm component này vào prefab trứng
// 2. Thêm component David_Fruit (đặt fruitType = Egg)
// 3. Thêm Rigidbody (kinematic ban đầu)
// 4. Thêm Collider (solid, không trigger)
// 5. Thêm XRGrabInteractable
public class David_Egg : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Points awarded when collected")]
    public int pointValue = 3;
    
    [Tooltip("Sound played when collected")]
    public AudioClip collectSound;
    
    // NOTE: Collision detection and scoring is handled by David_Fruit component.
    // LƯU Ý: Phát hiện va chạm và tính điểm được xử lý bởi component David_Fruit.
    
    private void Start()
    {
        // Validate setup
        var fruitComponent = GetComponent<David_Fruit>();
        if (fruitComponent == null)
        {
            Debug.LogError("[David_Egg] Missing David_Fruit component! Please add it and set fruitType = Egg");
        }
        else if (fruitComponent.fruitType != FruitType.Egg)
        {
            Debug.LogWarning($"[David_Egg] David_Fruit.fruitType should be Egg, but is {fruitComponent.fruitType}");
        }
        
        // Sync audio if needed
        if (collectSound != null && fruitComponent != null && fruitComponent.collectSound == null)
        {
            fruitComponent.collectSound = collectSound;
        }
    }
}
