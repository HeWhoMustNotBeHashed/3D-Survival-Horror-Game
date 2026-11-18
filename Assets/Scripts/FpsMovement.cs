using UnityEngine;
using UnityEngine.InputSystem;

public class FpsMovement : MonoBehaviour
{

    [Header("Speeds")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionReference moveAction;

    private CharacterController characterController;
    private Vector2 _moveInput;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }
    private void OnEnable()
    {
        moveAction.action.performed += StoreMovementInput;
        moveAction.action.canceled += StoreMovementInput;
    }

    private void OnDisable()
    {
        moveAction.action.performed -= StoreMovementInput;
        moveAction.action.canceled -= StoreMovementInput;
    }

    private void StoreMovementInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }



    void Update()
    {
        HandleMovement();
    }


    private void HandleMovement()
    {

    }
}
