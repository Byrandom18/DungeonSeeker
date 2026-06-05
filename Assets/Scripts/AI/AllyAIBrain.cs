using UnityEngine;

/// <summary>
/// Central ally decision hub. Behavior graph actions call into this component.
/// </summary>
[DisallowMultipleComponent]
public class AllyAIBrain : MonoBehaviour
{
    [SerializeField] private CombatPerception _perception;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private AbilitySystem _abilitySystem;
    [SerializeField] private ActiveWeapon _activeWeapon;
    [SerializeField] private AllyMLBridge _mlBridge;
    [SerializeField] private AllyAIProfile _profile;
    [SerializeField] private AllyCombatProfileType _fallbackProfileType = AllyCombatProfileType.Aggressive;
    [SerializeField] private TargetScoreWeights _weaponTargetWeights;
    [SerializeField] private AbilityScoreWeights _abilityScoreWeights;
    [SerializeField] private float _snapshotRefreshInterval = 0.2f;

    private UtilityAbilitySelector _abilitySelector;
    private CombatSnapshot _cachedSnapshot;
    private float _snapshotTimer;
    private Transform _currentWeaponTarget;
    private AbilityDecision _lastAbilityDecision;
    private TargetSelectionResult _lastWeaponTarget;
    private float _profileDistanceScale = 1f;
    private string _profileLabel = "Default";

    public CombatSnapshot Snapshot => _cachedSnapshot;
    public Transform CurrentWeaponTarget => _currentWeaponTarget;
    public AbilityDecision LastAbilityDecision => _lastAbilityDecision;
    public TargetSelectionResult LastWeaponTarget => _lastWeaponTarget;
    public AllyAIProfile Profile => _profile;
    public string ProfileLabel => _profileLabel;
    public float ProfileDistanceScale => _profileDistanceScale;

    public float HealthPercent =>
        _stats != null && _stats.MaxHealth > 0f ? _stats.Health / _stats.MaxHealth : 1f;

    public float ManaPercent =>
        _stats != null && _stats.MaxMana > 0f ? _stats.Mana / _stats.MaxMana : 1f;

    public float AttackDistance
    {
        get
        {
            WeaponBase weapon = _activeWeapon != null ? _activeWeapon.GetActiveWeapon() : null;
            float baseRange = 1.5f;
            if (weapon != null && weapon.WeaponData != null)
                baseRange = CombatPerception.GetWeaponRange(weapon.WeaponData);

            float scale = _profileDistanceScale;
            if (_mlBridge != null)
                scale *= _mlBridge.GetCombatDistanceScale();

            return baseRange * scale;
        }
    }

    private void Awake()
    {
        if (_perception == null)
            _perception = GetComponent<CombatPerception>();
        if (_stats == null)
            _stats = GetComponent<PlayerStats>();
        if (_abilitySystem == null)
            _abilitySystem = GetComponent<AbilitySystem>();
        if (_activeWeapon == null)
            _activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
        if (_mlBridge == null)
            _mlBridge = GetComponent<AllyMLBridge>();

        if (_profile == null)
            ApplyProfile(AllyAIProfile.CreateRuntimeDefault(_fallbackProfileType));
        else
            ApplyProfile(_profile);
    }

    private void OnDisable()
    {
        if (_currentWeaponTarget != null)
        {
            PartyCombatCoordinator.UnregisterFocus(_currentWeaponTarget);
            _currentWeaponTarget = null;
        }
    }

    private void Update()
    {
        _snapshotTimer -= Time.deltaTime;
        if (_snapshotTimer <= 0f)
        {
            _snapshotTimer = _snapshotRefreshInterval;
            RefreshSnapshot();
        }
    }

    public void ApplyProfile(AllyAIProfile profile)
    {
        _profile = profile;

        if (_profile != null)
        {
            _profileLabel = _profile.ProfileType.ToString();
            _profileDistanceScale = _profile.PreferredCombatDistanceScale;

            if (_profile.WeaponTargetWeights != null)
                _weaponTargetWeights = _profile.WeaponTargetWeights;
            if (_profile.AbilityScoreWeights != null)
                _abilityScoreWeights = _profile.AbilityScoreWeights;

            _mlBridge?.ApplyProfileDefaults(_profile);
        }
        else
        {
            _profileLabel = "Default";
            _profileDistanceScale = 1f;
        }

        _abilitySelector = new UtilityAbilitySelector(_abilityScoreWeights);
    }

    public CombatSnapshot RefreshSnapshot()
    {
        if (_perception == null)
            return default;

        _cachedSnapshot = _perception.BuildSnapshot();
        return _cachedSnapshot;
    }

    public TargetSelectionResult SelectWeaponTarget(bool forceRefresh = false)
    {
        if (forceRefresh || _cachedSnapshot.Enemies == null)
            RefreshSnapshot();

        _lastWeaponTarget = WeaponTargetSelector.Select(_cachedSnapshot, _weaponTargetWeights);

        if (_currentWeaponTarget != _lastWeaponTarget.Target)
        {
            if (_currentWeaponTarget != null)
                PartyCombatCoordinator.UnregisterFocus(_currentWeaponTarget);

            _currentWeaponTarget = _lastWeaponTarget.Target;

            if (_currentWeaponTarget != null)
                PartyCombatCoordinator.RegisterFocus(_currentWeaponTarget);
        }

        return _lastWeaponTarget;
    }

    public AbilityDecision SelectAbility(bool forceRefresh = false)
    {
        if (forceRefresh || _cachedSnapshot.Enemies == null)
            RefreshSnapshot();

        _lastAbilityDecision = _abilitySelector.Select(_cachedSnapshot, _abilitySystem, _activeWeapon);
        return _lastAbilityDecision;
    }

    public bool TryUseSelectedAbility()
    {
        AbilityDecision decision = SelectAbility();
        if (!decision.IsValid || _abilitySystem == null)
            return false;

        return _abilitySystem.UseAbility(decision.SlotIndex, decision.AimPosition);
    }

    public string GetAbilityDisplayName(int slotIndex)
    {
        if (_abilitySystem == null)
            return $"slot {slotIndex}";

        AbilitySO data = _abilitySystem.GetAbilityData(slotIndex);
        if (data == null)
            return $"slot {slotIndex}";

        return string.IsNullOrWhiteSpace(data.DisplayName) ? data.AbilityType.ToString() : data.DisplayName;
    }
}
