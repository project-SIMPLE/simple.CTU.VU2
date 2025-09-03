using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;

public class HoverInfo : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public GameObject uiInfo;
    public float sideOffset = 0.7f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        uiInfo.SetActive(true);

        Vector3 offset = transform.right * sideOffset;
        uiInfo.transform.position = transform.position + offset;

        // đảm bảo nó nhìn về player (camera)
        Transform cam = Camera.main.transform;
        uiInfo.transform.LookAt(cam);
        uiInfo.transform.Rotate(0, 180f, 0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //uiInfo.SetActive(false);
    }
    public void Start()
    {
        uiInfo.SetActive(false);
    }
}
