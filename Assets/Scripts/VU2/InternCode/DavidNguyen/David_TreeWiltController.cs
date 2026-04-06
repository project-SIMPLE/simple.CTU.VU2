using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script gắn vào CÂY (không phải quả) để điều khiển hiệu ứng héo khi mùa khô
/// Lắng nghe OnPhaseChanged từ RulesoftheGame_VU2_1
/// Mùa khô (Saltwater_Intrusion = 1) → Cây héo, đổi màu, thu nhỏ
/// Mùa mưa (Saltwater_Intrusion = 0) → Cây tươi trở lại
/// </summary>
public class David_TreeWiltController : MonoBehaviour
{
    [Header("Animator ")]
    [Tooltip("Kéo Animator của cây vào đây, hoặc để trống nếu dùng visual only")]
    public Animator treeAnimator;
    public string animGood = "Tree_Good";
    public string animBad = "Tree_Bad";
    public int animatorLayer = 0;
    
    [Header("Visual Effects (không cần Animator)")]
    [Tooltip("Bật nếu muốn đổi màu + scale thay vì dùng animation")]
    public bool useVisualEffects = true;
    
    [Tooltip("Màu khi cây héo")]
    public Color wiltColor = new Color(0.5f, 0.35f, 0.15f, 1f);  // Nâu héo
    
    [Tooltip("Tỷ lệ scale khi héo (0.8 = thu nhỏ 80%)")]
    [Range(0.5f, 1f)] 
    public float wiltScale = 0.85f;
    
    [Header("Shader Color Property")]
    [Tooltip("URP/HDRP: _BaseColor | Built-in: _Color")]
    public string colorProperty = "_BaseColor";
    public bool tryCommonColorProps = true;
    
    // Cache
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;
    private Vector3 _initialScale;
    private bool _isWilted = false;
    private Dictionary<Renderer, Color> _originalColors = new Dictionary<Renderer, Color>();
    
    private static readonly string[] _fallbackProps = { "_BaseColor", "_Color", "_Tint", "_TintColor" };
    
    private void Awake()
    {
        // Tìm animator nếu chưa gán
        if (treeAnimator == null)
            treeAnimator = GetComponentInChildren<Animator>();
        
        // Cache renderers
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb = new MaterialPropertyBlock();
        _initialScale = transform.localScale;
        
        // Lưu màu gốc
        CacheOriginalColors();
    }
    
    private void OnEnable()
    {
        // Đăng ký lắng nghe event đổi mùa
        GameRulesProvider.OnPhaseChanged += OnSeasonChanged;
        
        // Áp trạng thái hiện tại
        CheckCurrentSeason();
    }
    
    private void OnDisable()
    {
        GameRulesProvider.OnPhaseChanged -= OnSeasonChanged;
    }
    
    /// <summary>
    /// Kiểm tra mùa hiện tại khi script được enable
    /// </summary>
    private void CheckCurrentSeason()
    {
        bool isDry = GameRulesProvider.Saltwater_Intrusion >= 1f;
        
        if (isDry && !_isWilted)
            ApplyWilt();
        else if (!isDry && _isWilted)
            ClearWilt();
    }
    
    /// <summary>
    /// Được gọi khi mùa thay đổi
    /// </summary>
    private void OnSeasonChanged(SeasonPhase newPhase)
    {
        // Phase 3 (Rainy2 = T4) has high salinity → wilt tree.
        // Giai đoạn 3 (Rainy2 = T4) mặn cao → cây héo.
        bool isPhase3 = (newPhase == SeasonPhase.Rainy2);
        
        
        if (isPhase3 && !_isWilted)
        {
            ApplyWilt();
        }
        else if (!isPhase3 && _isWilted)
        {
            ClearWilt();
        }
    }
    
    /// <summary>
    /// Áp hiệu ứng héo cho cây
    /// </summary>
    public void ApplyWilt()
    {
        if (_isWilted) return;
        _isWilted = true;
        
        if (treeAnimator != null && !string.IsNullOrEmpty(animBad))
        {
            treeAnimator.Play(animBad, animatorLayer, 0f);
        }
        
        if (useVisualEffects)
        {
            ApplyWiltVisuals();
        }
    }
    
    /// <summary>
    /// Khôi phục cây về trạng thái tươi
    /// </summary>
    public void ClearWilt()
    {
        if (!_isWilted) return;
        _isWilted = false;
        
        if (treeAnimator != null && !string.IsNullOrEmpty(animGood))
        {
            treeAnimator.Play(animGood, animatorLayer, 0f);
        }
        if (useVisualEffects)
        {
            ClearWiltVisuals();
        }
    }
    
    /// <summary>
    /// Lưu màu gốc của tất cả renderers
    /// </summary>
    private void CacheOriginalColors()
    {
        _originalColors.Clear();
        
        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            
            string prop = GetColorProperty(r);
            if (string.IsNullOrEmpty(prop)) continue;
            
            Color original = r.sharedMaterial.GetColor(prop);
            _originalColors[r] = original;
        }
    }
    
    /// <summary>
    /// Tìm property màu phù hợp cho material
    /// </summary>
    private string GetColorProperty(Renderer r)
    {
        if (r.sharedMaterial.HasProperty(colorProperty))
            return colorProperty;
        
        if (tryCommonColorProps)
        {
            foreach (var prop in _fallbackProps)
            {
                if (r.sharedMaterial.HasProperty(prop))
                    return prop;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Áp màu héo + thu nhỏ
    /// </summary>
    private void ApplyWiltVisuals()
    {
        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;
            
            string prop = GetColorProperty(r);
            if (string.IsNullOrEmpty(prop)) continue;
            
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(prop, wiltColor);
            r.SetPropertyBlock(_mpb);
        }
        
        // Thu nhỏ cây
        transform.localScale = _initialScale * wiltScale;
    }
    
    /// <summary>
    /// Khôi phục màu gốc + scale gốc
    /// </summary>
    private void ClearWiltVisuals()
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            
            // Clear property block → về màu material gốc
            r.SetPropertyBlock(null);
        }
        
        // Khôi phục scale
        transform.localScale = _initialScale;
    }
    
    /// <summary>
    /// Kiểm tra cây có đang héo không
    /// </summary>
    public bool IsWilted => _isWilted;
    
    // Context menu để test trong Editor
    [ContextMenu("Test: Apply Wilt")]
    private void TestApplyWilt() => ApplyWilt();
    
    [ContextMenu("Test: Clear Wilt")]
    private void TestClearWilt() => ClearWilt();
}
