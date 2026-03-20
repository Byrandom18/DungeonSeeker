using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    private InputSystem_Actions _inputActions;
    public bool CanAttack;
    public event EventHandler OnPlayerAttack;
    public event EventHandler OnPlayerDodge;
    public event EventHandler OnInventoryButton;

    private void Awake()
    {
        Instance = this;
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
        
    }

    private void Start()
    {
        //_inputActions.Player.Attack.performed += PlayerAttack_performed;
        _inputActions.Player.Dodge.started += PlayerDodge_started;
        _inputActions.UI.Inventory.started += Inventory_started;
        CanAttack = true;
    }

    private void Update()
    {
        if (_inputActions.Player.Attack.IsPressed() && CanAttack)
        {
            OnPlayerAttack?.Invoke(this, EventArgs.Empty);
        }
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

    private void Inventory_started(InputAction.CallbackContext obj)
    {
        OnInventoryButton?.Invoke(this, EventArgs.Empty);
    }

    private void PlayerDodge_started(InputAction.CallbackContext obj)
    {
        OnPlayerDodge?.Invoke(this, EventArgs.Empty);
    }

    //private void PlayerAttack_performed(InputAction.CallbackContext obj)
    //{
    //    OnPlayerAttack?.Invoke(this, EventArgs.Empty);
    //}

    private void OnDestroy()
    {
        //_inputActions.Player.Attack.started -= PlayerAttack_performed;
        _inputActions.Player.Dodge.started -= PlayerDodge_started;
    }
}
