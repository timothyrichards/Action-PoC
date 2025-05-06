using UnityEngine;

public interface IHitboxReceiver
{
    void OnHitboxTriggerEnter(Collider other);
}