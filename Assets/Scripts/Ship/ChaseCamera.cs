using UnityEngine;

/// <summary>
/// Floats behind and above the ship, following with smoothing so fast turns
/// feel dynamic rather than rigid. Attach to the Main Camera; set Target to the ship.
/// </summary>
public class ChaseCamera : MonoBehaviour
{
    [Tooltip("The ship to follow.")]
    public Transform target;

    [Header("Offset (ship's local space)")]
    [Tooltip("Behind (-Z) and above (+Y) the ship.")]
    public Vector3 offset = new Vector3(0f, 2.5f, -9f);

    [Header("Smoothing")]
    [Tooltip("Lower = snappier follow, higher = more lag.")]
    public float positionSmoothTime = 0.12f;
    [Tooltip("How fast the camera rotates to face the ship.")]
    public float rotationLerp = 6f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, positionSmoothTime);

        Vector3 lookPoint = target.position + target.forward * 10f;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position, target.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationLerp * Time.deltaTime);
    }
}
