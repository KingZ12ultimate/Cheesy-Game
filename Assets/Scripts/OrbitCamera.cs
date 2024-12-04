using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private InputReader inputReader = default;

    [SerializeField, Tooltip("The transform of the camera's focus object.")]
    private Transform focus = default;

    [SerializeField, Range(1f, 20f), Tooltip("A fixed distance that the camera stays from the focus.")]
    private float distance = 5f;

    [SerializeField, Min(0f), Tooltip("The radius in which the camera won't respond to the object's movement.")]
    private float focusRadius = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("The rate at which the camera centers its focus.")]
    private float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f), Tooltip("Rotation speed of the camera around the object in degrees per second.")]
    private float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89f)]
    private float minVerticalAngle = 10f, maxVerticalAngle = 60f;

    [SerializeField, Min(0), Tooltip("The time the camera waits after the last manual rotation " +
        "before aligning itself automatically, measured in seconds.")]
    private float alignDelay = 5f;

    [SerializeField, Range(0f, 90f), Tooltip("The min delta angle between the object's heading and the camera's heading " +
        "from which the camera align itself automatically at full speed.")]
    private float alignSmoothRange = 45f;

    [SerializeField] private LayerMask obstructionMask;

    private Camera regularCamera;
    private Vector3 focusPoint, previousFocusPoint;
    private Vector2 orbitAngles = new Vector2(45f, 0f);
    private Vector2 lookInput = Vector2.zero;
    private float lastManualRotationTime;

    private Vector3 CameraHalfExtents
    {
        get
        {
            Vector3 halfExtents;
            halfExtents.y = regularCamera.nearClipPlane * 
                Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtents.x = halfExtents.y * regularCamera.aspect;
            halfExtents.z = 0f;
            return halfExtents;
        }
    }

    public static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? -angle : angle;
    }

    private void OnEnable()
    {
        inputReader.lookEvent += OnLook;
    }

    private void OnDisable()
    {
        inputReader.lookEvent -= OnLook;
    }

    private void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
            maxVerticalAngle = minVerticalAngle;
    }

    private void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }

    private void ConstrainAngles()
    {
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y < 0f)
            orbitAngles.y += 360f;
        else if (orbitAngles.y >= 360f)
            orbitAngles.y -= 360f;
    }

    private bool ManualRotation()
    {
        if (lookInput != Vector2.zero)
        {
            orbitAngles -= rotationSpeed * Time.unscaledDeltaTime * lookInput;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    private bool AutomaticRotation()
    {
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)
            return false;

        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z
        );

        if (movement.sqrMagnitude < 0.0001f)
            return false;

        float headingAngle = GetAngle(movement.normalized);
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movement.sqrMagnitude);
        if (deltaAbs < alignSmoothRange)
            rotationChange *= deltaAbs / alignSmoothRange;
        else if (180f - deltaAbs < alignSmoothRange)
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    private void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    private void LateUpdate()
    {
        UpdateFocusPoint();

        Quaternion lookRotation;
        // Checking done in that order to give priority for the player's input.
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }
        else
        {
            lookRotation = transform.localRotation;
        }

        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focus.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(castFrom, CameraHalfExtents, castDirection, out RaycastHit hit, 
            lookRotation, castDistance, ~obstructionMask))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }

        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    private void OnLook(Vector2 look)
    {
        // The orbit angles are stored in the format (Vertical, Horizontal) so the input vector should be swapped.
        lookInput.x = -look.y;
        lookInput.y = look.x;
    }
}
