using UnityEngine;

// =============================================================================
// TreeBarrier - Cây trồng chặn nước mặn xâm nhập nội đồng.
// TreeBarrier - Planted tree that traps one saltwater enemy from moving inland.
//
// CÁCH HOẠT ĐỘNG:
// - Khi enemy (tag "Enemy") đi vào vùng trigger → cây "bắt" enemy đó.
// - Enemy bị dừng di chuyển, đứng tại chỗ cạnh cây.
// - Mỗi cây chỉ giữ được 1 con mặn (1:1).
// - Cây từ từ mất máu khi đang giữ enemy (bị nước mặn ăn mòn).
// - Khi cây chết → enemy được thả ra, tiếp tục di chuyển.
// - Khi enemy chết trước (bị Ally trung hòa) → cây được giải phóng, sẵn sàng bắt con khác.
//
// SETUP:
// 1. Gắn script này vào prefab cây trồng
// 2. Thêm SphereCollider (isTrigger=true) làm vùng bắt
// 3. Đặt tag cây là "Construction" hoặc layer phù hợp
// 4. Tích hợp vào BuildSystemManager như 1 ConstructionSO mới (index 3)
// =============================================================================
public class TreeBarrier : MonoBehaviour, IDamageable
{
    // =========================================================================
    // CONFIGURATION
    // CẤU HÌNH
    // =========================================================================

    [Header("Stats / Chỉ số")]
    [Tooltip("Máu cây khi mới trồng")]
    [SerializeField] private int maxHealth = 5;

    [Tooltip("Sát thương mỗi giây khi đang giữ enemy (bị nước mặn ăn mòn)")]
    [SerializeField] private float corrosionDamagePerSecond = 0.5f;

    [Header("Visual")]
    [SerializeField] private Animator animator;

    [Header("Trap Settings / Bẫy")]
    [Tooltip("Bán kính phát hiện enemy (SphereCollider trigger)")]
    [SerializeField] private float trapRadius = 3f;

    // =========================================================================
    // RUNTIME STATE
    // TRẠNG THÁI RUNTIME
    // =========================================================================
    private int currentHealth;
    private float corrosionAccumulator;  // Tích lũy damage liên tục → int
    private GameObject trappedEnemy;     // Enemy đang bị giữ (null = sẵn sàng)
    private EnemyController trappedController;

    // =========================================================================
    // PUBLIC PROPERTIES
    // =========================================================================
    public int Health => currentHealth;
    public bool IsTrapping => trappedEnemy != null;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    void Start()
    {
        currentHealth = maxHealth;

        // Tự tạo / cấu hình trigger collider
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
        }
        trigger.isTrigger = true;
        trigger.radius = trapRadius;
        trigger.center = Vector3.zero;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (IsDead()) return;

        // Cập nhật HUD
        if (GameUI.Instance != null)
            GameUI.Instance.UpdateConstructionPosition(gameObject);

        // Nếu đang giữ enemy → cây bị ăn mòn dần
        if (trappedEnemy != null)
        {
            // Kiểm tra enemy đã bị hủy hoặc chết
            if (trappedEnemy == null || !trappedEnemy.activeInHierarchy)
            {
                ReleaseEnemy();
                return;
            }

            var enemy = trappedEnemy.GetComponent<Enemy>();
            if (enemy != null && enemy.IsDead())
            {
                ReleaseEnemy();
                return;
            }

            // Ăn mòn cây
            corrosionAccumulator += corrosionDamagePerSecond * Time.deltaTime;
            if (corrosionAccumulator >= 1f)
            {
                int dmg = Mathf.FloorToInt(corrosionAccumulator);
                corrosionAccumulator -= dmg;
                TakeDamage(dmg);
            }
        }
    }

    // =========================================================================
    // TRAP LOGIC — Bắt enemy
    // =========================================================================

    void OnTriggerEnter(Collider other)
    {
        if (IsDead()) return;
        if (trappedEnemy != null) return; // Đã giữ 1 con rồi

        if (!other.CompareTag("Enemy")) return;

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null || enemy.IsDead()) return;

        var controller = other.GetComponent<EnemyController>();
        if (controller == null) return;

        // Bắt enemy
        TrapEnemy(other.gameObject, controller);
    }

    private void TrapEnemy(GameObject enemyObj, EnemyController controller)
    {
        trappedEnemy = enemyObj;
        trappedController = controller;

        // Dừng di chuyển enemy
        controller.SetTrapped(true);

        if (animator != null)
            animator.Play("Tree_Good");

        Debug.Log($"[TreeBarrier] {name} trapped {enemyObj.name}");
    }

    /// <summary>
    /// Thả enemy khi cây chết hoặc enemy bị tiêu diệt.
    /// Release enemy when tree dies or enemy is neutralized.
    /// </summary>
    private void ReleaseEnemy()
    {
        if (trappedController != null)
        {
            trappedController.SetTrapped(false);
        }
        trappedEnemy = null;
        trappedController = null;

        Debug.Log($"[TreeBarrier] {name} released enemy");
    }

    // =========================================================================
    // IDAMAGEABLE
    // =========================================================================

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (animator != null)
        {
            if (currentHealth <= maxHealth / 2 && currentHealth > 0)
                animator.Play("Tree_Bad");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        currentHealth = 0;

        // Thả enemy trước khi chết
        ReleaseEnemy();

        if (animator != null)
            animator.Play("Tree_Die");

        // Hoàn trả số lượng cây cho BuildSystem (index 3)
        var remover = GetComponent<ConstructionRemover>();
        if (remover != null && remover.buildSystemManager != null)
            remover.buildSystemManager.Constructions[3].IncreaseQuantity();

        // Xóa HUD
        if (GameUI.Instance != null)
            GameUI.Instance.DeletePlayer(gameObject);

        Debug.Log($"[TreeBarrier] {name} died");
        Destroy(gameObject, 2f);
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    // =========================================================================
    // GIZMOS
    // =========================================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, trapRadius);
    }
}
