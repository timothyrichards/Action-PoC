using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Combat Settings")]
    [SerializeField] private float comboWindowTime = 1.0f;
    [SerializeField] private int maxComboCount = 3;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 10f;

    private int noMaskCombatLayerIndex;
    private int maskCombatLayerIndex;
    private float layerTransitionSpeed = 5f;
    private int currentCombo = 0;
    private float lastAttackTime;
    private bool canContinueCombo = true;

    // Animation parameter hashes
    private readonly int horizontalHash = Animator.StringToHash("Horizontal");
    private readonly int verticalHash = Animator.StringToHash("Vertical");
    private readonly int lookYawHash = Animator.StringToHash("LookYaw");
    private readonly int comboCountHash = Animator.StringToHash("ComboCount");
    private readonly int isWalkingHash = Animator.StringToHash("Walking");
    private readonly int isTurningHash = Animator.StringToHash("Turning");
    private readonly int isGroundedHash = Animator.StringToHash("Grounded");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int attackHash = Animator.StringToHash("Attack");

    public bool IsMoving => animator.GetBool(isWalkingHash);
    public bool IsTurning => animator.GetBool(isTurningHash);
    public bool IsJumping => animator.GetBool(jumpHash);
    public bool IsAttacking => animator.GetBool(attackHash);
    public int ComboCount => animator.GetInteger(comboCountHash);

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        noMaskCombatLayerIndex = animator.GetLayerIndex("No Mask Combat Layer");
        maskCombatLayerIndex = animator.GetLayerIndex("Mask Combat Layer");
    }

    private void Update()
    {
        animator.SetBool(isGroundedHash, PlayerEntity.LocalPlayer.controller.IsGrounded);

        // Check if combo window has expired
        if (Time.time - lastAttackTime > comboWindowTime && currentCombo > 0)
        {
            ResetCombo();
        }
    }

    public void SetMovementAnimation(Vector2 movement, bool isWalking)
    {
        // Use faster interpolation time when landing to make the transition snappier
        float interpolationTime = !animator.GetBool(isGroundedHash) ? 0.05f : 0.1f;

        // Maintain movement values during landing for smoother transitions
        animator.SetFloat(horizontalHash, movement.x, interpolationTime, Time.deltaTime);
        animator.SetFloat(verticalHash, movement.y, interpolationTime, Time.deltaTime);
        animator.SetBool(isWalkingHash, isWalking);

        // Rotate the model based on movement direction
        if (movement.magnitude > 0.1f)
        {
            // Calculate the target rotation based on movement direction
            float targetAngle = Mathf.Atan2(movement.x, movement.y) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);

            // Smoothly rotate towards the target rotation
            animator.transform.localRotation = Quaternion.Lerp(animator.transform.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetTurningState(bool isTurning, float yawDelta)
    {
        animator.SetBool(isTurningHash, isTurning);
        animator.SetFloat(lookYawHash, yawDelta < 0 ? 0f : 1f);
    }

    public void TriggerJump()
    {
        animator.SetTrigger(jumpHash);
        animator.SetBool(isGroundedHash, false);
    }

    public void TriggerAttack()
    {
        // If we're outside the combo window, reset the combo
        if (Time.time - lastAttackTime > comboWindowTime)
        {
            ResetCombo();
        }

        // Only proceed if we can continue the combo
        if (currentCombo < maxComboCount && canContinueCombo)
        {
            animator.SetTrigger(attackHash);
            animator.SetInteger(comboCountHash, currentCombo);
            DisableComboWindow();
            lastAttackTime = Time.time;
            currentCombo++;
        }
    }

    public void EnableComboWindow()
    {
        // Called via Animation Event when we can start the next attack
        canContinueCombo = true;
    }

    public void DisableComboWindow()
    {
        // Called via Animation Event when we can no longer combo
        canContinueCombo = false;
    }

    private void ResetCombo()
    {
        currentCombo = 0;
        canContinueCombo = true;
        animator.SetInteger(comboCountHash, 0);
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
