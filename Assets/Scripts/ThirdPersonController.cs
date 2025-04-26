using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public Transform cameraTransform;
    public Animator animator;

    private int noMaskCombatLayerIndex;
    private int maskCombatLayerIndex;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isMoving;
    private bool isGrounded;
    private bool jumpQueued;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            noMaskCombatLayerIndex = animator.GetLayerIndex("No Mask Combat Layer");
            maskCombatLayerIndex = animator.GetLayerIndex("Mask Combat Layer");
        }
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetBool("Walking", isMoving);
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            jumpQueued = true;
            animator.SetTrigger("Jump");
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
            animator.SetTrigger("Melee");
    }

    private void Update()
    {
        HandleLook();
        HandleMove();

        var isMovingOrJumping = isMoving || !isGrounded;
        animator.SetLayerWeight(noMaskCombatLayerIndex, isMovingOrJumping ? 0f : 1f);
        animator.SetLayerWeight(maskCombatLayerIndex, isMovingOrJumping ? 1f : 0f);
    }

    private void HandleLook()
    {
        if (lookInput.sqrMagnitude > 0.01f)
        {
            float yaw = lookInput.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, yaw, 0);
        }
    }

    private void HandleMove()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        if (move.magnitude > 0.01f)
        {
            move = Quaternion.Euler(0, cameraTransform ? cameraTransform.eulerAngles.y : transform.eulerAngles.y, 0) * move;
        }
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (jumpQueued)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpQueued = false;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}