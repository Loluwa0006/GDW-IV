using System.Collections.Generic;
using UnityEngine;

public class IDComponent : MonoBehaviour
{
    [SerializeField] EntityDatabaseID idType;
    public EntityDatabaseID IDType { get => idType; private set => idType = value; }

    public int ID { get; private set; }

    public bool Initalized { get; private set; }
    public void InitComponent(GameManager manager)
    {
        Initalized = true;
        manager.entityManager.RegisterEntity(IDType, transform);
    }
}
