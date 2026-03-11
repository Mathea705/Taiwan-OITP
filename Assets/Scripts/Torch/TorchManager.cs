using UnityEngine;
using UnityEngine.VFX;

public class TorchManager : MonoBehaviour
{
    VisualEffect[] _vfxs;
    Light _light;

    float[] _baseFireIntensity;
    float[] _baseFlameSize;
    float[] _baseGlowIntensity;
    float[] _baseEmbersRate;
    float _baseLight;

    float _lastIntensity = -1f;

    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        _vfxs  = GetComponentsInChildren<VisualEffect>(true);
        _light = GetComponentInChildren<Light>(true);

        _baseFireIntensity = new float[_vfxs.Length];
        _baseFlameSize     = new float[_vfxs.Length];
        _baseGlowIntensity = new float[_vfxs.Length];
        _baseEmbersRate    = new float[_vfxs.Length];

        for (int i = 0; i < _vfxs.Length; i++)
        {
            _baseFireIntensity[i] = _vfxs[i].GetFloat("Fire Intensity");
            _baseFlameSize[i]     = _vfxs[i].GetFloat("FlameSize");
            _baseGlowIntensity[i] = _vfxs[i].GetFloat("GlowIntensity");
            _baseEmbersRate[i]    = _vfxs[i].GetFloat("EmbersRate");
        }

        if (_light != null)
            _baseLight = _light.intensity;
    }

    void Update()
    {
        float intensity = IntensityManager.Instance.intensity;
        if (Mathf.Approximately(intensity, _lastIntensity)) return;

        float slowSize = Mathf.Pow(intensity, 0.5f);

        for (int i = 0; i < _vfxs.Length; i++)
        {
            _vfxs[i].SetFloat("Fire Intensity", _baseFireIntensity[i] * intensity);
            _vfxs[i].SetFloat("FlameSize",      _baseFlameSize[i]     * slowSize);
            _vfxs[i].SetFloat("GlowIntensity",  _baseGlowIntensity[i] * intensity);
            _vfxs[i].SetFloat("EmbersRate",     _baseEmbersRate[i]    * intensity);
        }

        if (_light != null)
            _light.intensity = _baseLight * (intensity * intensity * intensity * intensity * intensity);

        _lastIntensity = intensity;
    }
}
