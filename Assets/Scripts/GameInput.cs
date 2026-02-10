using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    private InputSystem_Actions _inputActions;

    public event EventHandler OnPlayerAttack;
    public event EventHandler OnPlayerDodge;

    private void Awake()
    {
        Instance = this;
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
        _inputActions.Player.Attack.started += PlayerAttack_started;
        _inputActions.Player.Dodge.started += PlayerDodge_started;
    }

    public Vector2 GetMousePosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        return mousePos;
    }

    public Vector2 GetMovementVector()
    {
        return _inputActions.Player.Move.ReadValue<Vector2>();
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
