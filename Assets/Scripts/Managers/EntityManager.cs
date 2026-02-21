using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour, ISimulationSnapshot<EntityManagerSnapshot>
{
    public const int MISSING_OWNER_ID = -420;
    public const int MISSING_ID = -69;
    Dictionary<int, EntityInfo> entityLookup = new();
    Dictionary<int, Transform> entityRegistry = new();
    Dictionary<Transform, int> idRegistry = new();
    Dictionary<EntityDatabaseID, List<Transform>> typeRegistry = new();
    public ISimulated.PriorityIndex Priority { get => ISimulated.PriorityIndex.EntityManager; set { } }

    int nextID;
    /// <summary>
    /// Registers an entity in the manager's database. 
    /// </summary>
    /// <returns> The ownerID of the registered entity. </returns>
    public int RegisterEntity(EntityDatabaseID t, Transform entity, int ownerID = -1)
    {
        if (!entityLookup.ContainsKey(ownerID))
        {
            ownerID = MISSING_OWNER_ID;
        }
        nextID++;
        var newID = nextID;
        
        entityLookup.Add(newID, new EntityInfo()
        {
            ownerID = ownerID,
            ID = newID,
            type = t
        });
        entityRegistry.Add(newID, entity);
        if (!typeRegistry.ContainsKey(t))
        {
            typeRegistry.Add (t, new List<Transform> { entity });
        }
        else
        {
            typeRegistry[t].Add(entity);
        }
        idRegistry.Add(entity, newID);
        return newID;
    }
    public void RemoveEntity(int id)
    {
        if (!entityLookup.ContainsKey(id)) return;
        var entity = entityLookup[id];
        foreach (var kvp in entityLookup)
        {
            var info = kvp.Value;
            if (info.ownerID == id)
            {
                info.ownerID = MISSING_OWNER_ID;
                entityLookup[info.ID] = info;
            }
        }
        if (entityRegistry.ContainsKey(id))
        {
            var trans = entityRegistry[id];
            typeRegistry[entity.type].Remove(trans);
            entityRegistry.Remove(id);
            idRegistry.Remove(trans);
        }
        entityLookup.Remove(id);
    }

    public Transform GetEntity(int id)
    {
        if (entityRegistry.ContainsKey(id))
        {
            return entityRegistry[id];
        }
        return null;
    }

    public List<Transform> GetEntitiesOfType(EntityDatabaseID type)
    {
        if (typeRegistry.ContainsKey(type))
        {
            return typeRegistry[type];
        }
        return null;
    }

    public int GetId(Transform entity)
    {
        if (entity == null) return MISSING_ID;
        if (idRegistry.ContainsKey(entity))
        {
            return idRegistry[entity];
        }
        return MISSING_ID;
    }


    public EntityManagerSnapshot CaptureState()
    {
        EntityManagerSnapshot snapshot = new ();
        int index = 0;
        foreach (var kvp in entityLookup)
        {
            switch (index)
            {
                case 0: snapshot.e1 = kvp.Value; break;
                case 1: snapshot.e2 = kvp.Value; break;
                case 2: snapshot.e3 = kvp.Value; break;
                case 3: snapshot.e4 = kvp.Value; break;
                case 4: snapshot.e5 = kvp.Value; break;
                case 5: snapshot.e6 = kvp.Value; break;
                case 6: snapshot.e7 = kvp.Value; break;
                case 7: snapshot.e8 = kvp.Value; break;
                case 8: snapshot.e9 = kvp.Value; break;
                case 9: snapshot.e10 = kvp.Value; break;
            }
            index++;
        }
        snapshot.numberOfEntities = (short) entityLookup.Count;
        
        return snapshot;
    }

    public void RestoreState(EntityManagerSnapshot snapshot)
    {
        entityLookup.Clear();
         if (snapshot.numberOfEntities > 0)
        {
            entityLookup[snapshot.e1.ID] = snapshot.e1;
            if (snapshot.numberOfEntities > 1)
            {
                entityLookup[snapshot.e2.ID] = snapshot.e2;
            }
            if (snapshot.numberOfEntities > 2)
            {
                entityLookup[snapshot.e3.ID] = snapshot.e3;
            }
            if (snapshot.numberOfEntities > 3)
            {
                entityLookup[snapshot.e4.ID] = snapshot.e4;
            }
            if (snapshot.numberOfEntities > 4)
            {
                entityLookup[snapshot.e5.ID] = snapshot.e5;
            }
            if (snapshot.numberOfEntities > 5)
            {
                entityLookup[snapshot.e6.ID] = snapshot.e6;
            }
            if (snapshot.numberOfEntities > 6)
            {
                entityLookup[snapshot.e7.ID] = snapshot.e7;
            }
            if (snapshot.numberOfEntities > 7)
            {
                entityLookup[snapshot.e8.ID] = snapshot.e8;
            }
            if (snapshot.numberOfEntities > 8)
            {
                entityLookup[snapshot.e9.ID] = snapshot.e9;
            }
            if (snapshot.numberOfEntities > 9)
            {
                entityLookup[snapshot.e10.ID] = snapshot.e10;
            }
        }
    }
   
}
public struct EntityInfo
{
    public int ownerID;
    public int ID;
    public EntityDatabaseID type;
}

public struct EntityManagerSnapshot
{
    public int numberOfEntities;
    public EntityInfo e1, e2, e3, e4, e5, e6, e7, e8, e9, e10;
}
public enum EntityDatabaseID
{
    Speaker,
    Echo,
    PrecedentClone,
    AnchorGrapple,
    RecallKnife,
    PolarityGrenade,
}

public interface IRegisterableEntity
{
    public EntityDatabaseID EntityType { set; get; }
    public int OwnerID { set; get; }
    public int ID { set; get; }
}