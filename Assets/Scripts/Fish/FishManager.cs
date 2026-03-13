using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FishManager : MonoBehaviour
{
    public static FishManager Instance { get; private set; }

    [Header("Fish Prefabs")]
    public GameObject[] fishPrefabs;

    [Header("Pool")]
    public int defaultPoolSize = 20;
    public int maxPoolSize     = 50;

    [Header("Spawn")]
    public Transform spawnCenter;
    public float spawnRadius = 5f;
    public float waterY      = 0f;

    [Header("Jump Settings")]
    public float minLaunchSpeed = 3f;
    public float maxLaunchSpeed = 7f;

    [Header("Splash")]
    public ParticleSystem splashPrefab;

    [Header("Activity")]
    [Range(0f, 1f)] public float fishActivity = 1f;
    public float maxSpawnRate = 5f;
    public int spawnBurst = 5;

    ObjectPool<GameObject> _pool;
    ObjectPool<ParticleSystem> _splashPool;
    float _spawnTimer;
    readonly Dictionary<GameObject, FishJump> _jumpCache = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _pool = new ObjectPool<GameObject>(
            createFunc:      CreateFish,
            actionOnGet:     fish => fish.SetActive(true),
            actionOnRelease: fish => fish.SetActive(false),
            actionOnDestroy: fish => Destroy(fish),
            collectionCheck: false,
            defaultCapacity: defaultPoolSize,
            maxSize:         maxPoolSize
        );

        if (splashPrefab != null)
            _splashPool = new ObjectPool<ParticleSystem>(
                createFunc:      () => Instantiate(splashPrefab),
                actionOnGet:     ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); },
                actionOnDestroy: ps => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize:         30
            );

    
        var prewarm = new GameObject[defaultPoolSize];
        for (int i = 0; i < defaultPoolSize; i++) prewarm[i] = _pool.Get();
        for (int i = 0; i < defaultPoolSize; i++) _pool.Release(prewarm[i]);
    }

    void Update()
    {
        if (fishActivity <= 0f || spawnCenter == null) return;

        float spawnRate = fishActivity * maxSpawnRate;
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= 1f / spawnRate)
        {
            _spawnTimer = 0f;
            for (int i = 0; i < spawnBurst; i++)
                SpawnFish();
        }
    }

    GameObject CreateFish()
    {
        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];
        GameObject fish   = Instantiate(prefab, transform);

        if (!fish.TryGetComponent(out FishJump jump))
            jump = fish.AddComponent<FishJump>();
        jump.manager = this;
        jump.waterY  = waterY;
        _jumpCache[fish] = jump;

        fish.SetActive(false);
        return fish;
    }

    void SpawnFish()
    {
        GameObject fish = _pool.Get();

        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        fish.transform.SetPositionAndRotation(
            new Vector3(spawnCenter.position.x + circle.x, waterY, spawnCenter.position.z + circle.y),
            Random.rotation
        );

        _jumpCache[fish].Launch(Random.Range(minLaunchSpeed, maxLaunchSpeed));
    }

    public void ReturnToPool(GameObject fish)
    {
        _pool.Release(fish);
    }

    public void SpawnSplash(Vector3 position)
    {
        if (_splashPool == null) return;
        ParticleSystem ps = _splashPool.Get();
        ps.transform.position = position;
        ps.Play();
        StartCoroutine(ReturnSplash(ps));
    }

    System.Collections.IEnumerator ReturnSplash(ParticleSystem ps)
    {
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetime.constantMax);
        _splashPool.Release(ps);
    }
}
