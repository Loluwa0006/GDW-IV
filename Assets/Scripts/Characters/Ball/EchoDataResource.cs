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


    [HideInInspector] public int deflectStreak = 1;




    [HideInInspector] public float currentSpeed;
    [HideInInspector] public float activeMinSpeed;
    [HideInInspector] public float activeMaxSpeed;


  [HideInInspector]  public Vector3 oldSpeed;



    public void ResetData()
    {
        deflectStreak = 1;
        activeMinSpeed = minSpeed;
        activeMaxSpeed = maxSpeed;
    }

}
