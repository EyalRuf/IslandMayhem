using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotemPiece : MonoBehaviour
{
    public TotemPieceGroup group = TotemPieceGroup.Any;
    public bool insertedToTotem = false;
}

public enum TotemPieceGroup
{
    None,
    Any,
    Red,
    Blue
}