using UnityEngine;

[CreateAssetMenu(fileName = "BounceStateResource", menuName = "Scriptable Objects/BounceStateResource")]
public class EchoDataResource : ScriptableObject
{

    [Header("Deflect Settings")]
    public int deflectsUntilMaxSpeed = 25;

    [Header("Speed Settings")]
    public float maxSpeed = 85;
    public float minSpeed = 20;
    public float igniteSpeed = 55;

}
