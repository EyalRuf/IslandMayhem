using CardboardCore.DI;
using UnityEngine;

[Injectable]
public class BootFailedUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void ShowError()
    {
        panel.SetActive(true);
    }
}
