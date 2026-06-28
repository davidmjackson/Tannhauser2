using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hybrid space flight: the ship carries linear momentum (Rigidbody) while
/// steering is applied directly for a controllable, assisted feel.
/// Mouse aims the nose, A/D roll, W/S throttle, Left Shift boosts, Left Ctrl brakes.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Thrust")]
    public float thrustPower = 30f;
    public float maxSpeed = 40f;

    [Header("Turning")]
    [Tooltip("Degrees turned per unit of mouse movement.")]
    public float mouseSensitivity = 0.15f;
    [Tooltip("Roll speed in degrees per second (A/D).")]
    public float rollRate = 90f;

    [Header("Flight Assist")]
    [Tooltip("How quickly drift bleeds off while coasting. Higher = stops sooner.")]
    public float linearAssist = 0.5f;
    [Tooltip("Extra damping while the brake (Left Ctrl) is held.")]
    public float brakeAssist = 4f;

    [Header("Boost")]
    [Tooltip("Speed/thrust multiplier while Left Shift is held.")]
    public float boostMultiplier = 2f;

    private Rigidbody rb;
    private float throttle;
    private float roll;
    private Vector2 look;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = linearAssist;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        Cursor.lockState = CursorLockMode.Locked; // free-look; press Esc in Editor to regain cursor
    }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        throttle = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        roll = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        // Accumulate every frame's mouse movement so none is lost or doubled
        // between physics steps. FixedUpdate consumes and resets it.
        look += mouse.delta.ReadValue();
    }

    void FixedUpdate()
    {
        var kb = Keyboard.current;
        bool boosting = kb != null && kb.leftShiftKey.isPressed;
        bool braking = kb != null && kb.leftCtrlKey.isPressed;

        // Flight assist
        rb.linearDamping = braking ? brakeAssist : linearAssist;

        // Steer directly (assisted feel): mouse Y = pitch, mouse X = yaw, A/D = roll
        float pitch = -look.y * mouseSensitivity;
        float yaw = look.x * mouseSensitivity;
        float bank = -roll * rollRate * Time.fixedDeltaTime;
        Quaternion delta = Quaternion.Euler(pitch, yaw, bank);
        rb.MoveRotation(rb.rotation * delta);
        look = Vector2.zero; // consumed this physics step

        // Thrust with momentum
        float power = thrustPower * (boosting ? boostMultiplier : 1f);
        rb.AddRelativeForce(Vector3.forward * (throttle * power), ForceMode.Acceleration);

        float cap = maxSpeed * (boosting ? boostMultiplier : 1f);
        if (rb.linearVelocity.magnitude > cap)
            rb.linearVelocity = rb.linearVelocity.normalized * cap;
    }
}
