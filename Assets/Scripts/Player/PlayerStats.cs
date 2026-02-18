using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;
    public static event System.Action<Transform> OnPlayerSpawned;
    //public static event System.Action OnPlayerDeath;

    //[Header("UI Elements")]
    //private Slider _healthBar;
    //private Text _levelText;
    //private Text _goldText;
    //private Text _gemsText;
    //public ExpBar expBar;

    [Header("Боевые характеристики")]
    public float Health = 100;
    public float BaseHealth = 100;
    public float HealthMod;
    public float MaxHealth;
    public float HealthRegen;
    public float BaseAtk = 10;
    public float CurrentAtk = 1;
    public float AtkMod;
    public float DamageMod;
    public float Luck;
    public float CritRate = 5;
    public float CritDamage = 50;
    public float Def;
    public float SpeedMod;
    public int PenetrationBoost;
    public float ProjectileSpeed;
    public float Durations;
    public float CdRed;
    public int AddProjectile;
    public float AreaMod;
    public float DefShred;
    public bool Invulnerability = false;

    //[Header("Вспомогательные характеристики")]
    //public float maxExp = 5;
    //public float exp;
    //public float expIncrease = 10;
    //public int lvl = 1;
    //public float gold;
    //public int gems;
    //public int killCount;
    //public int wavesCompleted;

    public bool IsAlive = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("PlayerStats can not assign the Instance on " + gameObject.name);
            return;
        }
    }

    private void Start()
    {
        InitializeStats();
        //StartCoroutines();
        //UpdateAllUI();

        OnPlayerSpawned?.Invoke(transform);
    }


    public void TakeDamage(float damage)
    {
        if (!IsAlive || Invulnerability) return;

        damage -= Def;
        //damage = Mathf.Max(1f, damage * (1 - def / (def + 100f))); // Формула уменьшения урона от защиты

        Health -= damage;
        Health = Mathf.Max(0, Health);

        //UpdateHealthUI();
        //OnHealthChanged?.Invoke();

        if (Health <= 0 && IsAlive)
        {
            Die();
        }
    }


    private void InitializeStats()
    {
        CurrentAtk = BaseAtk * (1 + AtkMod / 100);
        MaxHealth = BaseHealth * (1 + HealthMod / 100);
        Health = MaxHealth;
    }
    

    private void Die()
    {
        IsAlive = false;

        //// Останавливаем все корутины
        //if (healthRegenCoroutine != null) StopCoroutine(healthRegenCoroutine);
        //if (difficultyCoroutine != null) StopCoroutine(difficultyCoroutine);
        //if (survivalTimerCoroutine != null) StopCoroutine(survivalTimerCoroutine);

        //// Вызываем событие смерти
        //OnDeath?.Invoke();
        //OnPlayerDeath?.Invoke(); // <- НОВОЕ событие

        //// Показываем экран смерти
        //if (GameManager.Instance != null)
        //{
        //    GameManager.Instance.ShowDeathScreen();
        //}
    }
}
