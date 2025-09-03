using UnityEngine;
using UnityEngine.UI;

public class SaltSlider : MonoBehaviour
{
    public Slider saltSlider;
    public Text valueText;

    void Start()
    {
        if (saltSlider != null)
        {
            saltSlider.onValueChanged.AddListener(UpdateSalt);
            UpdateSalt(saltSlider.value);
        }
    }

    void UpdateSalt(float value)
    {
        GameVariables.currentSalt = value;  // Gán vào bi?n toàn c?c

        if (valueText != null)
            valueText.text = "Salt: " + value.ToString("F1");
    }
}
