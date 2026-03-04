using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.2f;

    // added to point light to make the light flicker
    // 加到點光源上以使光線閃爍
    void Update()
    {
        GetComponent<Light>().intensity = Random.Range(minIntensity, maxIntensity);
    }
}
