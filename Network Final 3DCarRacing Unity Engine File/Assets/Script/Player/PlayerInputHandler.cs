using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput playerInput;

    public Vector2 throttle;
    public Vector2 steer;
    public bool brake = false; 
    // public bool handbrake = false;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerInput.onActionTriggered += OnActionTrigger;
    }

    private void OnDisable()
    {
        playerInput.onActionTriggered -= OnActionTrigger;
    }
    
    void OnActionTrigger(InputAction.CallbackContext context)
    {
        switch (context.action.name)
        {
            case "Steer":
                steer = context.ReadValue<Vector2>();
                break;

            case "Throttle":
                throttle = context.ReadValue<Vector2>();
                break;
            
            case "Brake":
                brake = context.ReadValueAsButton();
                break;
            
            /*case "Handbrake":
                handbrake = context.ReadValueAsButton();
                break;*/
        }
    }
}
