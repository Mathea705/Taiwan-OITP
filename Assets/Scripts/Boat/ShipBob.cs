using UnityEngine;
using WaveHarmonic.Crest;

public class ShipBob : MonoBehaviour
{
    public float bobSmoothSpeed  = 2f;
    public float waterlineOffset = 1f;

    public float tiltAmount      = 3f;
    public float tiltSmoothSpeed = 2f;
    public float sampleDistance  = 2f;
    public float bankAmount      = 0.5f;

    readonly SampleCollisionHelper _sampleCenter = new();
    readonly SampleCollisionHelper _sampleFront  = new();
    readonly SampleCollisionHelper _sampleBack   = new();

    float _lastYRotation;

    void Start()
    {
        _lastYRotation = transform.eulerAngles.y;
    }

    void FixedUpdate()
    {
        var pos = transform.position;

        _sampleCenter.SampleHeight(pos,  out float heightCenter);
        _sampleFront .SampleHeight(pos + new Vector3(0f, 0f,  sampleDistance),  out float heightFront);
        _sampleBack  .SampleHeight(pos + new Vector3(0f, 0f, -sampleDistance),  out float heightBack);


        var targetPos = new Vector3(pos.x, heightCenter + waterlineOffset, pos.z);
        transform.position = Vector3.Lerp(pos, targetPos, Time.fixedDeltaTime * bobSmoothSpeed);

 
        float turnRate   = Mathf.DeltaAngle(_lastYRotation, transform.eulerAngles.y) / Time.fixedDeltaTime;
        float pitchAngle = Mathf.Atan2(heightFront - heightBack, sampleDistance * 2f) * Mathf.Rad2Deg * tiltAmount;
        float rollAngle  = Mathf.Sin(Time.time * 0.7f + pos.z * 0.5f) * tiltAmount * 0.5f
                         - turnRate * bankAmount;

        var targetRot = Quaternion.Euler(pitchAngle, transform.eulerAngles.y, rollAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.fixedDeltaTime * tiltSmoothSpeed);

        _lastYRotation = transform.eulerAngles.y;
    }
}
