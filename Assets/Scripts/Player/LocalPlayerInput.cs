using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerInput : MonoBehaviour
{
    public Vector3 moveInput = Vector3.zero;
    public bool jumpInput = false;
    public bool jumpInputDown = false;
    public bool pickupInputDown = false;
    public bool interactInputDown = false;
    public bool sprintInput = false;
    public bool throwInputDown = false;
    public bool aimInputDown = false;
    public bool aimInput = false;
    public bool aimInputUp = false;
    public bool lookAroundInput = false;

    // Update is called once per frame
    void Update()
    {
        float ver = Input.GetAxis("Vertical");
        float hor = Input.GetAxis("Horizontal");
        if (Mathf.Abs(ver) < 0.01f)
            ver = 0;
        if (Mathf.Abs(hor) < 0.01f)
            hor = 0;

        moveInput = new Vector3(hor, 0, ver);
        jumpInput = Input.GetKey(KeyCode.Space);
        jumpInputDown = Input.GetKeyDown(KeyCode.Space);
        pickupInputDown = Input.GetKeyDown(KeyCode.E);
        interactInputDown = Input.GetKeyDown(KeyCode.Return);
        sprintInput = Input.GetKey(KeyCode.LeftShift);
        throwInputDown = Input.GetKeyDown(KeyCode.Mouse0);
        aimInputDown = Input.GetKeyDown(KeyCode.Mouse1);
        aimInput = Input.GetKey(KeyCode.Mouse1);
        aimInputUp = Input.GetKeyUp(KeyCode.Mouse1);
        lookAroundInput = Input.GetKey(KeyCode.LeftAlt);
    }
}
