using UnityEngine;
using UnityEngine.UI;
public class EnemyHealth : MonoBehaviour
{
    public Enemy enemy;
    public Slider healthSlider;

    void Update()
    {
        healthSlider.value = enemy.health;
    }
}
