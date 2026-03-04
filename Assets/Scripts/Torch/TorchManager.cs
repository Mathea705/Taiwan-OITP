using UnityEngine;

public class TorchManager : MonoBehaviour
{

    // marks rod as dont destroy on load so it persists across scenes
    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

      // gets intensitymanager reference in start()

    void Start()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        IntensityManager.Instance.Register(systems);

       

    }
}
