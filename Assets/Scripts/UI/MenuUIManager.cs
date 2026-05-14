using Assets.Scripts.Networking;
using CardboardCore.DI;
using Steamworks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class MenuUIManager : MonoBehaviour
    {
        private MenuManager menuManager;

        // TODO: remove networkManager when isSteam flag is centralized to AppManager
        [SerializeField] private CustomNetworkManager networkManager;
        [SerializeField] private AppManager appManager;

        [Header("UI")]
        [SerializeField] private GameObject MenuParent;
        [SerializeField] private GameObject MainPage;
        [SerializeField] private GameObject LobbyPage;
        [SerializeField] private GameObject LoadPage;
        [SerializeField] private Text loadingTxt;

        private void Awake()
        {
            menuManager = FindAnyObjectByType<MenuManager>();
            menuManager.RegisterUI(this);
        }

        private void OnDestroy()
        {
            if (menuManager != null)
            {
                menuManager.ClearUI();
            }
            else if (Application.isPlaying) // Only log if we're in play mode, to avoid noisy logs during scene editing
            {
                Debug.LogError("MenuUIManager destroyed but MenuManager reference was null. This should not happen, investigate.");
            }
        }

        public void ClickedHost()
        {
            if (networkManager.isSteam)
            {
                appManager.RequestHostSteam();
            }
            else
            {
                appManager.RequestHostLocal();
            }
        }

        public void ClickedLobbies()
        {
            if (networkManager.isSteam)
            {
                appManager.RequestBrowseLobbies();

                // Panel switch kept here until BrowsingLobbiesState UI is fully wired
                MenuParent.gameObject.SetActive(true);
                MainPage.gameObject.SetActive(false);
                LoadPage.gameObject.SetActive(false);
                LobbyPage.gameObject.SetActive(true);
            }
            else
            {
                appManager.RequestJoinLocal("localhost");
            }
        }

        public void ClickedLobby()
        {
            loadingTxt.text = "Joining Lobby...";

            MenuParent.gameObject.SetActive(true);

            MainPage.gameObject.SetActive(false);
            LobbyPage.gameObject.SetActive(false);
            LoadPage.gameObject.SetActive(true);
        }

        public void ShowMainScreen()
        {
            MenuParent.SetActive(true);
            LobbyPage.SetActive(false);
            LoadPage.SetActive(false);
            MainPage.SetActive(true);
        }

        public void ShowLoadingScreen(string message = "Connecting...")
        {
            loadingTxt.text = message;
            MenuParent.SetActive(true);
            MainPage.SetActive(false);
            LobbyPage.SetActive(false);
            LoadPage.SetActive(true);
        }

        public void HideAll()
        {
            MenuParent.SetActive(false);
        }

        public void ClickedBackToMain() => ShowMainScreen();

        public void JoinedGame()
        {
            loadingTxt.text = "Loading...";

            MenuParent.gameObject.SetActive(false);

            LobbyPage.gameObject.SetActive(false);
            LoadPage.gameObject.SetActive(false);
            MainPage.gameObject.SetActive(true);
        }

        public void ClickedQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}
