using UnityEngine;

[AddComponentMenu("Weapons/Two Handed Weapon IK")]
public class WeaponTwoHandIK : MonoBehaviour
{
    [Tooltip("Transform where the left hand should grip the weapon.")]
    public Transform leftHandGrip;

    [Tooltip("Animator of the character wielding this weapon. If left null, will auto-find in parents.")]
    public Animator characterAnimator;

    private Transform leftHandBone;

    private void Start()
    {
        // Auto-find the character Animator if not assigned
        if (characterAnimator == null)
            characterAnimator = GetComponentInParent<Animator>();

        if (characterAnimator != null)
            leftHandBone = characterAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        else
            Debug.LogWarning("WeaponTwoHandIK: No Animator found in parents.");
    }

    private void LateUpdate()
    {
        if (leftHandBone == null || leftHandGrip == null) return;

        // Snap left hand to the grip point on the weapon
        leftHandBone.position = leftHandGrip.position;
        leftHandBone.rotation = leftHandGrip.rotation;
    }
}