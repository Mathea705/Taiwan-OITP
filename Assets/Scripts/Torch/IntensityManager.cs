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

    // destroy every other instance so only one instance exists as it is a singleton
    // 銷毀所有其他實例，以便只保留一個實例，因為它是單例
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

    // update intensity every frame
    // 每幀更新強度
    void Update()
    {
        if (!Mathf.Approximately(intensity, _lastIntensity))
            ApplyIntensity();
    }
   
   // register each particle system in the scene as the fire prefab has multiple gameobjects with particle systems
   // 註冊場景中的每個粒子系統，因為火焰預置體有多個帶有粒子系統的遊戲物件
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

 
   // change intensity
   // 改變強度
    void ApplyIntensity()
    {
        foreach (var data in _registeredFires)
            ApplyToSystems(data, intensity);
        _lastIntensity = intensity;
    }
    
    // apply the changed intensity value to every particle system
    // 將更改過的強度值應用到每個粒子系統
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
