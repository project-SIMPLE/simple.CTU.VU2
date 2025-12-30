using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script gắn vào cây để quản lý spawn/respawn quả
/// Quả sẽ respawn khi đổi mùa (nhờ lắng nghe OnPhaseChanged)
/// Vị trí spawn được set cứng trong Editor thông qua spawnPoints
/// </summary>
public class David_TreeSpawner : MonoBehaviour
{
    [Header("Prefab quả")]
    [Tooltip("Kéo prefab quả (có David_Fruit) vào đây")]
    public GameObject fruitPrefab;
    
    [Header("Vị trí spawn (Set cứng trong Editor)")]
    [Tooltip("Tạo Empty GameObjects làm con của cây, kéo vào đây")]
    public Transform[] spawnPoints;
    
    [Header("Cấu hình")]
    [Tooltip("Có spawn quả khi game bắt đầu không?")]
    public bool spawnOnStart = true;
    
    [Tooltip("Có respawn khi đổi mùa không?")]
    public bool respawnOnSeasonChange = true;
    
    // Danh sách quả đã spawn để quản lý
    private readonly List<GameObject> _spawnedFruits = new List<GameObject>();
    
    private void Start()
    {
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
    /// Xóa tất cả quả cũ và spawn quả mới tại các vị trí đã định
    /// </summary>
    public void RespawnAllFruits()
    {
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
        // Loại bỏ các quả đã bị destroy
        _spawnedFruits.RemoveAll(f => f == null);
        return _spawnedFruits.Count;
    }
    
    /// <summary>
    /// Vẽ gizmos trong Editor để dễ nhìn vị trí spawn
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        
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
