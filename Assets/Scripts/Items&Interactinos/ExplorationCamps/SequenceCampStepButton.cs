using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Mirror;

public class SequenceCampStepButton : CampStepButton
{
    [Header("Ref")]
    public SequenceCampStep sequenceCampStep;

    [Header("SequenceCampStepButton")]
    public int correctSequenceFigureIndex;
    [SyncVar]
    public int currSequenceFigureIndex;
    public Text sequenceFigureText;
    public float btnCD;

    public override void Update()
    {
        base.Update();

        sequenceFigureText.text = isEnabled ? sequenceCampStep.sequenceFigureOptions[currSequenceFigureIndex] + "" : "";
    }

    public override void PressButton()
    {
        base.PressButton();

        if (isEnabled)
        {
            currSequenceFigureIndex = (currSequenceFigureIndex + 1) % sequenceCampStep.sequenceFigureOptions.Count;
            StartCoroutine(BtnCD());
        }
    }

    IEnumerator BtnCD()
    {
        yield return new WaitForSeconds(btnCD);
        isCompleted = false;
    }
}
