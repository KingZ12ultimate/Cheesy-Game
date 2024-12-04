using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private InputReader inputReader =  default;
    [SerializeField] private Transform playerInputSpace = default;
    [SerializeField] private Transform model;

    [SerializeField, Tooltip("Max speed the player can reach.")]
    private float maxSpeed = 15.0f;

    [SerializeField, Tooltip("The rate at which the player's velocity changes.")]
    private float acceleration = 100.0f, airAcceleration = 25f;

    [SerializeField, Tooltip("The maximal height of the player's jump relative to its starting position.")]
    private float jumpHeight = 4f;

    [SerializeField, Tooltip("The amount of time that takes the player to reach the jump height.")]
    private float timeToApex = 0.3f;

    [SerializeField, Tooltip("The maximal time the player can request a jump before touching the ground.")]
    private float jumpBuffer = 0.2f;

    [SerializeField, Tooltip("Player's rotation speed in degrees per second.")]
    private float rotationSpeed = 90f;

    [SerializeField, Tooltip("Max angle of slope that is considered ground and not wall.")]
    private float maxGroundAngle = 40f;

    [SerializeField, Range(0f, 100f), Tooltip("Max speed at which the player still snaps to the ground when launching off a slope.")]
    private float maxSnapSpeed = 100f;

    [SerializeField, Min(0f), Tooltip("max distance for checking if snapping to ground is needed.")]
    private float probeDistance = 3f;

    [SerializeField, Tooltip("Which layers don't represent ground.")]
    private LayerMask probeMask = -1;

    [Tooltip("Take off speed of the player's jump.")]
    private float jumpSpeed;

    [Tooltip("The gravity applied to the player while airborne.")]
    private float jumpGravity;

    private int groundContactCount, steepContactCount = 0;
    private int stepsSinceLastGrounded = 0, stepsSinceLastJump = 0;
    private bool OnGround => groundContactCount > 0;
    private bool OnSteep => steepContactCount > 0;
    private float jumpBufferTimer = 0f;
    private float minGroundDotProduct;
    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 linearVelocity, desiredVelocity = Vector3.zero;
    private Vector3 contactNormal, steepNormal;

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    private void OnEnable()
    {
        inputReader.moveEvent += OnMove;
        inputReader.jumpEvent += OnJump;
    }

    private void OnDisable()
    {
        inputReader.moveEvent -= OnMove;
        inputReader.jumpEvent -= OnJump;
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollisions(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollisions(collision);
    }

    private void EvaluateCollisions(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount++;
                contactNormal += normal;
            }
            else if (normal.y > -0.01f)
            {
                steepContactCount++;
                steepNormal += normal;
            }
        }
    }

    private bool SnapToGround()
    {
        if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 3) return false;

        float speed = linearVelocity.magnitude;
        if (speed > maxSnapSpeed) return false;

        if (!Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, 
            probeDistance, ~probeMask)) return false;
        if (hit.normal.y < minGroundDotProduct) return false;

        groundContactCount = 1;
        contactNormal = hit.normal;
        float dot = Vector3.Dot(linearVelocity, hit.normal);
        if (dot > 0f)
            linearVelocity = (linearVelocity - hit.normal * dot).normalized * speed;
        return true;
    }

    private bool CheckSteepContact()
    {
        if (steepContactCount > 1)
        {
            steepNormal.Normalize();
            if (steepNormal.y >= minGroundDotProduct)
            {
                groundContactCount++;
                contactNormal = steepNormal;
                return true;
            }
        }
        return false;
    }

    private void UpdateState()
    {
        stepsSinceLastGrounded++;
        stepsSinceLastJump++;
        linearVelocity = rb.linearVelocity;
        if (!OnGround) linearVelocity.y -= jumpGravity * Time.fixedDeltaTime;

        if (OnGround || SnapToGround() || CheckSteepContact())
        {
            stepsSinceLastGrounded = 0;
            if (groundContactCount > 1)
                contactNormal.Normalize();
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    private void AdjustVelocity()
    {
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right);
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward);

        float currentX = Vector3.Dot(linearVelocity, xAxis);
        float currentZ = Vector3.Dot(linearVelocity, zAxis);

        float acceleration = OnGround ? this.acceleration : airAcceleration;
        Vector3 horizontalMovement = new Vector3(currentX, 0f, currentZ);
        Vector3 newMovement = Vector3.MoveTowards(horizontalMovement, desiredVelocity, acceleration * Time.fixedDeltaTime);
        linearVelocity += xAxis * (newMovement.x - horizontalMovement.x) +
            zAxis * (newMovement.z - horizontalMovement.z);
    }

    private void Jump()
    {
        jumpBufferTimer = 0f;
        stepsSinceLastJump = 0;

        Vector3 jumpDirection;
        if (OnGround)
            jumpDirection = contactNormal;
        else if (OnSteep)
            jumpDirection = steepNormal;
        else return;

        jumpDirection = (jumpDirection + Vector3.up).normalized;
        float alignedSpeed = Vector3.Dot(linearVelocity, jumpDirection);
        float finalJumpSpeed = alignedSpeed > 0f ?
            Mathf.Max(jumpSpeed - alignedSpeed, 0f) : jumpSpeed;
        linearVelocity += jumpDirection * finalJumpSpeed;
    }

    private void ClearState()
    {
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        OnValidate();
    }

    private void OnValidate()
    {
        jumpSpeed = 2 * jumpHeight / timeToApex;
        jumpGravity = jumpSpeed / timeToApex;

        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Update()
    {
        jumpBufferTimer -= Time.deltaTime;
        
        if (moveInput != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveInput);
            model.rotation = Quaternion.RotateTowards(model.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();

        if (jumpBufferTimer > 0f && (OnGround || OnSteep))
            Jump();

        rb.linearVelocity = linearVelocity;
        ClearState();
    }

    private void OnJump()
    {
        jumpBufferTimer = jumpBuffer;
    }

    private void OnJumpCanceled()
    {
        
    }

    private void OnMove(Vector2 move)
    {
        if (playerInputSpace)
        {
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            moveInput = right * move.x + forward * move.y;
            desiredVelocity = moveInput * maxSpeed;
        }
        else
        {
            moveInput = Vector3.right * move.x + Vector3.forward * move.y;
            desiredVelocity = moveInput * maxSpeed;
        }
    }
}
