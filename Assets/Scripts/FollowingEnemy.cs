using UnityEngine;

[RequireComponent (typeof(Renderer))]
public class FollowingEnemy : MonoBehaviour
{
    [SerializeField] private GlobalInfo globalInfo = default;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float followRadius = 5f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float delayAfterAttack = 1f;
    [SerializeField] private float damage = 20f;

    private Rigidbody rb;
    private Rigidbody target;
    private Renderer m_renderer;
    private Vector3 linearVelocity, desiredVelocity = Vector3.zero;
    private bool targetReached = false;
    private float health;
    private float delayAfterAttackTimer = 0f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody == target)
        {
            targetReached = true;
            delayAfterAttackTimer = delayAfterAttack;
            Player player = target.GetComponent<Player>();
            player.OnDamaged(collision, damage);
        }
        else if (collision.gameObject.GetComponent<Bullet>())
        {
            OnDamaged();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody == target)
            targetReached = false;
    }

    private void AdjustVelocity()
    {
        Vector3 horizontalVelocity = linearVelocity;
        horizontalVelocity.y = 0f;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
        linearVelocity.x = horizontalVelocity.x;
        linearVelocity.z = horizontalVelocity.z;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        target = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        m_renderer = GetComponent<Renderer>();
        m_renderer.material.color = Color.yellow;
        health = maxHealth;
    }

    private void Update()
    {
        delayAfterAttackTimer -= Time.deltaTime;
        if (health <= 0f)
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (delayAfterAttackTimer > 0f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        linearVelocity = rb.linearVelocity;

        Vector3 targetPos = target.position;
        targetPos.y = 0f;
        Vector3 rbPos = rb.position;
        rbPos.y = 0f;
        Vector3 delta = targetPos - rbPos;
        float distance = delta.magnitude;

        if (distance > followRadius || targetReached)
        {
            desiredVelocity = Vector3.zero;
        }
        else
        {
            desiredVelocity = (delta / distance) * maxSpeed;
        }
        AdjustVelocity();

        rb.linearVelocity = linearVelocity;
    }

    private void OnDamaged()
    {
        health -= globalInfo.bulletDamage;
        float t = health / maxHealth;
        m_renderer.material.color = t * Color.yellow + (1 - t) * Color.red;
    }
}
