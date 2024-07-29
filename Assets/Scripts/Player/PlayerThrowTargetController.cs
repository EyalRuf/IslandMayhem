using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Player
{
    public class PlayerThrowTargetController : MonoBehaviour
    {
        public GameObject targetUI;
        public Image aimSR;
        public Sprite aim1;
        public Sprite aim2;
        public Sprite aim3;

        public bool isAutoAim;

        Coroutine targetingCoroutine;

        public void ShowTarget ()
        {
            targetUI.SetActive(true);
            targetingCoroutine = StartCoroutine(HeldTarget());
        }

        IEnumerator HeldTarget()
        {
            yield return new WaitForSeconds(0.75f);
            aimSR.sprite = aim2;
            yield return new WaitForSeconds(0.75f);
            aimSR.sprite = aim3;

            isAutoAim = true;
        }

        public void HideTarget()
        {
            StopCoroutine(targetingCoroutine);
            targetUI.SetActive(false);
            aimSR.sprite = aim1;
            isAutoAim = false;
        }
    }
}