using UnityEngine;

public class UIFollowPlayer : MonoBehaviour
{
    public Transform playerCamera;   // gán Main Camera hoặc XR Rig Camera
    public float distance = 2f;      // khoảng cách menu trước mặt người chơi
    public float heightOffset = -0.5f; // chỉnh cao/thấp so với tầm mắt
    public bool followRotation = true; // có quay theo người chơi không

    void Update()
    {
        if (playerCamera == null) return;

        // Đặt UI ở trước mặt player
        Vector3 forward = playerCamera.forward;
        forward.y = 0; // giữ cho UI chỉ xoay ngang, không ngửa lên xuống
        forward.Normalize();

        transform.position = playerCamera.position + forward * distance + Vector3.up * heightOffset;

        if (followRotation)
        {
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }
}
