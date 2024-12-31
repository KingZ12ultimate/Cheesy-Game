using System;
using System.Collections;
using System.Data;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [SerializeField] private InputReader inputReader =  default;
    [SerializeField] private Transform playerInputSpace = default;
    [SerializeField] private Transform model;
    [SerializeField] private Rigidbody bullet;

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

    [SerializeField, Tooltip("Which layers represent ground.")]
    private LayerMask groundMask = -1;

    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float attackDelay = 0.2f;
    [SerializeField] private float fieldOfShooting = 120f;
    [SerializeField] private float shootRadius = 10f;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private LayerMask shootMask = -1;

    [SerializeField] private float shockSpeed = 15f;
    [SerializeField] private float shockTimeFromDamage = 0.5f;
    [SerializeField] private float maxHealth = 100f;

    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField, Range(0f, 0.4f)] private float dashDuration = 0.2f;

    [Tooltip("Take off speed of the player's jump.")]
    private float jumpSpeed;

    [Tooltip("The gravity applied to the player while airborne.")]
    private float jumpGravity;

    private int groundContactCount, steepContactCount = 0;
    private int stepsSinceLastGrounded = 0, stepsSinceLastJump = 0;
    private bool OnGround => groundContactCount > 0;
    private bool OnSteep => steepContactCount > 0;
    private float jumpBufferTimer = 0f;
    private float attackDelayTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float minGroundDotProduct;
    private Rigidbody rb;
    private Vector2 moveInput = Vector2.zero;
    private Vector3 linearVelocity, desiredVelocity, movement, jumpDirection = Vector3.zero;
    private Vector3 contactNormal, steepNormal;

    private float health;
    private bool dashing = false;

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    #region Enable / Disable
    private void OnEnable()
    {
        inputReader.MoveEvent += OnMove;
        inputReader.JumpEvent += OnJump;
        inputReader.JumpCanceledEvent += OnJumpCanceled;
        inputReader.AttackEvent += OnAttack;
        inputReader.dashEvent += OnDash;
    }

    private void OnDisable()
    {
        inputReader.MoveEvent -= OnMove;
        inputReader.JumpEvent -= OnJump;
        inputReader.JumpCanceledEvent -= OnJumpCanceled;
        inputReader.AttackEvent -= OnAttack;
        inputReader.dashEvent -= OnDash;
    }
    #endregion

    #region Collisions
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == groundMask)
            EvaluateGroundCollisions(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateGroundCollisions(collision);
    }

    private void EvaluateGroundCollisions(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount++;
                contactNormal += normal;
                linearVelocity.y = 0f;
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
            probeDistance, groundMask)) return false;
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
    #endregion

    #region Update Related Methods
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

        if (playerInputSpace)
        {
            Vector3 forward = playerInputSpace.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = playerInputSpace.right;
            right.y = 0f;
            right.Normalize();
            movement = right * moveInput.x + forward * moveInput.y;
        }
        else
        {
            movement = Vector3.right * moveInput.x + Vector3.forward * moveInput.y;
        }
        desiredVelocity = movement * maxSpeed;
    }

    private void ClearState()
    {
        groundContactCount = steepContactCount = 0;
        contactNormal = steepNormal = Vector3.zero;
    }
    #endregion

    #region Velocity Updates
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

        if (OnGround)
            jumpDirection = Vector3.up;
        else if (OnSteep)
            jumpDirection = steepNormal;
        else return;

        jumpDirection = (jumpDirection + Vector3.up).normalized;
        float alignedSpeed = Vector3.Dot(linearVelocity, jumpDirection);
        float finalJumpSpeed = alignedSpeed > 0f ?
            Mathf.Max(jumpSpeed - alignedSpeed, 0f) : jumpSpeed;
        linearVelocity += jumpDirection * finalJumpSpeed;
    }
    #endregion

    #region Unity Messages
    private void Awake()
    {
        health = maxHealth;

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
        attackDelayTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;
        
        if (movement != Vector3.zero && !dashing)
        {
            Quaternion toRotation = Quaternion.LookRotation(movement);
            model.rotation = Quaternion.RotateTowards(model.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        UpdateState();
        
        if (!dashing)
            AdjustVelocity();

        if (jumpBufferTimer > 0f && (OnGround || OnSteep))
            Jump();

        rb.linearVelocity = linearVelocity;
        ClearState();
    }
    #endregion

    #region Callback Methods
    private void OnAttack()
    {
        if (attackDelayTimer > 0f) return;

        Vector3 shootDir;
        Collider[] hits = Physics.OverlapSphere(rb.position, shootRadius, shootMask);
        if (hits.Length == 0)
        {
            shootDir = model.forward;
        }
        else
        {
            Rigidbody closestEnemy = hits[0].attachedRigidbody;
            shootDir = (closestEnemy.position - rb.position).normalized;
            if (Vector3.Angle(model.forward, shootDir) > 0.5f * fieldOfShooting)
                shootDir = model.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(shootDir);
        Rigidbody bullet_rb = Instantiate(bullet, shootingPoint.position, rotation);
        bullet_rb.linearVelocity = shootDir * bulletSpeed;

        attackDelayTimer = attackDelay;
    }

    private void OnDash()
    {
        if (dashCooldownTimer > 0f || dashing) return;

        dashing = true;
        Vector3 dashVelocity = (movement != Vector3.zero ? movement : model.forward) * dashSpeed;
        rb.AddForce(dashVelocity, ForceMode.VelocityChange);
        StartCoroutine(EndDash());
    }

    private void OnJump()
    {
        jumpBufferTimer = jumpBuffer;
    }

    private void OnJumpCanceled()
    {
        float alignedSpeed = Vector3.Dot(linearVelocity, jumpDirection);
        if (!OnGround && alignedSpeed > 0f)
            linearVelocity -= 0.5f * jumpDirection;
    }

    private void OnMove(Vector2 move)
    {
        moveInput = move;
    }

    public void OnDamaged(Collision collision, float damage)
    {
        if (health > 0f) health -= damage;
        Vector3 normal = Vector3.up;
        for (int i = 0; i < collision.contactCount; i++)
        {
            normal -= collision.GetContact(i).normal;
        }
        normal.Normalize();
        rb.AddForce(normal * shockSpeed, ForceMode.VelocityChange);
        inputReader.gameInput.Player.Disable();
        StartCoroutine(EnableInputAfterShock());
    }

    private IEnumerator EnableInputAfterShock()
    {
        yield return new WaitForSeconds(shockTimeFromDamage);
        inputReader.gameInput.Player.Enable();
    }

    private IEnumerator EndDash()
    {
        yield return new WaitForSeconds(dashDuration);
        dashing = false;
        dashCooldownTimer = dashCooldown;
    }
    #endregion
}
