using UnityEngine;

public class WeaponTwoHandIK : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform leftHandTarget;

    void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            // Set left hand IK
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
        }
    }
}
