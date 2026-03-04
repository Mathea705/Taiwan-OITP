using System.Collections.Generic;
using UnityEngine;

public class IntensityManager : MonoBehaviour
{
    public static IntensityManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] float intensity = 1f;

    struct FireSystemData
    {
        public ParticleSystem ps;
        public float baseSizeMultiplier;
        public float baseSpeedMultiplier;
        public float baseEmissionRate;
    }

    readonly List<FireSystemData[]> _registeredFires = new();
    float _lastIntensity = -1f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!Mathf.Approximately(intensity, _lastIntensity))
            ApplyIntensity();
    }

    public void Register(ParticleSystem[] systems)
    {
        var data = new FireSystemData[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            data[i] = new FireSystemData
            {
                ps = ps,
                baseSizeMultiplier = ps.main.startSizeMultiplier,
                baseSpeedMultiplier = ps.main.startSpeedMultiplier,
                baseEmissionRate = ps.emission.rateOverTimeMultiplier
            };
        }
        _registeredFires.Add(data);
        ApplyToSystems(data, intensity);
    }

    void ApplyIntensity()
    {
        foreach (var data in _registeredFires)
            ApplyToSystems(data, intensity);
        _lastIntensity = intensity;
    }

    static void ApplyToSystems(FireSystemData[] data, float value)
    {
        foreach (var d in data)
        {
            var main = d.ps.main;
            main.startSizeMultiplier = d.baseSizeMultiplier * value;
            main.startSpeedMultiplier = d.baseSpeedMultiplier * value;

            var emission = d.ps.emission;
            emission.rateOverTimeMultiplier = d.baseEmissionRate * value;
        }
    }
}
