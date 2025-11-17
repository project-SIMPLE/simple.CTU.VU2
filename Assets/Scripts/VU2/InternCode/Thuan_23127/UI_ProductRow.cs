using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class để set giá trị của Total board
/// </summary>
public class UI_ProductRow : MonoBehaviour
{
    public Image icon;
    public Text rainyText;
    public Text dryText;
    // public Text rainy2Text;

    public void SetData(Sprite s, int r, int d)
    {
        if (icon)
        {
            icon.sprite = s;
            icon.enabled = (s != null);
            icon.preserveAspect = true;
        }
        if (rainyText)  rainyText.text  = r.ToString();
        // if (rainy2Text) rainy2Text.text = n.ToString();
        if (dryText)    dryText.text    = d.ToString();
    }
}