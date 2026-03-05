using UnityEngine;

/// <summary>
/// TidalWaterController — Điều khiển mực nước SF_Water_Sea (1) theo đồng hồ thủy triều.
/// 
/// Quy tắc:
///   - MoonIcon ở vùng Marker 1 hoặc 3 → y = 0.1  (triều cường, nước dâng)
///   - MoonIcon ở vùng Marker 2 hoặc 4 → y = -0.4  (triều kém, nước rút)
///   - Chuyển đổi mượt bằng Lerp.
///
/// Cách dùng: Gắn lên object "SF_Water_Sea (1)" trong scene.
///            Tự động đọc từ MoonOrbitController.Instance.
/// </summary>
public class TidalWaterController : MonoBehaviour
{
    [Header("Mực nước (giá trị tuyệt đối, KHÔNG cộng thêm)")]
    [Tooltip("Y tuyệt đối khi triều cường (Marker 1 & 3).")]
    public float highTideY = 0.1f;

    [Tooltip("Y tuyệt đối khi triều kém (Marker 2 & 4).")]
    public float lowTideY = -0.4f;

    [Header("Chuyển đổi")]
    [Tooltip("Tốc độ Lerp chuyển mực nước (cao = nhanh).")]
    public float lerpSpeed = 1f;

    private float _targetY;

    void Start()
    {
        _targetY = transform.position.y;
    }

    void Update()
    {
        var moon = MoonOrbitController.Instance;
        if (moon == null) return;

        // Xác định mực nước mục tiêu theo marker zone
        int zone = moon.CurrentMarkerZone;
        if (zone == 1 || zone == 3)
            _targetY = highTideY;   // Triều cường
        else
            _targetY = lowTideY;    // Triều kém

        // Lerp mượt đến mục tiêu
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * lerpSpeed);
        transform.position = pos;
    }
}
