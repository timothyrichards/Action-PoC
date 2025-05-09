using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("Runtime")]
    public float cameraPitch;
    public float yawDelta;

    [Header("Look Up/Down Settings")]
    public Transform spineBone;
    public float spineRotationMultiplier = 1.0f;
    public float maxSpinePitch = 45f;
    public float maxSpineYaw = 30f;

    private Animator animator;
    private int noMaskCombatLayerIndex;
    private int maskCombatLayerIndex;
    private float layerTransitionSpeed = 5f;

    // Animation parameter hashes
    private readonly int horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int verticalHash = Animator.StringToHash("Vertical");
    private readonly int lookYawHash = Animator.StringToHash("LookYaw");
    private readonly int isWalkingHash = Animator.StringToHash("Walking");
    private readonly int isTurningHash = Animator.StringToHash("Turning");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int attackHash = Animator.StringToHash("Attack");

    public bool IsMoving => animator.GetBool(isWalkingHash);
    public bool IsTurning => animator.GetBool(isTurningHash);
    public bool IsJumping => animator.GetBool(jumpHash);
    public bool IsAttacking => animator.GetBool(attackHash);

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        noMaskCombatLayerIndex = animator.GetLayerIndex("No Mask Combat Layer");
        maskCombatLayerIndex = animator.GetLayerIndex("Mask Combat Layer");
    }

    private void LateUpdate()
    {
        if (spineBone != null)
        {
            // Normalize angles to -180 to 180 range
            float normalizedPitch = cameraPitch;
            if (normalizedPitch > 180f) normalizedPitch -= 360f;
            else if (normalizedPitch < -180f) normalizedPitch += 360f;

            float normalizedYaw = yawDelta;
            if (normalizedYaw > 180f) normalizedYaw -= 360f;
            else if (normalizedYaw < -180f) normalizedYaw += 360f;

            // Clamp to max values
            float clampedPitch = Mathf.Clamp(normalizedPitch, -maxSpinePitch, maxSpinePitch);
            float clampedYaw = Mathf.Clamp(normalizedYaw, -maxSpineYaw, maxSpineYaw);

            Vector3 localEuler = spineBone.localEulerAngles;
            localEuler.x = clampedPitch * spineRotationMultiplier;
            localEuler.y = clampedYaw;
            spineBone.localEulerAngles = localEuler;
        }
    }

    public void SetMovementAnimation(Vector2 movement, bool isWalking)
    {
        animator.SetFloat(horizontalHash, movement.x, 0.05f, Time.deltaTime);
        animator.SetFloat(verticalHash, movement.y, 0.05f, Time.deltaTime);
        animator.SetBool(isWalkingHash, isWalking);
    }

    public void SetTurningState(bool isTurning, float yawDelta)
    {
        animator.SetBool(isTurningHash, isTurning);
        animator.SetFloat(lookYawHash, yawDelta < 0 ? 0f : 1f);
    }

    public void TriggerJump()
    {
        animator.SetTrigger(jumpHash);
    }

    public void TriggerAttack()
    {
        animator.SetTrigger(attackHash);
    }

    public void UpdateCombatLayerWeight(bool isMoving, bool isGrounded)
    {
        float targetWeight = (isMoving || !isGrounded) ? 1f : 0f;

        float currentNoMaskWeight = animator.GetLayerWeight(noMaskCombatLayerIndex);
        float currentMaskWeight = animator.GetLayerWeight(maskCombatLayerIndex);

        float newNoMaskWeight = Mathf.Lerp(currentNoMaskWeight, 1f - targetWeight, layerTransitionSpeed * Time.deltaTime);
        float newMaskWeight = Mathf.Lerp(currentMaskWeight, targetWeight, layerTransitionSpeed * Time.deltaTime);

        animator.SetLayerWeight(noMaskCombatLayerIndex, newNoMaskWeight);
        animator.SetLayerWeight(maskCombatLayerIndex, newMaskWeight);
    }
}
