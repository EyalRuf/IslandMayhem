using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine.UI;

public class SequenceCampStep : CampStep
{
    [Header("SequenceCampStep")]
    public int sequenceSize;
    [SyncVar]
    public List<int> sequenceFiguresIndexes;
    public List<int> sequenceFigureOptions = new List<int>() {
        1,2,3,4,5,6,7,8,9,0
    };
    public List<SequenceCampStepButton> sequenceBtns;

    public List<CampStep> stepsToRevealSequence;
    public List<Text> sequenceRevealers;

    public override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        NewSequence();
    }

    public override void Update()
    {
        base.Update();

        for (int i = 0; i < sequenceSize; i++)
        {
            sequenceRevealers[i].text = isEnabled ? sequenceFigureOptions[sequenceFiguresIndexes[i]] + "" : "";
            sequenceRevealers[i].gameObject.SetActive(isEnabled && stepsToRevealSequence.TrueForAll(step => step.isCompleted));
        }

        if (!isServer || !isEnabled)
            return;

        if (IsSequenceCorrect())
        {
            CompleteStep();
        }
    }

    bool IsSequenceCorrect()
    {
        for (int i = 0; i < sequenceSize; i++)
        {
            if (sequenceBtns[i].currSequenceFigureIndex != sequenceFiguresIndexes[i])
                return false;
        }

        return true;
    }

    public override void CompleteStep()
    {
        base.CompleteStep();

        sequenceBtns.ForEach(step => step.isEnabled = false);
        sequenceRevealers.ForEach(revealer => revealer.gameObject.SetActive(false));
    }

    public override void ResetStep()
    {
        base.ResetStep();

        NewSequence();
        sequenceBtns.ForEach(step => step.isEnabled = true);
    }

    void NewSequence ()
    {
        sequenceFiguresIndexes = new List<int>();
        for (int i = 0; i < sequenceSize; i++)
        {
            int correctFigureIndex = Random.Range(0, sequenceFigureOptions.Count);
            sequenceFiguresIndexes.Add(correctFigureIndex);
            sequenceBtns[i].correctSequenceFigureIndex = correctFigureIndex;

            int randomFigureIndex = Random.Range(0, sequenceFigureOptions.Count);
            sequenceBtns[i].currSequenceFigureIndex = randomFigureIndex;
        }
    }
}