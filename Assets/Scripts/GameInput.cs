using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    private InputSystem_Actions inputActions;

    public event EventHandler OnPlayerAttack;
    public event EventHandler OnPlayerDodge;

    private void Awake()
    {
        Instance = this;
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        inputActions.Player.Attack.started += PlayerAttack_started;
        inputActions.Player.Dodge.started += PlayerDodge_started;
    }

    public Vector2 GetMovementVector()
    {
        return inputActions.Player.Move.ReadValue<Vector2>();
    }

    private void PlayerDodge_started(InputAction.CallbackContext obj)
    {
        OnPlayerDodge?.Invoke(this, EventArgs.Empty);
    }

    private void PlayerAttack_started(InputAction.CallbackContext obj)
    {
        OnPlayerAttack?.Invoke(this, EventArgs.Empty);
    }
}
