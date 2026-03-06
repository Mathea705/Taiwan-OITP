using UnityEngine;

public class IntensityManager : MonoBehaviour
{
    public static IntensityManager Instance { get; private set; }

    [Range(0f, 1f)] public float intensity = 1f;

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
}
