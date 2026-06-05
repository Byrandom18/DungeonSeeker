using UnityEngine;

public class PlayerAbilityInput : MonoBehaviour
{
    private AbilitySystem _abilitySystem;
    private Camera _cam;

    private void Awake()
    {
        _abilitySystem = GetComponent<AbilitySystem>();
        _cam = Camera.main;
    }

    private void Start()
    {
        GameInput.Instance.OnAbilityUsed += OnAbilityUsed;
    }

    private void OnAbilityUsed(int slotIndex)
    {
        Vector2 mouseScreen = GameInput.Instance.GetMousePosition();
        Vector3 aimWorld = AbilityTargetHelper.ScreenToGameplayPlane(_cam, mouseScreen, transform);
        _abilitySystem.UseAbility(slotIndex, aimWorld);
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnAbilityUsed -= OnAbilityUsed;
    }
}
