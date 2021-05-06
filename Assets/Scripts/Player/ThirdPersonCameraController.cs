using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonCharacterController characterController;
    public Camera camera;
    public Transform targetTransform, playerTransform;

    [Header("General")]
    public float cameraRotationSpeed;
    public float playerRotationSpeed;
    public float cameraYClampMin, cameraYClampMax;
    private Vector3 targetRotationOffset;
    private float mouseX, mouseY;
    private Vector3 currCameraLocalPos;

    [Header("Camera Collision")]
    public LayerMask cameraCollisionLayers;
    private Vector3 initialCameraPos;
    private Vector3 dollyDir;
    public float minDistance = 5, maxDistance = 12;
    public float smooth = 10;
    public float collisionDistanceOffset = 2f;

    [Header("Aiming")]
    private bool isAiming;
    public Vector3 cameraAimPos;
    public Quaternion cameraAimRot;
    public float cameraTransitionDuration;
    public float aimFovAdjustment;
    public Transform cameraLooksAtThisWhileAiming;
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
        initialCameraPos = camera.transform.localPosition;
        targetRotationOffset = targetTransform.localRotation.eulerAngles;
        initialFov = camera.fieldOfView;
        cameraBackToOrigin = true;
        dollyDir = camera.transform.localPosition.normalized;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        mouseX += Input.GetAxis("Mouse X") * cameraRotationSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * cameraRotationSpeed;
        mouseY = Mathf.Clamp(mouseY, isAiming ? cameraYClampMinAim : cameraYClampMin, isAiming ? cameraYClampMaxAim : cameraYClampMax);
        currCameraLocalPos = camera.transform.localPosition;

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
            camera.transform.LookAt(targetTransform);
        }
        
        if (!isAiming)
        {
            Vector3 desiredCameraPos = targetTransform.TransformPoint(dollyDir * (maxDistance + collisionDistanceOffset));
            float distance = maxDistance;

            RaycastHit hit;
            if (Physics.Linecast(targetTransform.position, desiredCameraPos, out hit, cameraCollisionLayers))
            {
                distance = Mathf.Clamp(hit.distance - collisionDistanceOffset, minDistance, maxDistance);
            }
            currCameraLocalPos = Vector3.Lerp(camera.transform.localPosition, dollyDir * distance, Time.deltaTime * smooth);
        }

        camera.transform.localPosition = currCameraLocalPos;
        camera.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    public void ToggleCameraAim(bool isAiming)
    {
        this.isAiming = isAiming;
        isInAimTransition = true;
        cameraTransitionTimer = 0;
        cameraPosAtStartOfTransition = camera.transform.localPosition;
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
            camera.transform.LookAt(Vector3.Lerp(targetTransform.position, cameraLooksAtThisWhileAiming.position, cameraTransitionTimer));
        }
        else
        {
            currCameraLocalPos = Vector3.Lerp(cameraPosAtStartOfTransition, initialCameraPos, cameraTransitionTimer);
            camera.fieldOfView = Mathf.Lerp(fovAtStartOfTransition, initialFov, cameraTransitionTimer);
            camera.transform.LookAt(Vector3.Lerp(cameraLooksAtThisWhileAiming.position, targetTransform.position, cameraTransitionTimer));
        }
    }

    void AimTransitionOver ()
    {
        currCameraLocalPos = isAiming ? cameraAimPos : initialCameraPos;
        camera.fieldOfView = isAiming ? initialFov + aimFovAdjustment : initialFov;
        camera.transform.LookAt(isAiming ? cameraLooksAtThisWhileAiming.position : targetTransform.position);

        isInAimTransition = false;
        cameraBackToOrigin = !isAiming;
    }
}
