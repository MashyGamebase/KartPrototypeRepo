using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 30f;
    public float turnSpeed = 100f;
    public float maxSpeed = 20f;
    public float drag = 1f;

    [Header("Boost Settings")]
    public float boostSpeed = 10f;
    public float boostTime = 1.5f;
    private bool isBoosting = false;

    [Header("Visual Feedback")]
    public Transform CameraTarget;
    public Transform vehicleBodyVisual;
    public Transform vehicleVisual;
    public float tiltAmount = 10f;
    public float bankAmount = 15f;
    public float tiltSmooth = 5f;
    private float bobFrequency = 2f;
    private float bobAmount = 0.02f;
    private Vector3 originalVisualPosition;

    [Header("Boost UI Elements")]
    public CanvasGroup boostUIGroup;
    public UnityEngine.UI.Image boostFillImage;
    public float uiFadeSpeed = 4f;

    [Header("Drift Settings")]
    public bool allowDrift = true;
    public float driftTurnMultiplier = 2f;
    public float driftBankMultiplier = 2f;
    public float driftSideFriction = 0.4f;

    private float driftTurnDirection = 0f;
    private float driftControlFactor = 0.5f;

    private bool isDrifting;
    private bool driftInput;

    [Header("Drift Effects")]
    public float driftBounceHeight = 0.3f;
    public float driftBounceDuration = 0.2f;
    private bool isDriftBouncing;
    private float driftBounceStartTime;

    private float driftTimer = 0f;
    [SerializeField] private float driftDurationRequired = 2.5f;
    private bool driftWasActiveLastFrame = false;

    [Header("Wheels")]
    public Transform frontLeftSteer, frontRightSteer;
    public Transform frontLeftModel, frontRightModel, rearLeftModel, rearRightModel;
    public float wheelRadius = 0.35f;

    private Rigidbody rb;
    private KartInputHandler inputHandler;
    private KartInput inputData;

    [Header("Particles")]
    public ParticleSystem[] driftParticlesR;
    public ParticleSystem[] driftParticlesL;
    public ParticleSystem[] boostParticlesR;
    public ParticleSystem[] boostParticlesL;

    [Header("Spinout Settings")]
    public float spinDuration = 1.2f;
    public float spinSpeed = 720f;
    private bool isSpinningOut = false;
    private float spinTimer = 0f;

    [Header("Item Use settings")]
    public bool allowUseItem = true;
    private KartItemContainer itemContainer;

    [Header("Grounding / Stability")]
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float airborneDrag = 0f;
    [SerializeField] private float groundedDragMultiplier = 1f;
    private bool isGrounded;
    private float steerSmoother;

    // Visual smoothing
    float _visualTurn;
    float _visualTurnVel;
    float _visualWheelAngle;
    float _visualWheelVel;

    const float VISUAL_TURN_SMOOTH = 0.06f;
    const float VISUAL_WHEEL_SMOOTH = 0.04f;
    const float YAW_TO_BANK = 0.08f;
    const float MAX_EXTRA_BANK = 10f;
    const float BASE_VISUAL_GAIN = 1.25f;
    const float VISUAL_SPEED_GAIN = 0.35f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        inputHandler = GetComponent<KartInputHandler>();
        itemContainer = GetComponent<KartItemContainer>();

        originalVisualPosition = vehicleVisual ? vehicleVisual.localPosition : Vector3.zero;

        var cam = FindObjectOfType<ManualCameraController>();
        if (cam)
        {
            cam.kartRigidbody = rb;
            cam.cameraTarget = CameraTarget;
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Read input from IKartInputInterface
        if (inputHandler != null)
            inputData = inputHandler.GetKartInput();

        // Ground check
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        // Apply drag
        rb.drag = isGrounded ? Mathf.Max(0f, drag * groundedDragMultiplier) : airborneDrag;

        if (isSpinningOut)
        {
            spinTimer -= dt;
            if (vehicleVisual) vehicleVisual.Rotate(Vector3.up, spinSpeed * dt);
            if (spinTimer <= 0f)
            {
                isSpinningOut = false;
                if (vehicleVisual) vehicleVisual.localRotation = Quaternion.identity;
            }
            return;
        }

        HandleMovement(dt);
        UseItem();

        if (isGrounded)
            rb.angularVelocity *= 0.98f;
    }

    void Update()
    {
        HandleVisuals();
        UpdateBoostUI();
    }

    void UseItem()
    {
        if (!allowUseItem || itemContainer == null) return;
        if (inputData.UseItem)
            itemContainer.UsePowerup(() => Boost());
    }

    void HandleMovement(float dt)
    {
        float horizontal = inputData.Horizontal;
        float vertical = inputData.Vertical;
        driftInput = inputData.Drift;

        float speedMag = rb.velocity.magnitude;

        // Drift detection
        if (isGrounded && !isDrifting && allowDrift && driftInput && Mathf.Abs(horizontal) > 0.1f && speedMag > 5f && vertical != 0)
        {
            isDrifting = true;
            StartDriftBounce();
            driftTurnDirection = Mathf.Sign(horizontal);
        }
        else if (!driftInput || !isGrounded || vertical <= 0f)
        {
            isDrifting = false;
        }

        // Acceleration logic
        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        float reverseSpeed = Vector3.Dot(rb.velocity, -transform.forward);

        if (vertical > 0 && forwardSpeed < maxSpeed)
        {
            float accelBoost = Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(forwardSpeed / maxSpeed));
            rb.AddForce(transform.forward * vertical * acceleration * accelBoost, ForceMode.Acceleration);
        }
        else if (vertical < 0 && reverseSpeed < 10f)
        {
            rb.AddForce(transform.forward * vertical * acceleration, ForceMode.Acceleration);
        }
        else if (Mathf.Approximately(vertical, 0f) && isGrounded)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
            float forwardZ = localVel.z;
            float effectiveDrag = isDrifting ? drag * 0.3f : drag;
            Vector3 engineBrakingForce = -transform.forward * forwardZ * effectiveDrag;
            rb.AddForce(engineBrakingForce, ForceMode.Acceleration);
        }

        // Turning
        float speedFactor = Mathf.Max(Mathf.InverseLerp(0, maxSpeed, speedMag), 0.3f);
        float baseTurnStrength = turnSpeed * speedFactor;
        float turnStrength = isDrifting ? baseTurnStrength * driftTurnMultiplier : baseTurnStrength;

        float desiredTurnInput = isDrifting
            ? Mathf.Lerp(driftTurnDirection, horizontal, driftControlFactor)
            : horizontal;

        steerSmoother = Mathf.Lerp(steerSmoother, desiredTurnInput, 1f - Mathf.Exp(-12f * dt));
        Quaternion turnOffset = Quaternion.Euler(0f, steerSmoother * turnStrength * dt, 0f);
        rb.MoveRotation(rb.rotation * turnOffset);

        if (isGrounded)
        {
            Vector3 v = rb.velocity;
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;

            Vector3 fwdVel = Vector3.Project(v, fwd);
            Vector3 sideVel = Vector3.Project(v, right);

            sideVel = Vector3.Lerp(sideVel, Vector3.zero, isDrifting ? (1f - driftSideFriction) : 0.9f);
            rb.velocity = fwdVel + sideVel;

            if (isDrifting)
            {
                Vector3 slip = right * driftTurnDirection * 2f;
                rb.AddForce(slip, ForceMode.Acceleration);
            }
        }
    }

    void HandleVisuals()
    {
        float speed = rb.velocity.magnitude;
        float rawTurn = inputData.Horizontal;

        _visualTurn = Mathf.SmoothDamp(_visualTurn, rawTurn, ref _visualTurnVel, VISUAL_TURN_SMOOTH);
        float yawRate = rb.angularVelocity.y * Mathf.Rad2Deg;
        float extraFromYaw = Mathf.Clamp(yawRate * YAW_TO_BANK, -MAX_EXTRA_BANK, MAX_EXTRA_BANK);
        float speedGain = 1f + VISUAL_SPEED_GAIN * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(speed / maxSpeed));

        float targetTilt = -Mathf.Clamp(inputData.Vertical, -1f, 1f) * tiltAmount;
        float baseBank = -_visualTurn * bankAmount * BASE_VISUAL_GAIN * speedGain;
        float targetBank = baseBank + extraFromYaw;
        if (isDrifting) targetBank *= driftBankMultiplier;

        float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.Clamp(speed, 0.5f, maxSpeed)) * bobAmount;

        Vector3 bounceOffset = Vector3.zero;
        if (isDriftBouncing)
        {
            float t = Mathf.Clamp01((Time.time - driftBounceStartTime) / driftBounceDuration);
            bounceOffset.y = Mathf.Sin(t * Mathf.PI) * driftBounceHeight;
            if (t >= 1f) isDriftBouncing = false;
        }
        if (vehicleVisual) vehicleVisual.localPosition = originalVisualPosition + bounceOffset;

        Quaternion bankRot = Quaternion.Euler(targetTilt + bob * 100f, 0f, targetBank);
        if (vehicleBodyVisual)
        {
            vehicleBodyVisual.localRotation = Quaternion.Lerp(
                vehicleBodyVisual.localRotation,
                bankRot,
                Time.deltaTime * tiltSmooth
            );
        }

        if (isDrifting) driftTimer += Time.deltaTime;
        else
        {
            if (driftWasActiveLastFrame && driftTimer >= driftDurationRequired) Boost();
            driftTimer = 0f;
        }
        driftWasActiveLastFrame = isDrifting;

        PlayParticles();
        RotateCarWheels();
    }

    void UpdateBoostUI()
    {
        if (boostUIGroup == null || boostFillImage == null) return;

        float targetAlpha = (isDrifting && driftTimer < driftDurationRequired) ? 1f : 0f;
        boostUIGroup.alpha = Mathf.Lerp(boostUIGroup.alpha, targetAlpha, Time.deltaTime * uiFadeSpeed);

        float fillValue = Mathf.Clamp01(driftTimer / driftDurationRequired);
        boostFillImage.fillAmount = fillValue;
    }

    void RotateCarWheels()
    {
        if (!rb) return;

        float direction = Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1 : -1;
        float rotationAmount = (rb.velocity.magnitude / (2 * Mathf.PI * wheelRadius)) * 360f * Time.deltaTime * direction;

        if (frontLeftModel) frontLeftModel.Rotate(Vector3.right, rotationAmount);
        if (frontRightModel) frontRightModel.Rotate(Vector3.right, rotationAmount);
        if (rearLeftModel) rearLeftModel.Rotate(Vector3.right, rotationAmount);
        if (rearRightModel) rearRightModel.Rotate(Vector3.right, rotationAmount);

        float targetSteerAngle = inputData.Horizontal * 30f;
        _visualWheelAngle = Mathf.SmoothDamp(_visualWheelAngle, targetSteerAngle, ref _visualWheelVel, VISUAL_WHEEL_SMOOTH);

        if (frontLeftSteer) frontLeftSteer.localRotation = Quaternion.Euler(0, 0, _visualWheelAngle);
        if (frontRightSteer) frontRightSteer.localRotation = Quaternion.Euler(0, 0, _visualWheelAngle);
    }

    public void Boost()
    {
        StopCoroutine(boostVehicleCO());
        StartCoroutine(boostVehicleCO());
    }

    public void TriggerSpinout()
    {
        if (isSpinningOut) return;

        isSpinningOut = true;
        spinTimer = spinDuration;
        rb.velocity *= 0.5f;
    }

    IEnumerator boostVehicleCO()
    {
        isBoosting = true;

        foreach (var ps in boostParticlesR) ps.Play();
        foreach (var ps in boostParticlesL) ps.Play();

        float timer = 0f;
        while (timer < boostTime)
        {
            rb.AddForce(transform.forward * acceleration * boostSpeed, ForceMode.Acceleration);
            timer += Time.deltaTime;
            yield return null;
        }

        foreach (var ps in boostParticlesR) ps.Stop();
        foreach (var ps in boostParticlesL) ps.Stop();

        isBoosting = false;
    }

    void PlayParticles()
    {
        if (isDrifting)
        {
            foreach (var ps in driftParticlesR) ps.Play();
            foreach (var ps in driftParticlesL) ps.Play();
        }
        else
        {
            foreach (var ps in driftParticlesR) ps.Stop();
            foreach (var ps in driftParticlesL) ps.Stop();
        }
    }

    void StartDriftBounce()
    {
        isDriftBouncing = true;
        driftBounceStartTime = Time.time;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        Gizmos.DrawLine(transform.position, (rayOrigin + Vector3.down) * groundCheckDistance);
    }
}
