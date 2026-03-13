using UnityEngine;

public class FishJump : MonoBehaviour
{
    [HideInInspector] public FishManager manager;
    [HideInInspector]  public  float waterY;

    private Rigidbody _rb;
    bool _hasPeaked;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        _hasPeaked = false;
    }

    void Update()
    {
        if (_rb.linearVelocity.y < 0f) _hasPeaked = true;

        if (_hasPeaked && transform.position.y <= waterY)
        {
            manager.SpawnSplash(new Vector3(transform.position.x, waterY, transform.position.z));
            manager.ReturnToPool(gameObject);
        }
    }

    public void Launch(float speed)
    {
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        Vector3 dir = new Vector3(
            Random.Range(-1.5f, 1.5f),
            1f,
            Random.Range(-1.5f, 1.5f)
        ).normalized;

        manager.SpawnSplash(new Vector3(transform.position.x, waterY, transform.position.z));
        _rb.AddForce(dir * speed, ForceMode.VelocityChange);
        _rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
    }
}
