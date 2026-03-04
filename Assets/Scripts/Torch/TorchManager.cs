using UnityEngine;

public class TorchManager : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        IntensityManager.Instance.Register(systems);
    }
}
