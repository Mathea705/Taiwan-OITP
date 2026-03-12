using UnityEngine;
using UnityEngine.VFX;

public class TorchManager : MonoBehaviour
{
    VisualEffect[] _vfxs;
    Light _light;
    float _baseLight;

    float[] _baseFlameEmissive;
    float[] _baseFlameEmissiveIntensity;
    float[] _baseFlamePower;
    float[] _baseFlameSize;
    float[] _baseGlowIntensity;
    float[] _baseEmbersRate;
    float[] _baseSmokeDensity;

    float _lastIntensity = -1f;
    float _burstMultiplier = 1f;

    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        _vfxs  = GetComponentsInChildren<VisualEffect>(true);
        _light = GetComponentInChildren<Light>(true);
        if (_light != null) _baseLight = _light.intensity;

        _baseFlameEmissive          = new float[_vfxs.Length];
        _baseFlameEmissiveIntensity = new float[_vfxs.Length];
        _baseFlamePower             = new float[_vfxs.Length];
        _baseFlameSize              = new float[_vfxs.Length];
        _baseGlowIntensity          = new float[_vfxs.Length];
        _baseEmbersRate             = new float[_vfxs.Length];
        _baseSmokeDensity           = new float[_vfxs.Length];

        for (int i = 0; i < _vfxs.Length; i++)
        {
            _baseFlameEmissive[i]          = _vfxs[i].GetFloat("FlameEmissive");
            _baseFlameEmissiveIntensity[i] = _vfxs[i].GetFloat("FlameEmissive intensity");
            _baseFlamePower[i]             = _vfxs[i].GetFloat("Flame_Power");
            _baseFlameSize[i]              = _vfxs[i].GetFloat("FlameSize");
            _baseGlowIntensity[i]          = _vfxs[i].GetFloat("GlowIntensity");
            _baseEmbersRate[i]             = _vfxs[i].GetFloat("EmbersRate");
            _baseSmokeDensity[i]           = _vfxs[i].GetFloat("Smoke_Density");
        }
    }

    void Update()
    {
        float intensity = IntensityManager.Instance.intensity;
        if (Mathf.Approximately(intensity, _lastIntensity)) return;

        float slowSize = Mathf.Pow(intensity, 0.5f);

        for (int i = 0; i < _vfxs.Length; i++)
        {
            _vfxs[i].SetFloat("FlameEmissive",           _baseFlameEmissive[i]          * intensity);
            _vfxs[i].SetFloat("FlameEmissive intensity", _baseFlameEmissiveIntensity[i] * intensity);
            _vfxs[i].SetFloat("Flame_Power",             _baseFlamePower[i]             * intensity);
            _vfxs[i].SetFloat("FlameSize",               _baseFlameSize[i]              * slowSize);
            _vfxs[i].SetFloat("GlowIntensity",           _baseGlowIntensity[i]          * intensity);
            _vfxs[i].SetFloat("EmbersRate",              _baseEmbersRate[i]             * intensity);
            _vfxs[i].SetFloat("Smoke_Density",           _baseSmokeDensity[i]           * intensity);
        }

        if (_light != null)
        {
            if (intensity > _lastIntensity + 0.01f)
                _burstMultiplier = 10f;

            _burstMultiplier = Mathf.MoveTowards(_burstMultiplier, 1f, 6f * Time.deltaTime);
            _light.intensity = _baseLight * intensity * _burstMultiplier;
        }

        _lastIntensity = intensity;
    }
}
