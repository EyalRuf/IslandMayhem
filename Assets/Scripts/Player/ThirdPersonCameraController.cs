using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    public LocalPlayerInput lpInput;
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
    private Quaternion initialCameraRot;

    [Header("Aiming")]
    public bool isAiming;
    private bool isInAimTransition;
    public Vector3 cameraAimPos;
    public Quaternion cameraAimRot;
    public float cameraTransitionDuration;
    private float cameraTransitionTimer;
    private bool cameraBackToOrigin;
    private Vector3 cameraPosAtStartOfTransition;
    private Quaternion cameraRotAtStartOfTransition;
    public float aimFovAdjustment;
    private float initialFov;
    private float fovAtStartOfTransition;

    // Start is called before the first frame update
    void Start()
    {
        //TEMPORARILY DISABLED SORRY - Pelle
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        initialCameraPos = cameraTransform.localPosition;
        initialCameraRot = cameraTransform.localRotation;
        targetRotationOffset = targetTransform.localRotation.eulerAngles;
        initialFov = camera.fieldOfView;
        cameraBackToOrigin = true;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        mouseX += Input.GetAxis("Mouse X") * cameraRotationSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * cameraRotationSpeed;
        mouseY = Mathf.Clamp(mouseY, cameraYClampMin, cameraYClampMax);

        // If movign or aiming -> cameraBackToOrigin is only true when you're no longer aiming and camera transition ended
        if (lpInput.lookAroundInput && cameraBackToOrigin)
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
        cameraRotAtStartOfTransition = cameraTransform.localRotation;
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
            if (Vector3.Distance(cameraTransform.localPosition, cameraAimPos) > 0.01f)
            {
                currCameraLocalPos = Vector3.Lerp(cameraPosAtStartOfTransition, cameraAimPos, cameraTransitionTimer);
                cameraTransform.localRotation = Quaternion.Lerp(cameraRotAtStartOfTransition, cameraAimRot, cameraTransitionTimer);
                camera.fieldOfView = Mathf.Lerp(fovAtStartOfTransition, initialFov + aimFovAdjustment, cameraTransitionTimer);
            }
            else
            {
                currCameraLocalPos = cameraAimPos;
                cameraTransform.localRotation = cameraAimRot;
                camera.fieldOfView = initialFov + aimFovAdjustment;
                AimTransitionOver();
            }
        }
        else
        {
            if (Vector3.Distance(cameraTransform.localPosition, initialCameraPos) > 0.01f)
            {
                currCameraLocalPos = Vector3.Lerp(cameraPosAtStartOfTransition, initialCameraPos, cameraTransitionTimer);
                cameraTransform.localRotation = Quaternion.Lerp(cameraRotAtStartOfTransition, initialCameraRot, cameraTransitionTimer);
                camera.fieldOfView = Mathf.Lerp(fovAtStartOfTransition, initialFov, cameraTransitionTimer);
            }
            else
            {
                currCameraLocalPos = initialCameraPos;
                cameraTransform.localRotation = initialCameraRot;
                camera.fieldOfView = initialFov;
                AimTransitionOver();
            }
        }
    }

    void AimTransitionOver ()
    {
        isInAimTransition = false;
        cameraBackToOrigin = !isAiming;
    }
}
