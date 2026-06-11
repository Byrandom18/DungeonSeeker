using UnityEngine;

/// <summary>
/// On-screen debug info for ally AI decisions. Disable on release builds.
/// </summary>
[DisallowMultipleComponent]
public class AllyAIDebugOverlay : MonoBehaviour
{
    [SerializeField] private AllyAIBrain _brain;
    [SerializeField] private AllyMLBridge _mlBridge;
    [SerializeField] private AllyMLMovementModifier _movementModifier;
    [SerializeField] private ThreatPerception _threatPerception;
    [SerializeField] private bool _showOverlay = true;
    [SerializeField] private Vector2 _screenOffset = new Vector2(12f, 12f);

    private void Awake()
    {
        if (_brain == null)
            _brain = GetComponent<AllyAIBrain>();
        if (_mlBridge == null)
            _mlBridge = GetComponent<AllyMLBridge>();
        if (_movementModifier == null)
            _movementModifier = GetComponent<AllyMLMovementModifier>();
        if (_threatPerception == null)
            _threatPerception = GetComponent<ThreatPerception>();
    }

    private void OnGUI()
    {
        if (!_showOverlay || _brain == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 world = _brain.transform.position + Vector3.up * 1.2f;
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f)
            return;

        float x = screen.x + _screenOffset.x;
        float y = Screen.height - screen.y + _screenOffset.y;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 11
        };

        TargetSelectionResult weaponTarget = _brain.LastWeaponTarget;
        AbilityDecision ability = _brain.LastAbilityDecision;
        CombatSnapshot snapshot = _brain.Snapshot;

        string weaponName = weaponTarget.Target != null ? weaponTarget.Target.name : "none";
        string abilityName = ability.IsValid
            ? _brain.GetAbilityDisplayName(ability.SlotIndex)
            : "none";

        ThreatSnapshot threat = _threatPerception != null ? _threatPerception.LastSnapshot : default;
        string mlLine = _mlBridge != null
            ? $"\nML dist: {_mlBridge.PreferredDistance:0.00}  retreat: {_mlBridge.RetreatUrgency:0.00}" +
              $"\nStrafe: {_mlBridge.GetStrafeDirectionSigned():0.00} x {_mlBridge.StrafeIntensity:0.00}" +
              $"\nThreat: {threat.IncomingThreatUrgency:0.00}  atkSelf: {threat.AttackingSelfCount}  atkAll: {threat.AttackingEnemyCount}" +
              (_movementModifier != null
                  ? $"\nML move: {(_movementModifier.IsControllingMovement ? "ON" : "off")}  mode: {_movementModifier.CurrentMode}" +
                    (_movementModifier.IsInLowHealthEscape
                        ? $"  escape until {_movementModifier.LowHealthExitThreshold:0}%"
                        : string.Empty)
                  : string.Empty)
            : string.Empty;

        string text =
            $"[{_brain.ProfileLabel}]\n" +
            $"HP {snapshot.HealthPercent * 100f:0}%  MP {snapshot.ManaPercent * 100f:0}%\n" +
            $"Enemies: {snapshot.EnemyCount}  cluster: {snapshot.ClusteredEnemyCount}\n" +
            $"Weapon target: {weaponName} ({weaponTarget.Score:0.00})\n" +
            $"Ability: {abilityName} ({ability.Score:0.00})" +
            mlLine;

        GUI.Box(new Rect(x, y, 260f, 145f), text, style);
    }
}
