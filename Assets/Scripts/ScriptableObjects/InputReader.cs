using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReader")]
public class InputReader : ScriptableObject, GameInput.IPlayerActions
{
    public event UnityAction<Vector2> MoveEvent;
    public event UnityAction<Vector2> LookEvent;
    public event UnityAction JumpEvent;
    public event UnityAction JumpCanceledEvent;
    public event UnityAction AttackEvent;
    public event UnityAction dashEvent;
    
    public GameInput gameInput;

    private void OnEnable()
    {
        if (gameInput == null)
        {
            gameInput = new GameInput();
            gameInput.Player.SetCallbacks(this);
        }

        gameInput.Player.Enable();
    }

    private void OnDisable()
    {
        gameInput.Player.Disable();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (AttackEvent != null)
            AttackEvent.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (dashEvent != null)
            dashEvent.Invoke();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (JumpEvent != null && context.phase == InputActionPhase.Started)
            JumpEvent.Invoke();

        if (JumpCanceledEvent != null && context.phase == InputActionPhase.Canceled)
            JumpCanceledEvent.Invoke();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (LookEvent != null)
        {
            LookEvent.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (MoveEvent != null)
        {
            MoveEvent.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        
    }
}
