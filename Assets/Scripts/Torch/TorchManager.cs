using UnityEngine;

public class TorchManager : MonoBehaviour
{
    ParticleSystem[] _systems;
    float[] _baseSizes, _baseSpeeds, _baseEmissions;

    Light _light;
    float _baseLight;

    float _lastIntensity = -1f;

    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
        _baseSizes      = new float[_systems.Length];
        _baseSpeeds     = new float[_systems.Length];
        _baseEmissions  = new float[_systems.Length];

        for (int i = 0; i < _systems.Length; i++)
        {
            _baseSizes[i]     = _systems[i].main.startSizeMultiplier;
            _baseSpeeds[i]    = _systems[i].main.startSpeedMultiplier;
            _baseEmissions[i] = _systems[i].emission.rateOverTimeMultiplier;
        }

        _light     = GetComponentInChildren<Light>(true);
        _baseLight = _light.intensity;
    }

    void Update()
    {
        float intensity = IntensityManager.Instance.intensity;
        if (Mathf.Approximately(intensity, _lastIntensity)) return;

        for (int i = 0; i < _systems.Length; i++)
        {
            var main = _systems[i].main;
            main.startSizeMultiplier  = _baseSizes[i]     * intensity;
            main.startSpeedMultiplier = _baseSpeeds[i]    * intensity;

            var em = _systems[i].emission;
            em.rateOverTimeMultiplier = _baseEmissions[i] * intensity;
        }

        _light.intensity = _baseLight * intensity;
        _lastIntensity   = intensity;
    }
}
