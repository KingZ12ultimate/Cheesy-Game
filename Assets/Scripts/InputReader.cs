using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReader")]
public class InputReader : ScriptableObject, GameInput.IPlayerActions
{
    public event UnityAction<Vector2> moveEvent;
    public event UnityAction<Vector2> lookEvent;
    public event UnityAction jumpEvent;
    public event UnityAction jumpCanceledEvent;
    
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
        
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (jumpEvent != null && context.phase == InputActionPhase.Started)
            jumpEvent.Invoke();

        if (jumpCanceledEvent != null && context.phase == InputActionPhase.Canceled)
            jumpCanceledEvent.Invoke();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (lookEvent != null)
        {
            lookEvent.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (moveEvent != null)
        {
            moveEvent.Invoke(context.ReadValue<Vector2>());
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
