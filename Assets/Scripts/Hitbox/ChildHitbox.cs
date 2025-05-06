using UnityEngine;

public class ChildHitbox : MonoBehaviour
{
    private IHitboxReceiver parentHitbox;

    private void Awake()
    {
        parentHitbox = GetComponentInParent<IHitboxReceiver>();
    }

    private void OnTriggerEnter(Collider other)
    {
        parentHitbox.OnHitboxTriggerEnter(other);
    }
}