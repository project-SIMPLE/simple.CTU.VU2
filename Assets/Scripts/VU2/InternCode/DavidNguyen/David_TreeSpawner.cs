using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// David_TreeSpawner - Manages fruit spawning and respawning on trees.
// David_TreeSpawner - Quản lý việc spawn và respawn quả trên cây.
// 
// This script attaches to a tree GameObject and handles:
// - Initial fruit spawning on game start
// - Automatic fruit respawning when seasons change
// - Position reset for pre-placed fruits (prevents physics drift)
// 
// Script này gắn vào GameObject cây và xử lý:
// - Spawn quả ban đầu khi game bắt đầu
// - Tự động respawn quả khi mùa thay đổi
// - Reset vị trí cho quả đã đặt sẵn (ngăn trôi do vật lý)
// 
// TWO MODES:
// 1. Pre-placed mode: Fruits are already children of the tree, script just
//    manages their state (enable/disable, position reset)
// 2. Spawn mode: Fruits are instantiated from prefab at spawn points
// 
// HAI CHẾ ĐỘ:
// 1. Chế độ đặt sẵn: Quả đã là con của cây, script chỉ quản lý trạng thái
//    (bật/tắt, reset vị trí)
// 2. Chế độ spawn: Quả được instantiate từ prefab tại các điểm spawn
// =============================================================================
public class David_TreeSpawner : MonoBehaviour
{
    // =========================================================================
    // MODE CONFIGURATION
    // CẤU HÌNH CHẾ ĐỘ
    // =========================================================================
    [Header("Mode / Chế độ")]
    [Tooltip("If true: Find and manage existing David_Fruit children. If false: Spawn from prefab.")]
    // When true: Uses pre-placed fruits that are already in the scene.
    // When false: Spawns new fruits from prefab at spawn points.
    // Khi true: Dùng quả đã đặt sẵn trong scene.
    // Khi false: Spawn quả mới từ prefab tại các điểm spawn.
    public bool usePrePlacedFruits = true;

    // =========================================================================
    // PREFAB SPAWN MODE CONFIGURATION
    // CẤU HÌNH CHẾ ĐỘ SPAWN PREFAB
    // =========================================================================
    [Header("Fruit Prefab (Only when usePrePlacedFruits = false)")]
    [Tooltip("Drag fruit prefab with David_Fruit script here")]
    // Prefab to instantiate for each fruit.
    // Prefab để instantiate cho mỗi quả.
    public GameObject fruitPrefab;
    
    [Header("Spawn Points (Only when usePrePlacedFruits = false)")]
    [Tooltip("Create empty GameObjects as children of tree, drag here")]
    // Transform positions where fruits will be spawned.
    // Các vị trí Transform nơi quả sẽ được spawn.
    public Transform[] spawnPoints;
    
    // =========================================================================
    // GENERAL CONFIGURATION
    // CẤU HÌNH CHUNG
    // =========================================================================
    [Header("General Config / Cấu hình chung")]
    [Tooltip("Spawn/reset fruits when game starts?")]
    // If true, fruits appear immediately when game loads.
    // Nếu true, quả xuất hiện ngay khi game load.
    public bool spawnOnStart = true;
    
    [Tooltip("Respawn fruits when season changes?")]
    // If true, all fruits are restored when season transitions.
    // Nếu true, tất cả quả được khôi phục khi chuyển mùa.
    public bool respawnOnSeasonChange = true;
    
    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // List of managed fruit GameObjects.
    // Danh sách các GameObject quả được quản lý.
    private List<GameObject> _spawnedFruits = new List<GameObject>();
    
    // Stores original position/rotation for pre-placed fruits.
    // Used to reset fruits to their original positions after collection.
    // Lưu vị trí/rotation gốc cho quả đặt sẵn.
    // Dùng để reset quả về vị trí ban đầu sau khi thu hoạch.
    private Dictionary<GameObject, Pose> _initialTransforms = new Dictionary<GameObject, Pose>();

    // =========================================================================
    // Start - Initialize fruit management based on selected mode.
    // Start - Khởi tạo quản lý quả dựa trên chế độ đã chọn.
    // =========================================================================
    private void Start()
    {
        // Pre-placed mode: Find existing fruits and record their positions.
        // Chế độ đặt sẵn: Tìm các quả có sẵn và ghi lại vị trí của chúng.
        if (usePrePlacedFruits)
        {
            // Find all David_Fruit components including inactive ones.
            // Tìm tất cả component David_Fruit kể cả đang tắt.
            David_Fruit[] foundFruits = GetComponentsInChildren<David_Fruit>(true);
            foreach (var f in foundFruits)
            {
                _spawnedFruits.Add(f.gameObject);
                
                // Record initial position for later reset.
                // Ghi lại vị trí ban đầu để reset sau.
                _initialTransforms[f.gameObject] = new Pose(f.transform.position, f.transform.rotation);
                
                // IMPORTANT: Set destroyOnCollect = false so fruits can be respawned.
                // QUAN TRỌNG: Đặt destroyOnCollect = false để quả có thể respawn.
                f.destroyOnCollect = false; 
            }
            Debug.Log($"[David_TreeSpawner] Found {_spawnedFruits.Count} pre-placed fruits on tree {gameObject.name}");
        }

        if (spawnOnStart)
        {
            RespawnAllFruits();
        }
    }
    
    // =========================================================================
    // OnEnable - Subscribe to season change events.
    // OnEnable - Đăng ký lắng nghe sự kiện đổi mùa.
    // =========================================================================
    private void OnEnable()
    {
        RulesoftheGame_VU2_1.OnPhaseChanged += OnSeasonChanged;
    }
    
