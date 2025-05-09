using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public HealthComponent HealthComponent { get; private set; }

    protected virtual void Awake()
    {
        HealthComponent = GetComponent<HealthComponent>();
    }

    public virtual void TakeDamage(float damage)
    {
        HealthComponent.TakeDamage(damage);
        if (HealthComponent.CurrentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void ResetHealth()
    {
        HealthComponent.ResetHealth();
    }

    protected abstract void Die();
}