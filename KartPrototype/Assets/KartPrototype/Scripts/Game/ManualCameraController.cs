using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ManualCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform cameraTarget;
    public Rigidbody kartRigidbody;

    [Header("Follow Settings")]
    public Vector3 followOffset = new Vector3(0, 3, -6);
    public float followSmoothSpeed = 5f;

    [Header("Turn-Based Offset")]
    public float maxTurnOffsetX = 3f;       // How far to shift on extreme turn
    public float turnSensitivity = 1.5f;    // Multiplier for turn strength
    public float turnOffsetSmooth = 6f;

    [Header("FOV Settings")]
    public float baseFOV = 60f;
    public float maxFOV = 80f;
    public float fovSpeedMultiplier = 1f;

    private Camera cam;
    private float currentXOffset = 0f;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (kartRigidbody == null && cameraTarget != null)
            kartRigidbody = cameraTarget.GetComponentInParent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (cameraTarget == null || kartRigidbody == null)
            return;

        // === 1. Calculate Desired X Offset Toward Turn Direction ===
        float angularY = cameraTarget.InverseTransformDirection(kartRigidbody.angularVelocity).y;
        float targetXOffset = Mathf.Clamp(angularY * turnSensitivity, -1f, 1f) * maxTurnOffsetX;
        currentXOffset = Mathf.Lerp(currentXOffset, targetXOffset, Time.deltaTime * turnOffsetSmooth);

        // === 2. Apply Offset and Follow ===
        Vector3 totalOffset = followOffset + new Vector3(currentXOffset, 0f, 0f);
        Vector3 desiredPos = cameraTarget.TransformPoint(totalOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, 1f / followSmoothSpeed, Mathf.Infinity, Time.fixedDeltaTime);

        // === 3. Look At Target ===
        transform.LookAt(cameraTarget.position);

        // === 4. Dynamic FOV Based on Speed ===
        float speed = kartRigidbody.velocity.magnitude;
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speed * fovSpeedMultiplier / 50f);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }
}