using UnityEngine;

public class TorchManager : MonoBehaviour
{
    // marks the torch as dont destroy on load so it persists across scenes
    // 標記物件，使其在載入時不被銷毀，允許它在場景之間持續存在
    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

    // registers all particle systems and the point light with the intensity manager
    // 向強度管理器註冊所有粒子系統和點光源
    void Start()
    {
        IntensityManager.Instance.Register(GetComponentsInChildren<ParticleSystem>(true));
        IntensityManager.Instance.RegisterLight(GetComponentInChildren<Light>(true));
    }
}
