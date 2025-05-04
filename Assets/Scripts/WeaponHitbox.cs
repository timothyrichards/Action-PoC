using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour, IHitboxReceiver
{
    public float damage = 25f;
    public LayerMask enemyLayer;
    public List<Collider> hitboxColliders = new();
    private bool hitboxActive = false;
    private HashSet<Enemy> damagedEnemies = new();

    private void Awake()
    {
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
        damagedEnemies.Clear();
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

        // Only hit enemies
        if (((1 << other.gameObject.layer) & enemyLayer) != 0)
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && !damagedEnemies.Contains(enemy))
            {
                enemy.TakeDamage(damage);
                damagedEnemies.Add(enemy);
            }
        }
    }
}