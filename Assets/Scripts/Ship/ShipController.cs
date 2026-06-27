using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hybrid space flight: the ship carries momentum (Rigidbody) while a
/// flight-assist damping keeps it controllable. This version handles
/// forward/back thrust, momentum, and braking. Steering is added next task.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Thrust")]
    [Tooltip("Forward/back acceleration.")]
    public float thrustPower = 30f;
    [Tooltip("Top speed under normal thrust.")]
    public float maxSpeed = 40f;

    [Header("Flight Assist")]
    [Tooltip("How quickly drift bleeds off while coasting. Higher = stops sooner.")]
    public float linearAssist = 0.5f;
    [Tooltip("Extra damping while the brake (Left Ctrl) is held.")]
    public float brakeAssist = 4f;

    private Rigidbody rb;
    private float throttle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = linearAssist;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        throttle = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
    }

    void FixedUpdate()
    {
        var kb = Keyboard.current;
        bool braking = kb != null && kb.leftCtrlKey.isPressed;
        rb.linearDamping = braking ? brakeAssist : linearAssist;

        rb.AddRelativeForce(Vector3.forward * (throttle * thrustPower), ForceMode.Acceleration);

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }
}
