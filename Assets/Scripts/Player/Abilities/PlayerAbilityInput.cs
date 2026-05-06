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
        Vector3 mouseScreen = GameInput.Instance.GetMousePosition();
        Vector3 aimWorld = _cam.ScreenToWorldPoint(
                                  new Vector3(mouseScreen.x, mouseScreen.y, _cam.nearClipPlane));
        _abilitySystem.UseAbility(slotIndex, aimWorld);
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnAbilityUsed -= OnAbilityUsed;
    }
}
