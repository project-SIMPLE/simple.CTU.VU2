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
    
    [Tooltip("Thời gian giữa mỗi lần đẻ trứng (giây)")]
    public float spawnInterval = 5f;
    
    [Tooltip("Offset vị trí spawn so với gà (Y = độ cao)")]
    public Vector3 spawnOffset = new Vector3(0f, 0.1f, 0f);
    
    [Header("Giới hạn")]
    [Tooltip("Số trứng tối đa có thể tồn tại (0 = không giới hạn)")]
    public int maxEggs = 10;
    
    private float timer;
    private int currentEggCount = 0;

    private void Start()
    {
        timer = spawnInterval;
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
            timer = spawnInterval;
        }
    }

    private void SpawnEgg()
    {
        // Kiểm tra giới hạn
        if (maxEggs > 0 && currentEggCount >= maxEggs)
        {
            return;
        }
        
        // Tính vị trí spawn
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        // Spawn trứng
        GameObject egg = Instantiate(eggPrefab, spawnPosition, Quaternion.identity);
        
        if (egg != null)
        {
            currentEggCount++;
            // Đăng ký event khi trứng bị hủy để giảm count
            egg.AddComponent<EggDestroyNotifier>().Initialize(this);
        }
    }

    /// <summary>
    /// Gọi khi trứng bị hủy để giảm số lượng
    /// </summary>
    public void OnEggDestroyed()
    {
        currentEggCount--;
        if (currentEggCount < 0) currentEggCount = 0;
    }
    
    /// <summary>
    /// Reset về trạng thái ban đầu
    /// </summary>
    public void ResetSpawner()
    {
        timer = spawnInterval;
        currentEggCount = 0;
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
            spawner.OnEggDestroyed();
        }
    }
}
