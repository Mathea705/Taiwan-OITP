using System.Collections.Generic;
using UnityEngine;

public class IntensityManager : MonoBehaviour
{
    public static IntensityManager Instance { get; private set; }

    [SerializeField, Range(0f, 1f)] float intensity = 1f;

    readonly List<ParticleSystem[]> _registeredFires = new();
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
        _registeredFires.Add(systems);
        ApplyToSystems(systems, intensity);
    }

    void ApplyIntensity()
    {
        foreach (var systems in _registeredFires)
            ApplyToSystems(systems, intensity);
        _lastIntensity = intensity;
    }

    static void ApplyToSystems(ParticleSystem[] systems, float value)
    {
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.startSizeMultiplier = value;
            main.startSpeedMultiplier = value;

            var emission = ps.emission;
            emission.rateOverTimeMultiplier = value;
        }
    }
}
