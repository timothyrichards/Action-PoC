using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour, IHealth
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float _currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = value;
            OnHealthChanged?.Invoke(_currentHealth);
        }
    }

    public event Action<float> OnHealthChanged;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void SetHealth(float health, float maxHealth)
    {
        this.maxHealth = maxHealth;
        CurrentHealth = health;
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
    }
}