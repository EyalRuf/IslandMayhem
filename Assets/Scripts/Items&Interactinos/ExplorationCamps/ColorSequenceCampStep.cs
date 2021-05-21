using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class ColorSequenceCampStep : SequenceCampStep
{
    public List<Material> sequenceMaterialOptions;
    public bool wereStepsComplete;
    public float timeSequenceRevealed;
    [SyncVar]
    public bool isSequenceBeingRevealed;

    private Coroutine currRevealSequenceCoroutine;

    // Use this for initialization
    public override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        if (isEnabled)
        {
            for (int i = 0; i < sequenceFiguresIndexes.Count; i++)
            {
                var colorBtn = sequenceBtns[i] as ColorSequenceCampStepButton;

                colorBtn.btnMR.material = isSequenceBeingRevealed ?
                    sequenceMaterialOptions[sequenceFiguresIndexes[i]] : sequenceMaterialOptions[colorBtn.currSequenceFigureIndex];
            }

            if (!isServer)
                return;

            bool areStepsComplete = stepsToRevealSequence.TrueForAll(step => step.isCompleted);
            if (!wereStepsComplete && areStepsComplete)
            {
                NewSequence();

                if (currRevealSequenceCoroutine != null)
                    StopCoroutine(currRevealSequenceCoroutine);

                currRevealSequenceCoroutine = StartCoroutine(RevealSequence());
            }

            wereStepsComplete = areStepsComplete;
        } 
    }

    IEnumerator RevealSequence()
    {
        isSequenceBeingRevealed = true;
        yield return new WaitForSeconds(timeSequenceRevealed);
        isSequenceBeingRevealed = false;

        if (!stepsToRevealSequence.TrueForAll(step => step.isCompleted))
        {
            stepsToRevealSequence.ForEach(step => step.ResetStep());
        }
    }

    public override void CompleteStep()
    {
        base.CompleteStep();
        wereStepsComplete = false;
    }
}
