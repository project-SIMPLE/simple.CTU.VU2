using UnityEngine;

public class WaterShaderController : MonoBehaviour
{
    [Header("Water Renderer")]
    [Tooltip("Renderer component of water object (auto-find if empty)")]
    public Renderer waterRenderer;
    
    [Header("Rainy Season Settings / Cài đặt mùa mưa")]
    [Tooltip("Speed during rainy season")]
    public float rainySpeed = -0.5f;
    
    [Tooltip("Salinity Contrast during rainy season")]
    public float rainySalinityContrast = 7f;
    
    [Header("Dry Season Settings / Cài đặt mùa khô")]
    [Tooltip("Speed during dry season")]
    public float drySpeed = 0.5f;
    
    [Tooltip("Salinity Contrast during dry season")]
    public float drySalinityContrast = 5f;
    
    [Header("Transition Settings / Cài đặt chuyển đổi")]
    [Tooltip("Duration of smooth transition between seasons (seconds)")]
    public float transitionDuration = 10f;
    private Material _waterMaterial;
    private Coroutine _transitionCoroutine;
    
    private void Start()
    {
        if (waterRenderer == null)
        {
            waterRenderer = GetComponent<Renderer>();
        }
        if (waterRenderer != null)
        {
            _waterMaterial = waterRenderer.material;
        }
        else
        {
            return;
        }
        LevelManager.OnWaveStepChanged += OnWaveStepChanged;
        // Start with rainy season settings (Preparation phase is first)
        ApplySeasonSettings(isRainy: true);
    }
    
    private void OnDestroy()
    {
        LevelManager.OnWaveStepChanged -= OnWaveStepChanged;
    }
    
    private void OnWaveStepChanged(WaveStep step)
    {
        if (_waterMaterial == null) return;
        ApplySeasonSettings(isRainy: step == WaveStep.Preparation);
    }

    private void ApplySeasonSettings(bool isRainy)
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }
        
        float currentSpeed = _waterMaterial.GetFloat("_Speed");
        float currentSalinityContrast = _waterMaterial.GetFloat("_Salinity_Contrast");

        float targetSpeed = isRainy ? rainySpeed : drySpeed;
        float targetSalinityContrast = isRainy ? rainySalinityContrast : drySalinityContrast;

        _transitionCoroutine = StartCoroutine(TransitionShaderValues(
            currentSpeed, targetSpeed,
            currentSalinityContrast, targetSalinityContrast
        ));
    }
    
    private System.Collections.IEnumerator TransitionShaderValues(
        float startSpeed, float endSpeed,
        float startSalinity, float endSalinity)
    {
        _waterMaterial.SetFloat("_Speed", endSpeed);
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            float currentSalinity = Mathf.Lerp(startSalinity, endSalinity, smoothT);
            
            _waterMaterial.SetFloat("_Salinity_Contrast", currentSalinity);
            
            yield return null;
        }
        
        _waterMaterial.SetFloat("_Salinity_Contrast", endSalinity);
        
        _transitionCoroutine = null;
    }
}
