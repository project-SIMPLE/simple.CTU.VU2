using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script đơn giản để con gà đẻ trứng.
/// Cứ sau mỗi n giây sẽ spawn 1 prefab tại vị trí hiện tại của con gà.
/// </summary>
public class Thuan_23127_ChickenEggSpawner : MonoBehaviour
{
    [Header("Cấu hình đẻ trứng")]
    [Tooltip("Prefab trứng sẽ được spawn")]
    public GameObject eggPrefab;
    
    [Tooltip("Thời gian tối thiểu giữa mỗi lần đẻ trứng (giây)")]
    public float minSpawnInterval = 10f;
    
    [Tooltip("Thời gian tối đa giữa mỗi lần đẻ trứng (giây)")]
    public float maxSpawnInterval = 10f;
    
    [Tooltip("Offset vị trí spawn so với gà (Y = độ cao)")]
    public Vector3 spawnOffset = new Vector3(0f, 0.1f, 0f);
    
    [Header("Giới hạn")]
    [Tooltip("Số trứng tối đa có thể tồn tại (0 = không giới hạn)")]
    public int maxEggs = 50;
    
    [Tooltip("Gán object con gà đang di chuyển vào đây. Nếu để trống sẽ dùng vị trí của script này.")]
    public Transform chickenTransform;
    
    private float timer;
    private int currentEggCount = 0;
    
    // Danh sách trứng đã đẻ để quản lý (xóa khi đổi mùa)
    private List<GameObject> spawnedEggs = new List<GameObject>();

    private void Start()
    {
        SetRandomTimer();
    }
    
    private void OnEnable()
    {
        // Đăng ký sự kiện đổi mùa
        RulesoftheGame_VU2_1.OnPhaseChanged += OnSeasonChanged;
    }

    private void OnDisable()
    {
        // Hủy đăng ký sự kiện
        RulesoftheGame_VU2_1.OnPhaseChanged -= OnSeasonChanged;
    }

    private void Update()
    {
        if (eggPrefab == null)
        {
            return;
        }
        
        timer -= Time.deltaTime;
        
        if (timer <= 0f)
        {
            SpawnEgg();
            SetRandomTimer();
        }
    }
    
    /// <summary>
    /// Xử lý khi mùa thay đổi
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        Debug.Log($"[ChickenEggSpawner] Mùa đổi sang {newPhase}. Reset trứng!");
        
        // 1. Xóa tất cả trứng cũ
        foreach (var egg in spawnedEggs)
        {
            if (egg != null)
            {
                Destroy(egg);
            }
        }
        spawnedEggs.Clear();
        currentEggCount = 0;
        
        // 2. Reset timer để bắt đầu đẻ lứa mới
        SetRandomTimer();
    }

    private void SpawnEgg()
    {
        // Kiểm tra giới hạn
        if (maxEggs > 0 && currentEggCount >= maxEggs)
        {
            return;
        }
        
        // Tính vị trí spawn
        // Nếu đã gán chickenTransform thì dùng vị trí của nó, ngược lại dùng vị trí của script này
        Vector3 basePosition = (chickenTransform != null) ? chickenTransform.position : transform.position;
        Vector3 spawnPosition = basePosition + spawnOffset;
        
        // Spawn trứng
        GameObject egg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
        
        if (egg != null)
        {
            currentEggCount++;
            spawnedEggs.Add(egg); // Thêm vào danh sách quản lý
            
            // Đăng ký event khi trứng bị hủy để giảm count
            egg.AddComponent<EggDestroyNotifier>().Initialize(this);
        }
    }

    /// <summary>
    /// Gọi khi trứng bị hủy để giảm số lượng và xóa khỏi list
    /// </summary>
    public void OnEggDestroyed(GameObject egg)
    {
        currentEggCount--;
        if (currentEggCount < 0) currentEggCount = 0;
        
        if (spawnedEggs.Contains(egg))
        {
            spawnedEggs.Remove(egg);
        }
    }
    
    /// <summary>
    /// Reset về trạng thái ban đầu
    /// </summary>
    public void ResetSpawner()
    {
        SetRandomTimer();
        currentEggCount = 0;
        spawnedEggs.Clear();
    }

    private void SetRandomTimer()
    {
        timer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}

/// <summary>
/// Helper component để thông báo khi trứng bị hủy
/// </summary>
public class EggDestroyNotifier : MonoBehaviour
{
    private Thuan_23127_ChickenEggSpawner spawner;
    
    public void Initialize(Thuan_23127_ChickenEggSpawner owner)
    {
        spawner = owner;
    }
    
    private void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnEggDestroyed(gameObject);
        }
    }
}
