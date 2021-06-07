using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DeathFade : MonoBehaviour
{
    public Image img;

    // Use this for initialization
    void Start()
    {
        img.canvasRenderer.SetAlpha(0);
        img.enabled = true;
    }

    public void FadeOut(float duration)
    {
        img.CrossFadeAlpha(1, duration, false);
    }

    public void FadeIn(float duration)
    {
        img.CrossFadeAlpha(0, duration, false);
    }
}
