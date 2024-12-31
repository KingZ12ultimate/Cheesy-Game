using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GlobalInfo globalInfo = default;

    private float lifetimeCounter;

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }

    private void Start()
    {
        lifetimeCounter = globalInfo.bulletLifeSpan;
    }

    private void Update()
    {
        if (lifetimeCounter > 0)
            lifetimeCounter -= Time.deltaTime;
        else
            Destroy(gameObject);
    }
}
