using UnityEngine;

public class UIInfoFollowMenu : MonoBehaviour
{
    public Transform uiMenu;   // gán UI Menu
    // lệch sang trái (X âm = trái, dương = phải)
    public Vector3 offset = new Vector3(-0.6f, 0f, 0f); 

    void Update()
    {
        if (uiMenu == null) return;

        // Vị trí dựa trên UI Menu + offset
        transform.position = uiMenu.position + uiMenu.right * offset.x 
                                           + uiMenu.up * offset.y 
                                           + uiMenu.forward * offset.z;

        // Quay cùng hướng với UI Menu
        transform.rotation = uiMenu.rotation;
    }
}
