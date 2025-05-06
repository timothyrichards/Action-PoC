using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    public Entity damageableObject;
    public Slider healthSlider;

    void OnEnable()
    {
        if (damageableObject == null)
        {
            Debug.LogError($"HealthDisplay is not assigned to a damageable object", this);
            return;
        }

        damageableObject.OnHealthChanged += UpdateHealth;
    }

    void UpdateHealth(float health)
    {
        healthSlider.maxValue = damageableObject.maxHealth;
        healthSlider.value = health;
    }

    void OnDestroy()
    {
        if (damageableObject != null)
        {
            damageableObject.OnHealthChanged -= UpdateHealth;
        }
    }
}
