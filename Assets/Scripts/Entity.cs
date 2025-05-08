using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    protected HealthComponent healthComponent;

    protected virtual void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
    }

    public virtual void TakeDamage(float damage)
    {
        healthComponent.TakeDamage(damage);
        if (healthComponent.CurrentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void ResetHealth()
    {
        healthComponent.ResetHealth();
    }

    protected abstract void Die();
}