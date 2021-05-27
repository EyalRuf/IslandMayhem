using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemPiece : MonoBehaviour
{
    public GameObject lightBeam;
    public TotemPieceGroup group = TotemPieceGroup.Any;
    public bool insertedToTotem = false;

    void Update()
    {
        lightBeam.SetActive(!insertedToTotem);
    }
}

public enum TotemPieceGroup
{
    None,
    Any,
    Red,
    Blue
}