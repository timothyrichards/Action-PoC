using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    public WeaponHitbox weaponHitbox;
    public AnimationController animationController;

    public void ActivateHitbox()
    {
        weaponHitbox.ActivateHitbox();
    }

    public void DeactivateHitbox()
    {
        weaponHitbox.DeactivateHitbox();
    }

    public void EnableComboWindow()
    {
        animationController.EnableComboWindow();
    }

    public void DisableComboWindow()
    {
        animationController.DisableComboWindow();
    }
}
