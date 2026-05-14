using Assets.Scripts.UI;
using CardboardCore.DI;
using UnityEngine;

// DDOL bridge between the state machine layer and the scene-local MenuUIManager.
// States inject this and call UI methods through it. MenuUIManager registers itself
// here on Awake each time the menu scene loads, keeping the reference always fresh.
[Injectable]
public class MenuManager : MonoBehaviour
{
    private MenuUIManager menuUI;

    public event System.Action UIReady;

    public void RegisterUI(MenuUIManager ui)
    {
        menuUI = ui;
        UIReady?.Invoke();
    }

    public void ClearUI() => menuUI = null;

    public void NotifyIfReady()
    {
        if (menuUI != null)
        {
            UIReady.Invoke();
        }
    }

    public void ShowMainScreen() => menuUI.ShowMainScreen();
    public void HideAll() => menuUI.HideAll();
    public void ShowLoadingScreen(string message = "Connecting...") => menuUI.ShowLoadingScreen(message);
}
