using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Movement")]
    public float baseAcceleration = 40f;
    public float topSpeed = 22f;
    public float reverseSpeed = 10f;
    public float steerAngle = 110f;
    public float downforce = 80f;
    public float turnAssist = 0.15f;
    public AnimationCurve accelCurve = AnimationCurve.EaseInOut(0, 1f, 1, 0.3f);

    [Header("Drift")]
    public bool allowDrift = true;
    public float driftGripLoss = 0.45f;
    public float driftYawMultiplier = 1.6f;
    public float driftBuildRate = 1f;
    public float driftBoostForce = 14f;
    public float driftHopForce = 3f;
    public float driftHopDuration = 0.15f;

    [Header("Boost")]
    public float boostTime = 1.3f;
    public float boostMultiplier = 1.3f;

    [Header("Visuals")]
    public Transform kartBody;
    public float bankAmount = 20f;
    public float bodySmooth = 8f;

    private Rigidbody rb;
    private KartInputHandler input;
    private float steerInput;
    private float throttleInput;
    private bool driftInput;
    private bool isDrifting;
    private float driftDirection;
    private float driftCharge;
    private bool boosting;
    private Vector3 localVel;
    private bool isGrounded;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.4f, 0);
        input = GetComponent<KartInputHandler>();
    }

    private void FixedUpdate()
    {
        if (input)
        {
            var i = input.GetKartInput();
            steerInput = i.Horizontal;
            throttleInput = i.Vertical;
            driftInput = i.Drift;
        }

        // ground check
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.8f);
        if (!isGrounded) return;

        localVel = transform.InverseTransformDirection(rb.velocity);
        float speed = rb.velocity.magnitude;
        float accelFactor = accelCurve.Evaluate(speed / topSpeed);

        // apply forward acceleration
        if (throttleInput > 0 && speed < topSpeed)
            rb.AddForce(transform.forward * baseAcceleration * accelFactor, ForceMode.Acceleration);
        else if (throttleInput < 0 && speed < reverseSpeed)
            rb.AddForce(transform.forward * baseAcceleration * throttleInput, ForceMode.Acceleration);

        // drift logic
        HandleDrift();

        // steering
        float steerStrength = steerAngle * Mathf.Clamp01(speed / topSpeed);
        float steer = steerInput * steerStrength * Time.fixedDeltaTime;
        if (isDrifting)
            steer *= driftYawMultiplier;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steer, 0f));

        // lateral friction adjustment
        Vector3 fwdVel = transform.forward * Vector3.Dot(rb.velocity, transform.forward);
        Vector3 sideVel = transform.right * Vector3.Dot(rb.velocity, transform.right);
        float sideFactor = isDrifting ? driftGripLoss : 0.85f;
        rb.velocity = fwdVel + sideVel * sideFactor;

        // downforce (keeps kart “planted”)
        rb.AddForce(-transform.up * downforce);

        // auto straighten on low input
        if (Mathf.Abs(steerInput) < 0.2f)
            rb.angularVelocity *= (1f - turnAssist * Time.fixedDeltaTime);

        UpdateVisuals();
    }

    void HandleDrift()
    {
        if (!allowDrift) return;

        if (!isDrifting && driftInput && Mathf.Abs(steerInput) > 0.3f && throttleInput > 0)
        {
            isDrifting = true;
            driftDirection = Mathf.Sign(steerInput);
            StartCoroutine(DriftHop());
        }
        else if (!driftInput)
        {
            if (isDrifting)
            {
                if (driftCharge >= 1f)
                    StartCoroutine(DriftBoost());
                driftCharge = 0f;
            }
            isDrifting = false;
        }

        if (isDrifting)
            driftCharge += Time.fixedDeltaTime * driftBuildRate;
    }

    IEnumerator DriftHop()
    {
        float timer = 0f;
        while (timer < driftHopDuration)
        {
            rb.AddForce(Vector3.up * driftHopForce, ForceMode.Acceleration);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DriftBoost()
    {
        boosting = true;
        float timer = 0f;
        while (timer < boostTime)
        {
            rb.AddForce(transform.forward * baseAcceleration * boostMultiplier, ForceMode.Acceleration);
            timer += Time.deltaTime;
            yield return null;
        }
        boosting = false;
    }

    void UpdateVisuals()
    {
        if (!kartBody) return;
        float bank = -steerInput * bankAmount * (isDrifting ? 1.5f : 1f);
        Quaternion targetRot = Quaternion.Euler(0f, 0f, bank);
        kartBody.localRotation = Quaternion.Lerp(kartBody.localRotation, targetRot, Time.deltaTime * bodySmooth);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * 0.8f);
    }
}
