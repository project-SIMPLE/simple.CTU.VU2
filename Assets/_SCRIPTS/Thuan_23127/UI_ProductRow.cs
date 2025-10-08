using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class để set giá trị của Total board
/// </summary>
public class UI_ProductRow : MonoBehaviour
{
    public Image icon;
    public Text rainyText;
    public Text normalText;
    public Text dryText;

    public void SetData(Sprite s, int r, int n, int d)
    {
        if (icon)
        {
            icon.sprite = s;
            icon.enabled = (s != null);
            icon.preserveAspect = true;
        }
        if (rainyText)  rainyText.text  = r.ToString();
        if (normalText) normalText.text = n.ToString();
        if (dryText)    dryText.text    = d.ToString();
    }
}