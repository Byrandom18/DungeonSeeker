using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
public class GameInput : MonoBehaviour
{
    public static GameInput instance { get; private set; }

    private InputSystem_Actions inputActions;

    public event EventHandler OnPlayerAttack;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Player.Attack.started += PlayerAttack_started;
    }

    private void PlayerAttack_started(InputAction.CallbackContext obj)
    {
        
    }
}
