using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnMouseHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image img;
    public Color baseColor;
    public Color hoverColor;

    public void OnPointerEnter(PointerEventData eventData)
    {
        img.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        img.color = baseColor;
    }
}
