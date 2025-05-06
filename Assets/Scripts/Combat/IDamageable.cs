using System;

public interface IDamageable
{
    float health { get; set; }
    float maxHealth { get; set; }
    Action<float> OnHealthChanged { get; set; }

    void TakeDamage(float damage);
    void Die();
}
