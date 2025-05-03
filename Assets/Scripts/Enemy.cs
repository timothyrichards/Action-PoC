using UnityEngine;
using System.Collections;
using System;

public class Enemy : MonoBehaviour
{
    public float health = 100f;
    public float fallDuration = 3f;
    private bool isDown = false;
    private float maxHealth;

    public Action DamageTaken;

    void Awake()
    {
        maxHealth = health;
    }

    public void TakeDamage(float amount)
    {
        if (isDown) return;

        health -= amount;
        DamageTaken?.Invoke();
        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        StartCoroutine(FallAndRecover());
    }

    IEnumerator FallAndRecover()
    {
        isDown = true;

        // Save the original and fallen rotations
        Quaternion startRot = transform.rotation;
        Quaternion fallenRot = Quaternion.Euler(-90f, transform.eulerAngles.y, 0f);

        // Fall over (lerp to fallenRot)
        float t = 0f;
        float fallTime = 0.5f;
        while (t < 1f)
        {
            t += Time.deltaTime / fallTime;
            transform.rotation = Quaternion.Lerp(startRot, fallenRot, t);
            yield return null;
        }
        transform.rotation = fallenRot;

        yield return new WaitForSeconds(fallDuration);

        // Stand up (lerp back to startRot)
        t = 0f;
        float standTime = 0.5f;
        while (t < 1f)
        {
            t += Time.deltaTime / standTime;
            transform.rotation = Quaternion.Lerp(fallenRot, startRot, t);
            yield return null;
        }
        transform.rotation = startRot;

        health = maxHealth;
        isDown = false;
    }
}