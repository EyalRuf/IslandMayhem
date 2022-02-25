using Assets.Scripts.Networking;
using Steamworks;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class MenuUIManager : MonoBehaviour
    {
        public CustomNetworkManager networkManager;
        [SerializeField] private SteamLobby steamLobby;

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
                ClickedBackToMain();
                steamLobby.LeaveLobby();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void ClickedHost()
        {
            steamLobby.HostLobby();
            
            MenuParent.gameObject.SetActive(false);
        }

        public void ClickedLobbies()
        {
            steamLobby.GetLobbies();

            MenuParent.gameObject.SetActive(true);

            MainPage.gameObject.SetActive(false);
            LoadPage.gameObject.SetActive(false);
            LobbyPage.gameObject.SetActive(true);
        }

        public void ClickedLobby()
        {
            loadingTxt.text = "Joining Lobby...";

            MenuParent.gameObject.SetActive(true);

            MainPage.gameObject.SetActive(false);
            LobbyPage.gameObject.SetActive(false);
            LoadPage.gameObject.SetActive(true);
        }

        public void ClickedBackToMain()
        {
            MenuParent.gameObject.SetActive(true);

            LobbyPage.gameObject.SetActive(false);
            LoadPage.gameObject.SetActive(false);
            MainPage.gameObject.SetActive(true);
        }

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