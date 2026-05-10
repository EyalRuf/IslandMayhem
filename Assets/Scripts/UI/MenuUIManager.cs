using Assets.Scripts.Networking;
using CardboardCore.DI;
using Steamworks;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    [Injectable]
    public class MenuUIManager : MonoBehaviour
    {
        public CustomNetworkManager networkManager;
        [SerializeField] private SteamLobby steamLobby;
        [SerializeField] private AppManager appManager;

        [Header("UI")]
        [SerializeField] private GameObject MenuParent;
        [SerializeField] private GameObject MainPage;
        [SerializeField] private GameObject LobbyPage;
        [SerializeField] private GameObject LoadPage;
        [SerializeField] private Text loadingTxt;

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                networkManager.ResetManager();
                steamLobby.LeaveLobby();
                ClickedBackToMain();
                //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void ClickedHost()
        {
            if (networkManager.isSteam)
            {
                steamLobby.HostLobby();
                MenuParent.gameObject.SetActive(false);
            }
            else
            {
                appManager.RequestHostLocal(networkManager);
                // UI hidden by state machine (Chunk 4)
            }
        }

        public void ClickedLobbies()
        {
            if (networkManager.isSteam) 
            {
                steamLobby.GetLobbies();

                MenuParent.gameObject.SetActive(true);

                MainPage.gameObject.SetActive(false);
                LoadPage.gameObject.SetActive(false);
                LobbyPage.gameObject.SetActive(true);
            }
            else
            {
                appManager.RequestJoinLocal(networkManager, "localhost");
                // UI hidden by state machine (Chunk 4)
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

        public void JoinedGame ()
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