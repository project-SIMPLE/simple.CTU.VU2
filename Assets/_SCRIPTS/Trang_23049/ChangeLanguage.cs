using UnityEngine;
using UnityEngine.UI;  // ?? dùng Dropdown
using System;

public class ChangeLanguage : MonoBehaviour
{
    [Header("References")]
    public Dropdown languageDropdown;   // g?n Dropdown vào ?ây
    public LoaderData loader;           // tham chi?u t?i LoaderData

    void Start()
    {
        if (languageDropdown != null)
        {   
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    void OnLanguageChanged(int index)
    {
        if (loader == null) return;

        // Map option -> language string
        switch (index)
        {
            case 0:
                loader.SetLanguage("vi");  // Ti?ng Vi?t
                break;
            case 1:
                loader.SetLanguage("en");  // English
                break;
            case 2:
                loader.SetLanguage("jp");  // Japanese (d? phòng)
                break;
            case 3:
                loader.SetLanguage("kr");  // Korean (d? phòng)
                break;
            default:
                loader.SetLanguage("vi");
                break;
        }

        Debug.Log("Ngôn ng? hi?n t?i: " + loader.GetLanguage());
    }
}