    // =========================================================================
    // OnDisable - Unsubscribe to prevent memory leaks.
    // OnDisable - Hủy đăng ký để tránh rò rỉ bộ nhớ.
    // =========================================================================
    private void OnDisable()
    {
        RulesoftheGame_VU2_1.OnPhaseChanged -= OnSeasonChanged;
    }
    
    // =========================================================================
    // OnSeasonChanged - Called when season transitions (Rainy ↔ Dry).
    // OnSeasonChanged - Được gọi khi chuyển mùa (Mưa ↔ Khô).
    // 
    // Respawns all fruits to simulate new season's harvest cycle.
    // Respawn tất cả quả để mô phỏng chu kỳ thu hoạch của mùa mới.
    // =========================================================================
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        if (!respawnOnSeasonChange) return;
        
        Debug.Log($"[David_TreeSpawner] Season changed to {newPhase}, respawning fruits...");
        RespawnAllFruits();
    }
    
    // =========================================================================
    // RespawnAllFruits - Restores all fruits to their original state.
    // RespawnAllFruits - Khôi phục tất cả quả về trạng thái ban đầu.
    // 
    // For pre-placed mode: Resets position, physics, and state.
    // For spawn mode: Destroys old fruits and creates new ones.
    // 
    // Cho chế độ đặt sẵn: Reset vị trí, vật lý, và trạng thái.
    // Cho chế độ spawn: Hủy quả cũ và tạo quả mới.
    // =========================================================================
    public void RespawnAllFruits()
    {
        // PRE-PLACED MODE: Reset existing fruits.
        // CHẾ ĐỘ ĐẶT SẴN: Reset các quả hiện có.
        if (usePrePlacedFruits)
        {
            foreach (var fruitObj in _spawnedFruits)
            {
                if (fruitObj == null) continue;

                // 1. Reset position and rotation to initial values.
                // 1. Reset vị trí và rotation về giá trị ban đầu.
                if (_initialTransforms.ContainsKey(fruitObj))
                {
                    Pose initPose = _initialTransforms[fruitObj];
                    fruitObj.transform.position = initPose.position;
                    fruitObj.transform.rotation = initPose.rotation;
                }

                // 2. Reset physics to prevent momentum from previous state.
                // 2. Reset vật lý để ngăn động lượng từ trạng thái trước.
                Rigidbody rb = fruitObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // 3. Reset David_Fruit state by toggling active state.
                // This triggers OnEnable which resets _collected flag.
                // 3. Reset trạng thái David_Fruit bằng cách toggle active.
                // Điều này kích hoạt OnEnable để reset cờ _collected.
                David_Fruit fruitScript = fruitObj.GetComponent<David_Fruit>();
                if (fruitScript != null)
                {
                    fruitObj.SetActive(false); 
                    fruitObj.SetActive(true);
                }
                else
                {
                    fruitObj.SetActive(true);
                }
            }
            return;
        }

        // SPAWN MODE: Instantiate new fruits from prefab.
        // CHẾ ĐỘ SPAWN: Instantiate quả mới từ prefab.
        if (fruitPrefab == null)
        {
            Debug.LogWarning("[David_TreeSpawner] fruitPrefab not assigned!");
            return;
        }
        
        // Clear any remaining old fruits.
        // Xóa các quả cũ còn sót lại.
        ClearAllFruits();
        
        // Spawn new fruit at each defined spawn point.
        // Spawn quả mới tại mỗi điểm spawn đã định.
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            
            var fruit = Instantiate(fruitPrefab, point.position, point.rotation);
            fruit.SetActive(true);
            _spawnedFruits.Add(fruit);
        }
        
        Debug.Log($"[David_TreeSpawner] Spawned {_spawnedFruits.Count} fruits on tree {gameObject.name}");
    }
    
    // =========================================================================
    // ClearAllFruits - Destroys all spawned fruits (spawn mode only).
    // ClearAllFruits - Hủy tất cả quả đã spawn (chỉ chế độ spawn).
    // =========================================================================
    public void ClearAllFruits()
    {
        // Only clear for spawn mode; pre-placed fruits are just hidden.
        // Chỉ xóa cho chế độ spawn; quả đặt sẵn chỉ bị ẩn.
        if (usePrePlacedFruits) return;

        foreach (var fruit in _spawnedFruits)
        {
            if (fruit != null)
            {
                Destroy(fruit);
            }
        }
        _spawnedFruits.Clear();
    }
    
    // =========================================================================
    // GetRemainingFruitCount - Returns count of active (uncollected) fruits.
    // GetRemainingFruitCount - Trả về số quả đang active (chưa thu hoạch).
    // =========================================================================
    public int GetRemainingFruitCount()
    {
        // Remove references to destroyed fruits.
        // Xóa tham chiếu đến quả đã bị hủy.
        _spawnedFruits.RemoveAll(f => f == null);
        
        // Count only active fruits.
        // Chỉ đếm quả đang active.
        int count = 0;
        foreach(var f in _spawnedFruits)
        {
            if (f.activeSelf) count++;
        }
        return count;
    }
    
    // =========================================================================
    // OnDrawGizmosSelected - Draws spawn points in Unity Editor.
    // OnDrawGizmosSelected - Vẽ các điểm spawn trong Unity Editor.
    // 
    // Yellow spheres show where fruits will be spawned.
    // Các hình cầu vàng hiển thị nơi quả sẽ được spawn.
    // =========================================================================
    private void OnDrawGizmosSelected()
    {
        // Only draw gizmos for spawn mode.
        // Chỉ vẽ gizmos cho chế độ spawn.
        if (spawnPoints == null || usePrePlacedFruits) return;
        
        Gizmos.color = Color.yellow;
        foreach (var point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.2f);
            }
        }
    }
}
