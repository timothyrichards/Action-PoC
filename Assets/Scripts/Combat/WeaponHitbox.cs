using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour, IHitboxReceiver
{
    public Entity wielder;
    public float damage = 25f;
    public LayerMask entityLayer;
    public List<Collider> hitboxColliders = new();
    private bool hitboxActive = false;
    private HashSet<Entity> damagedEntities = new();

    private void Awake()
    {
        wielder = GetComponentInParent<Entity>();

        // Optionally auto-populate hitboxColliders with all child colliders if empty
        if (hitboxColliders.Count == 0)
        {
            hitboxColliders.AddRange(GetComponentsInChildren<Collider>());
        }

        // Disable all hitboxes at start
        foreach (var col in hitboxColliders)
        {
            col.enabled = false;
        }
    }

    public void ActivateHitbox()
    {
        hitboxActive = true;
        damagedEntities.Clear();
        foreach (var col in hitboxColliders)
        {
            col.enabled = true;
        }
    }

    public void DeactivateHitbox()
    {
        hitboxActive = false;
        foreach (var col in hitboxColliders)
        {
            col.enabled = false;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        TriggerEnterHandler(other);
    }

    public void OnHitboxTriggerEnter(Collider other)
    {
        TriggerEnterHandler(other);
    }

    public void TriggerEnterHandler(Collider other)
    {
        if (!hitboxActive) return;

        if (other.gameObject == wielder.gameObject) return;

        // Only entities on the designated entity layer
        if (((1 << other.gameObject.layer) & entityLayer) != 0)
        {
            Entity entity = other.GetComponent<Entity>();
            if (entity != null && !damagedEntities.Contains(entity))
            {
                entity.TakeDamage(damage);
                damagedEntities.Add(entity);
            }
        }
    }
}