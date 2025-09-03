using UnityEngine;

public class UIToggle : MonoBehaviour
{
    public GameObject uiMenu;   // gán UI Menu ở đây
    private bool isActive = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isActive = !isActive;
            uiMenu.SetActive(isActive);
        }
    }
}
