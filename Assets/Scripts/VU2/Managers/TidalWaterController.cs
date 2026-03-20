using UnityEngine;

/// <summary>
/// TidalWaterController — Điều khiển mực nước theo mùa (Preparation / Defense).
/// 
/// Quy tắc:
///   - Mùa mưa (Preparation, 60s đầu) → nước dâng cao (highTideY)
///   - Mùa khô (Defense)               → nước rút xuống (lowTideY)
///   - Chuyển đổi mượt bằng Lerp.
///
/// Cách dùng: Gắn lên object nước trong scene.
///            Tự động lắng nghe LevelManager.OnWaveStepChanged.
/// </summary>
public class TidalWaterController : MonoBehaviour
{
    [Header("Mực nước (giá trị tuyệt đối)")]
    [Tooltip("Y tuyệt đối khi mùa mưa (Preparation - nước dâng cao).")]
    public float highTideY = 0.1f;

    [Tooltip("Y tuyệt đối khi mùa khô (Defense - nước rút).")]
    public float lowTideY = -0.4f;

    [Header("Chuyển đổi")]
    [Tooltip("Tốc độ Lerp chuyển mực nước (cao = nhanh).")]
    public float lerpSpeed = 1f;

    private float _targetY;

    void Start()
    {
        // Mùa mưa là giai đoạn đầu → bắt đầu ở mực nước cao
        _targetY = highTideY;
        LevelManager.OnWaveStepChanged += OnWaveStepChanged;
    }

    void OnDestroy()
    {
        LevelManager.OnWaveStepChanged -= OnWaveStepChanged;
    }

    void OnWaveStepChanged(WaveStep step)
    {
        if (step == WaveStep.Preparation)
            _targetY = highTideY;   // Mùa mưa → nước dâng
        else
            _targetY = lowTideY;    // Mùa khô → nước rút
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * lerpSpeed);
        transform.position = pos;
    }
}
