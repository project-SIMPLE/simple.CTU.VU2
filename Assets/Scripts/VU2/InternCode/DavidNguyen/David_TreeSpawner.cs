using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script gắn vào cây để quản lý spawn/respawn quả
/// Quả sẽ respawn khi đổi mùa (nhờ lắng nghe OnPhaseChanged)
/// Hỗ trợ 2 chế độ:
/// 1. Spawn từ prefab tại các điểm định sẵn (Spawn Points)
/// 2. Quản lý quả có sẵn (Pre-placed) - chỉ bật/tắt và reset vị trí
/// </summary>
public class David_TreeSpawner : MonoBehaviour
{
    [Header("Chế độ")]
    [Tooltip("Nếu true: Sẽ tìm tất cả script David_Fruit con của cây này và quản lý chúng.")]
    public bool usePrePlacedFruits = true;

    [Header("Prefab quả (Chỉ dùng khi usePrePlacedFruits = false)")]
    [Tooltip("Kéo prefab quả (có David_Fruit) vào đây")]
    public GameObject fruitPrefab;
    
    [Header("Vị trí spawn (Chỉ dùng khi usePrePlacedFruits = false)")]
    [Tooltip("Tạo Empty GameObjects làm con của cây, kéo vào đây")]
    public Transform[] spawnPoints;
    
    [Header("Cấu hình chung")]
    [Tooltip("Có spawn/reset quả khi game bắt đầu không?")]
    public bool spawnOnStart = true;
    
    [Tooltip("Có respawn khi đổi mùa không?")]
    public bool respawnOnSeasonChange = true;
    
    // Danh sách quả đã spawn hoặc tìm thấy để quản lý
    private List<GameObject> _spawnedFruits = new List<GameObject>();
    
    // Lưu trữ vị trí/rotation ban đầu để reset (dùng cho Pre-placed)
    private Dictionary<GameObject, Pose> _initialTransforms = new Dictionary<GameObject, Pose>();

    private void Start()
    {
        // Nếu dùng chế độ Pre-placed, tìm các quả có sẵn
        if (usePrePlacedFruits)
        {
            David_Fruit[] foundFruits = GetComponentsInChildren<David_Fruit>(true); // true để lấy cả object đang tắt
            foreach (var f in foundFruits)
            {
                _spawnedFruits.Add(f.gameObject);
                
                // Lưu vị trí ban đầu
                _initialTransforms[f.gameObject] = new Pose(f.transform.position, f.transform.rotation);
                
                // Đảm bảo quả này ko tự hủy
                f.destroyOnCollect = false; 
            }
            Debug.Log($"[David_TreeSpawner] Đã tìm thấy {_spawnedFruits.Count} quả có sẵn trên cây {gameObject.name}");
        }

        if (spawnOnStart)
        {
            RespawnAllFruits();
        }
    }
    
    private void OnEnable()
    {
        // Đăng ký lắng nghe event đổi mùa
        RulesoftheGame_VU2_1.OnPhaseChanged += OnSeasonChanged;
    }
    
    private void OnDisable()
    {
        // Hủy đăng ký để tránh memory leak
        RulesoftheGame_VU2_1.OnPhaseChanged -= OnSeasonChanged;
    }
    
    /// <summary>
    /// Được gọi khi mùa thay đổi (Rainy1 <-> Dry)
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        if (!respawnOnSeasonChange) return;
        
        Debug.Log($"[David_TreeSpawner] Mùa đổi sang {newPhase}, respawn quả...");
        RespawnAllFruits();
    }
    
    /// <summary>
    /// Respawn hoặc Reset quả
    /// </summary>
    public void RespawnAllFruits()
    {
        if (usePrePlacedFruits)
        {
            foreach (var fruitObj in _spawnedFruits)
            {
                if (fruitObj == null) continue;

                // 1. Reset vị trí và rotation về ban đầu
                if (_initialTransforms.ContainsKey(fruitObj))
                {
                    Pose initPose = _initialTransforms[fruitObj];
                    fruitObj.transform.position = initPose.position;
                    fruitObj.transform.rotation = initPose.rotation;
                }

                // 2. Reset Physics (quan trọng để nó không bay lung tung do quán tính cũ)
                Rigidbody rb = fruitObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                
                // 3. Reset trạng thái script David_Fruit (như cờ _collected)
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

        if (fruitPrefab == null)
        {
            Debug.LogWarning("[David_TreeSpawner] Chưa gán fruitPrefab!");
            return;
        }
        
        // Xóa quả cũ còn sót lại
        ClearAllFruits();
        
        // Spawn quả mới tại các điểm đã định
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            
            var fruit = Instantiate(fruitPrefab, point.position, point.rotation);
            fruit.SetActive(true);
            _spawnedFruits.Add(fruit);
        }
        
        Debug.Log($"[David_TreeSpawner] Đã spawn {_spawnedFruits.Count} quả trên cây {gameObject.name}");
    }
    
    /// <summary>
    /// Xóa tất cả quả đã spawn
    /// </summary>
    public void ClearAllFruits()
    {
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
    
    /// <summary>
    /// Lấy số lượng quả còn lại trên cây
    /// </summary>
    public int GetRemainingFruitCount()
    {
        // Loại bỏ các quả đã bị destroy (nếu có)
        _spawnedFruits.RemoveAll(f => f == null);
        
        // Đếm số quả đang Active
        int count = 0;
        foreach(var f in _spawnedFruits)
        {
            if (f.activeSelf) count++;
        }
        return count;
    }
    
    /// <summary>
    /// Vẽ gizmos trong Editor để dễ nhìn vị trí spawn
    /// </summary>
    private void OnDrawGizmosSelected()
    {
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
