using System;

public interface IHealth
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    event Action<float> OnHealthChanged;
    void TakeDamage(float damage);
    void ResetHealth();
}