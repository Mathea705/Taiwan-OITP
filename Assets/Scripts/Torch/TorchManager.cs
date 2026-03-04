using UnityEngine;

public class TorchManager : MonoBehaviour
{

    // marks rod as dont destroy on load so it persists across scenes
    // 標記物件，使其在載入時不被銷毀，允許它在場景之間持續存在
    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

      // gets intensitymanager reference in start()
      // 在 start() 中獲取 intensitymanager 參考

    void Start()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        IntensityManager.Instance.Register(systems);
    

    }
}
