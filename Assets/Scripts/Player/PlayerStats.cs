using System;
using UnityEngine;
using UnityEngine.UI;


public class PlayerStats : MonoBehaviour, ICharacterEntity
{
    public static PlayerStats Instance { get; private set; }

    /// <summary>
    /// Single human-controlled character in the scene registers the singleton.
    /// Bots use the same stats component with this disabled.
    /// </summary>
    [SerializeField] private bool _registerAsPrimaryPlayer = true;

    [Header("Identity")]
    [SerializeField] private string _characterName;

    private Knockback _knockback;
    private EquipmentComponent _equipment;
    [SerializeField] private Slider _healthBar;
    
    public event EventHandler OnPlayerDeath;
    public event EventHandler OnFlashBlink;

    [Header("Base stats")]
    [SerializeField] private float _baseHealth = 100f;
    [SerializeField] private float _baseAttack = 10f;
    [SerializeField] private float _baseDefence;
    [SerializeField] private float _baseCritChance = 5f;
    [SerializeField] private float _baseCritDamage = 50f;
    [SerializeField] private float _baseResistance;
    [SerializeField] private float _baseSizeMod;
    [SerializeField] private float _baseManaFlat = 100;
    [SerializeField] private float _baseManaRegenMod;
    [SerializeField] private float _baseSpellDamageMod;
    [SerializeField] private float _baseBaseAttackDamageMod;
    [SerializeField] private float _baseBaseAttackSpeedMod;
    [SerializeField] private float _baseCooldownReduction;
    [SerializeField] private float _baseLuck;
    [SerializeField] private float _baseSpeed = 3f;

    [SerializeField] private float _baseManaRegenPercent = 0.05f;
    public string CharacterName =>
        string.IsNullOrWhiteSpace(_characterName) ? gameObject.name : _characterName;

    public bool IsPrimaryPlayer => _registerAsPrimaryPlayer;

    // ICharacterEntity
    public StatSystem StatSystem => _equipment.GetStatSystem();
    public bool IsAlive { get; private set; } = true;
    public Transform Transform => transform;

    public float Health { get; private set; }
    public float Mana {  get; private set; }
    public bool Invulnerability { get; set; }

    public float MaxHealth =>               StatSystem.GetFinalValue(StatType.HealthFlat);
    public float Attack =>                  StatSystem.GetFinalValue(StatType.AttackFlat);
    public float Defence =>                 StatSystem.GetFinalValue(StatType.DefenceFlat);
    public float CritChance =>              StatSystem.GetFinalValue(StatType.CritChance);
    public float CritDamage =>              StatSystem.GetFinalValue(StatType.CritDamage);
    public float Resistance =>              StatSystem.GetFinalValue(StatType.Resistance);
    public float SizeMod =>                 StatSystem.GetFinalValue(StatType.SizeMod);
    public float MaxMana =>                 StatSystem.GetFinalValue(StatType.ManaFlat);
    public float ManaRegenMod =>            StatSystem.GetFinalValue(StatType.ManaRegenMod);
    public float SpellDamageMod =>          StatSystem.GetFinalValue(StatType.SpellDamageMod);
    public float BaseAttackDamageMod =>     StatSystem.GetFinalValue(StatType.BaseAttackDamageMod);
    public float CooldownReduction =>       StatSystem.GetFinalValue(StatType.CooldownReduction);
    public float AttackSpeed =>             StatSystem.GetFinalValue(StatType.BaseAttackSpeedMod);
    public float Luck =>                    StatSystem.GetFinalValue(StatType.Luck);
    public float Speed =>                   StatSystem.GetFinalValue(StatType.Speed);


    private BoxCollider2D _collisionCollider;
    private CircleCollider2D _hitboxCollider;

    [Header("Knockback settings")]
    public float KnockbackResist;
    public float KnockbackMultiplier = 1;


    private void Awake()
    {
        if (_registerAsPrimaryPlayer)
        {
            if (Instance == null) Instance = this;
            else
            {
                Debug.LogError($"[PlayerStats] Duplicate primary player on {gameObject.name}. Destroying.");
                Destroy(gameObject);
                return;
            }
        }

        _knockback          = GetComponent<Knockback>();
        _equipment          = GetComponent<EquipmentComponent>();
        _collisionCollider  = GetComponent<BoxCollider2D>();
        _hitboxCollider     = GetComponent<CircleCollider2D>();

        if (_equipment == null)
            Debug.LogError($"[PlayerStats] EquipmentComponent missing on {gameObject.name}");
    }
    private void OnEnable()
    {
        TryRegisterWithParty();
    }

    private void OnDisable()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.Unregister(this);
    }

    private void Start()
    {
        TryRegisterWithParty();

        InitializeBaseStats();
        StatSystem.OnStatsChanged += OnStatsChanged;
        Health = MaxHealth;
        UpdateUI();
    }

    private void Update()
    {
        RegenerateMana();
    }

    public void TakeDamage(float rawDamage, Vector3 knockbackSource, float knockbackMultiplier)
    {
        if (!IsAlive || Invulnerability) return;

        float damage = Mathf.Max(0f, rawDamage - Defence);
        damage *= Mathf.Max(0f, 1f - Resistance/100);
        Health -= damage;
        Health = Mathf.Max(0f, Health);

        OnFlashBlink?.Invoke(this, EventArgs.Empty);

        if (Health <= 0f) Die();
        else _knockback.GetKnockedBack(knockbackSource, knockbackMultiplier, 0f);
        UpdateUI();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        Health = Mathf.Min(Health + amount, MaxHealth);
        UpdateUI();
    }

    private void InitializeBaseStats()
    {
        _equipment.SetBaseStats(new (StatType, float)[]
        {
        (StatType.HealthFlat,         _baseHealth),
        (StatType.AttackFlat,         _baseAttack),
        (StatType.DefenceFlat,        _baseDefence),
        (StatType.BaseAttackSpeedMod, _baseBaseAttackSpeedMod),
        (StatType.CritChance,         _baseCritChance),
        (StatType.CritDamage,         _baseCritDamage),
        (StatType.Resistance,         _baseResistance),
        (StatType.SizeMod,            _baseSizeMod),
        (StatType.ManaFlat,           _baseManaFlat),
        (StatType.ManaRegenMod,       _baseManaRegenMod),
        (StatType.SpellDamageMod,     _baseSpellDamageMod),
        (StatType.BaseAttackDamageMod,_baseBaseAttackDamageMod),
        (StatType.CooldownReduction,  _baseCooldownReduction),
        (StatType.Luck,               _baseLuck),
        (StatType.Speed,              _baseSpeed),
        });
    }

    private void OnStatsChanged()
    {
        Health = Mathf.Min(Health, MaxHealth);
        UpdateUI();
    }

    private void Die()
    {
        IsAlive = false;
        _hitboxCollider.enabled = false;
        _collisionCollider.enabled = false;
        OnPlayerDeath?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateUI()
    {
        _healthBar.maxValue = MaxHealth;
        _healthBar.value = Health;
    }

    private void RegenerateMana()
    {
        if (Mana >= MaxMana) return;

        float regenPerSecond = MaxMana * _baseManaRegenPercent * (1 + ManaRegenMod);
        Mana += regenPerSecond * Time.deltaTime;
        Mana = Mathf.Min(Mana, MaxMana);
    }

    private void TryRegisterWithParty()
    {
        if (PartyManager.Instance != null)
            PartyManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (StatSystem != null)
            StatSystem.OnStatsChanged -= OnStatsChanged;
    }
}
