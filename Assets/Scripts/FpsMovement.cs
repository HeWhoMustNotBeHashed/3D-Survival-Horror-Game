using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class FpsMovement : MonoBehaviour
{

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;


    [Header("Crouching")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float cameraOffset = 0.4f;
    [Header("Jump and Fall")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -12f;
    [SerializeField] private float initialFallVelocity = -2f;


    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference crouchAction;

    private CharacterController _characterController;
    private Vector2 _moveInput;

    private bool _isGrounded;
    private float targetHeight;
    private bool _isRunning;
    private bool _isCrouching;
       
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        targetHeight = standingHeight;
    }
    private void OnEnable()
    {
        moveAction.action.performed += StoreMovementInput;
        moveAction.action.canceled += StoreMovementInput;
        jumpAction.action.performed += Jump;

        sprintAction.action.performed += Sprint;
        sprintAction.action.canceled += Sprint;
        crouchAction.action.performed += Crouch;
        
    }

    private void Sprint(InputAction.CallbackContext context)
    {
        _isRunning = context.performed;
    }

    private void Crouch(InputAction.CallbackContext context)
    {
        if (_isCrouching)
        {

            if (!CanStandUp())
            {
                return;
            }
            targetHeight = standingHeight;
        }
        else
        {
            targetHeight = crouchingHeight;
        }
        _isCrouching = !_isCrouching;
    }

    private bool CanStandUp()
    {
        float radius = _characterController.radius;
        float height = _characterController.height;
        Vector3 centerWorld = transform.position + _characterController.center;

        Vector3 bottom = centerWorld - Vector3.up * (height * 0.5f - radius);
        Vector3 top = centerWorld + Vector3.up * (height * 0.5f - radius);

        float distance = standingHeight - height;   

        return !Physics.CapsuleCast(
            bottom, top, radius * 0.95f, Vector3.up, distance,
            ~0, QueryTriggerInteraction.Ignore);
    }
    private void OnDisable()
    {
        moveAction.action.performed -= StoreMovementInput;
        moveAction.action.canceled -= StoreMovementInput;
        jumpAction.action.performed -= Jump;

        sprintAction.action.performed -= Sprint;
        sprintAction.action.canceled -= Sprint;
        crouchAction.action.performed -= Crouch;
    }

    private void StoreMovementInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (_isGrounded)
        {
            _verticalVelocity = jumpForce;
        }
    }


    void Update()
    {
        _isGrounded = _characterController.isGrounded;
        HandleGravity();
        HandleMovement();
        HandleCrouchTransition();
            
    }

    private void HandleGravity()
    {
        if (_isGrounded && _verticalVelocity < 0)
            _verticalVelocity = initialFallVelocity;
        _verticalVelocity += gravity * Time.deltaTime;
    }
 
    private void HandleMovement()
    {
        var move = cameraTransform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.y)).normalized;
        var currentSpeed = _isCrouching ? crouchSpeed : (_isRunning ? runSpeed : walkSpeed);
        var finalMov = currentSpeed * move;
        finalMov.y = _verticalVelocity;
            
        var collisions = _characterController.Move(finalMov * Time.deltaTime);
        if ((collisions & CollisionFlags.Above) != 0)
        {
            _verticalVelocity = initialFallVelocity;
        } 
    }


    private void HandleCrouchTransition()
    {
        var currentHeight = _characterController.height;
        if(Math.Abs(currentHeight - targetHeight) == 0)
        {
            _characterController.height = targetHeight; 
            return;
        }

        var newHeight = Mathf.Lerp(currentHeight,targetHeight,crouchTransitionSpeed * Time.deltaTime);
        _characterController.height = newHeight;
        _characterController.center = Vector3.up * (newHeight * 0.5f);

        var cameraTargetPosition = cameraTransform.localPosition;
        cameraTargetPosition.y = targetHeight - cameraOffset;
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraTargetPosition, crouchTransitionSpeed * Time.deltaTime);

    }
}
    