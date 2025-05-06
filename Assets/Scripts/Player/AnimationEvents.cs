using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public WeaponHitbox weaponHitbox;

    public void ActivateHitbox()
    {
        weaponHitbox.ActivateHitbox();
    }

    public void DeactivateHitbox()
    {
        weaponHitbox.DeactivateHitbox();
    }
}
