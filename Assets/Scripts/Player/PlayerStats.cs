using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;
    public static event System.Action<Transform> OnPlayerSpawned;
    //public static event System.Action OnPlayerDeath;

    [Header("UI Elements")]
    public Slider healthBar;
    public Text levelText;
    public Text goldText;
    public Text gemsText;
    //public ExpBar expBar;

    [Header("Боевые характеристики")]
    public float health = 100;
    public float baseHealth = 100;
    public float healthMod;
    public float maxHealth;
    public float healthRegen;
    public float baseAtk = 10;
    public float atk = 1;
    public float atkMod;
    public float damageMod;
    public float luck;
    public float critRate = 5;
    public float critDamage = 50;
    public float def;
    public float speedMod;
    public int penetrationBoost;
    public float projectileSpeed;
    public float durations;
    public float cdRed;
    public int addProjectile;
    public float areaMod;
    public float defShred;
    public bool invulnerability = false;

    [Header("Вспомогательные характеристики")]
    public float maxExp = 5;
    public float exp;
    public float expIncrease = 10;
    public int lvl = 1;
    public float gold;
    public int gems;
    public int killCount;
    public int wavesCompleted;

    private bool isDead = false;

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

    private void InitializeStats()
    {
        atk = baseAtk * (1 + atkMod / 100);
        maxHealth = baseHealth * (1 + healthMod / 100);
        health = maxHealth;
    }
    public void TakeDamage(float damage)
    {
        if (isDead || invulnerability) return;

        damage -= def;
        //damage = Mathf.Max(1f, damage * (1 - def / (def + 100f))); // Формула уменьшения урона от защиты

        health -= damage;
        health = Mathf.Max(0, health);

        //UpdateHealthUI();
        //OnHealthChanged?.Invoke();

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

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
