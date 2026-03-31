using UnityEngine;

// =============================================================================
// TreeBarrier - Cây trồng chặn nước mặn xâm nhập nội đồng.
// TreeBarrier - Planted tree that traps one saltwater enemy from moving inland.
//
// CÁCH HOẠT ĐỘNG:
// - Khi enemy (tag "Enemy") đi vào vùng trigger → cây "bắt" enemy đó.
// - Enemy bị kéo về gốc cây, đứng tại chỗ.
// - Mỗi cây chỉ giữ được 1 con mặn (1:1).
// - Cây từ từ mất máu khi đang giữ enemy (bị nước mặn ăn mòn).
// - Màu cây thay đổi theo HP: xanh (full) → vàng (nửa) → nâu (sắp chết).
// - Khi cây chết → enemy được thả ra, tiếp tục di chuyển.
// - Khi enemy chết trước → cây được giải phóng, sẵn sàng bắt con khác.
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
    // =========================================================================

    [Header("Stats / Chỉ số")]
    [Tooltip("Máu cây khi mới trồng")]
    [SerializeField] private int maxHealth = 5;

    [Tooltip("Sát thương mỗi giây khi đang giữ enemy (bị nước mặn ăn mòn)")]
    [SerializeField] private float corrosionDamagePerSecond = 0.5f;

    [Header("Visual")]
    [SerializeField] private Animator animator;

    [Header("Đổi màu theo HP")]
    [Tooltip("Màu cây khi đầy máu")]
    [SerializeField] private Color healthyColor = Color.white;
    [Tooltip("Màu cây khi hết máu")]
    [SerializeField] private Color deadColor = new Color(0.4f, 0.25f, 0.1f, 1f);
    [Tooltip("Scale nhỏ nhất khi sắp chết (% so với gốc)")]
    [Range(0.5f, 1f)]
    [SerializeField] private float minScale = 0.8f;

    [Header("Trap Settings / Bẫy")]
    [Tooltip("Bán kính phát hiện enemy (SphereCollider trigger)")]
    [SerializeField] private float trapRadius = 3f;
    [Tooltip("Tốc độ kéo enemy về gốc cây (units/s)")]
    [SerializeField] private float enemyPullSpeed = 3f;

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================
    private int currentHealth;
    private float corrosionAccumulator;
    private GameObject trappedEnemy;
    private EnemyController trappedController;

    // Visual state
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Vector3 _initialScale;
    private static readonly string[] _colorProps = { "_BaseColor", "_Color", "_Tint", "_TintColor" };

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
            trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = trapRadius;
        trigger.center = Vector3.zero;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Visual setup
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
        _initialScale = transform.localScale;

        // Màu ban đầu (full HP)
        UpdateVisuals();
    }

    void Update()
    {
        if (IsDead()) return;

        // Cập nhật HUD
        if (GameUI.Instance != null)
            GameUI.Instance.UpdateConstructionPosition(gameObject);

        // Nếu đang giữ enemy → kéo về gốc cây + ăn mòn
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

            // Kéo enemy về gốc cây cho rõ ràng
            Vector3 pullTarget = transform.position;
            pullTarget.y = trappedEnemy.transform.position.y; // giữ nguyên Y
            trappedEnemy.transform.position = Vector3.MoveTowards(
                trappedEnemy.transform.position, pullTarget, enemyPullSpeed * Time.deltaTime);

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
        if (currentHealth < 0) currentHealth = 0;

        UpdateVisuals();

        if (currentHealth <= 0)
            Die();
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
    // VISUAL — Đổi màu + scale theo HP
    // =========================================================================

    private void UpdateVisuals()
    {
        float hpRatio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;

        // Lerp màu: healthyColor (full HP) → deadColor (0 HP)
        Color currentColor = Color.Lerp(deadColor, healthyColor, hpRatio);

        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            string prop = GetColorProp(r);
            if (prop == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, currentColor);
            r.SetPropertyBlock(_mpb);
        }

        // Scale: _initialScale (full HP) → _initialScale * minScale (0 HP)
        float scaleFactor = Mathf.Lerp(minScale, 1f, hpRatio);
        transform.localScale = _initialScale * scaleFactor;

        // Animation theo ngưỡng HP
        if (animator != null)
        {
            if (hpRatio > 0.5f)
                animator.Play("Tree_Good");
            else if (hpRatio > 0f)
                animator.Play("Tree_Bad");
        }
    }

    private string GetColorProp(Renderer r)
    {
        foreach (var prop in _colorProps)
            if (r.sharedMaterial.HasProperty(prop)) return prop;
        return null;
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
