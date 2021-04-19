using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonCharacterController characterController;
    public Camera camera;
    public Transform targetTransform, playerTransform, cameraTransform;

    [Header("General")]
    public float cameraRotationSpeed;
    public float playerRotationSpeed;
    public float cameraYClampMin, cameraYClampMax;
    private Vector3 targetRotationOffset;
    private float mouseX, mouseY;
    private Vector3 currCameraLocalPos;

    [Header("Camera Collision")]
    public LayerMask cameraCollisionLayers;
    public float cameraColTransitionSpeed;
    private Vector3 initialCameraPos;

    [Header("Aiming")]
    private bool isAiming;
    public Vector3 cameraAimPos;
    public Quaternion cameraAimRot;
    public float cameraTransitionDuration;
    public float aimFovAdjustment;
    public float cameraYClampMinAim, cameraYClampMaxAim;
    private bool isInAimTransition;
    private float cameraTransitionTimer;
    private bool cameraBackToOrigin;
    private Vector3 cameraPosAtStartOfTransition;
    private float initialFov;
    private float fovAtStartOfTransition;

    // Start is called before the first frame update
    void Start()
    {
        //TEMPORARILY DISABLED SORRY - Pelle
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        initialCameraPos = cameraTransform.localPosition;
        targetRotationOffset = targetTransform.localRotation.eulerAngles;
        initialFov = camera.fieldOfView;
        cameraBackToOrigin = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        mouseX += Input.GetAxis("Mouse X") * cameraRotationSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * cameraRotationSpeed;
        mouseY = Mathf.Clamp(mouseY, isAiming ? cameraYClampMinAim : cameraYClampMin, isAiming ? cameraYClampMaxAim : cameraYClampMax);

        if (characterController.isLookingAround && cameraBackToOrigin)
        {
            targetTransform.rotation = Quaternion.Euler(mouseY + targetRotationOffset.x, mouseX, 0);
        } else
        {
            playerTransform.rotation = Quaternion.Lerp(playerTransform.rotation, Quaternion.Euler(0, mouseX, 0), playerRotationSpeed);
            targetTransform.rotation = Quaternion.Euler(mouseY + targetRotationOffset.x, mouseX + targetRotationOffset.y, 0 + targetRotationOffset.z);
        }

        if (isInAimTransition)
        {
            AimCamera();
        } else if (cameraBackToOrigin)
        {
            cameraTransform.LookAt(targetTransform);
        }

        RaycastHit hit;
        // Cast from player pos to player pos + all rotations that apply to camera object * it's original offset
        if (Physics.Linecast(playerTransform.position, playerTransform.position + (playerTransform.localRotation * targetTransform.localRotation * currCameraLocalPos), out hit, cameraCollisionLayers))
        {
            currCameraLocalPos = new Vector3(currCameraLocalPos.x, currCameraLocalPos.y, -Vector3.Distance(playerTransform.position, hit.point));
        } else if(!isAiming)
        {
            currCameraLocalPos = Vector3.Lerp(cameraTransform.localPosition, initialCameraPos, cameraColTransitionSpeed);
        }

        cameraTransform.localPosition = currCameraLocalPos;
    }

    public void ToggleCameraAim(bool isAiming)
    {
        this.isAiming = isAiming;
        isInAimTransition = true;
        cameraTransitionTimer = 0;
        cameraPosAtStartOfTransition = cameraTransform.localPosition;
        fovAtStartOfTransition = camera.fieldOfView;
    }

    void AimCamera ()
    {
        float transitionTime = 1 / cameraTransitionDuration;
        float transitionRate = Time.deltaTime * transitionTime;
        cameraTransitionTimer += transitionRate;

        if (cameraTransitionTimer > 1.0)
        {
            AimTransitionOver();
            return;
        }

        if (isAiming)
        {
            currCameraLocalPos = Vector3.Lerp(cameraPosAtStartOfTransition, cameraAimPos, cameraTransitionTimer);
            camera.fieldOfView = Mathf.Lerp(fovAtStartOfTransition, initialFov + aimFovAdjustment, cameraTransitionTimer);
        }
        else
        {
            currCameraLocalPos = Vector3.Lerp(cameraPosAtStartOfTransition, initialCameraPos, cameraTransitionTimer);
            camera.fieldOfView = Mathf.Lerp(fovAtStartOfTransition, initialFov, cameraTransitionTimer);
        }
    }

    void AimTransitionOver ()
    {
        currCameraLocalPos = isAiming ? cameraAimPos : initialCameraPos;
        camera.fieldOfView = isAiming ? initialFov + aimFovAdjustment : initialFov;

        isInAimTransition = false;
        cameraBackToOrigin = !isAiming;
    }
}
