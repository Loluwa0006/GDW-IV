using UnityEngine;
using System.Collections.Generic;

public static class ProjectileHelper
{
    public static RaycastHit CollisionLogic(Vector3 previous, Vector3 current, LayerMask collisionMask, Collider collider, QueryTriggerInteraction collideWithTriggers)
    {
        Vector3 travelVector = current - previous;
        float checkerDistance = travelVector.magnitude;
        if (checkerDistance < 0.001f) return new RaycastHit();

        Ray ray = new(previous, travelVector.normalized);

        var hits = Physics.RaycastAll(ray, checkerDistance, collisionMask, collideWithTriggers);
        foreach ( var hit in hits)
        {
            if (hit.collider != collider)
            {
                return hit;
            }
        }
        return new RaycastHit();
    }

    public static List<T> GetOverlappingEntities<T>(Collider hitbox, LayerMask collisionMask, bool checkChildren) where T: MonoBehaviour
    {
        List<T> components = new();
        var overlap = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.size / 2.0f, hitbox.transform.rotation, collisionMask, QueryTriggerInteraction.Collide);

        foreach (var entity in overlap)
        {
            Debug.Log("Projectile " + hitbox.transform.parent.name + " detected entity " + entity.name);
            if (entity.TryGetComponent(out T component)) components.Add(component);
            else if (checkChildren)
            {
                var comp = entity.GetComponentInChildren<T>();
                if (comp != null) components.Add(comp);
            }
            if (!components.Contains(component))
            {
                Debug.Log("Entity " + entity.name + " has no component of type " + typeof(T).Name);
            }
        }
        return components;
    }

}
