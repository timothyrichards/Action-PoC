using UnityEngine;
using System;

public class Entity : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float health { get; set; } = 100f;
    public float maxHealth { get; set; } = 100f;

    public Action<float> OnHealthChanged { get; set; }

    protected virtual void Awake()
    {
        maxHealth = health;
    }

    public virtual void ResetHealth()
    {
        health = maxHealth;
        OnHealthChanged?.Invoke(health);
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        OnHealthChanged?.Invoke(health);

        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        Debug.Log($"{gameObject.name} died");
    }
}