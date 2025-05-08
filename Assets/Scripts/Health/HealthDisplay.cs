using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    public HealthComponent healthComponent;

    private void OnEnable()
    {
        healthComponent.OnHealthChanged += UpdateHealth;
        UpdateHealth(healthComponent.CurrentHealth);
    }

    private void OnDisable()
    {
        if (healthComponent == null) return;

        healthComponent.OnHealthChanged -= UpdateHealth;
    }

    private void UpdateHealth(float health)
    {
        healthSlider.maxValue = healthComponent.MaxHealth;
        healthSlider.value = health;
    }
}
