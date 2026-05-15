using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private ActiveWeapon _activeWeapon;

    private float _attackCooldown;
    private bool _canAttack = true;

    private void Awake()
    {
        if (_playerStats == null)
            _playerStats = GetComponent<PlayerStats>();
        if (_activeWeapon == null)
            _activeWeapon = GetComponentInChildren<ActiveWeapon>(true);
    }

    private void Start()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnPlayerAttack += OnPlayerAttack;
    }

    private void OnPlayerAttack(object sender, System.EventArgs e)
    {
        if (!_canAttack || _playerStats == null || !_playerStats.IsAlive) return;
        if (_activeWeapon == null) return;

        WeaponBase weapon = _activeWeapon.GetActiveWeapon();
        if (weapon == null) return;

        weapon.Attack();
        _attackCooldown = weapon.Cooldown;

        StartCoroutine(AttackCooldownRoutine(weapon));
    }

    private IEnumerator AttackCooldownRoutine(WeaponBase weapon)
    {
        _canAttack = false;
        if (GameInput.Instance != null)
            GameInput.Instance.CanAttack = false;

        bool lockRotation = weapon.WeaponData != null && weapon.WeaponData.LockRotationOnSwing;
        if (lockRotation)
            _activeWeapon.RotationEnabled = false;

        _activeWeapon.NotifyAttackStarted();

        yield return new WaitForSeconds(_attackCooldown);

        if (lockRotation)
            _activeWeapon.RotationEnabled = true;

        _attackCooldown = 0;
        _canAttack = true;
        if (GameInput.Instance != null)
            GameInput.Instance.CanAttack = true;

        _activeWeapon.NotifyAttackEnded();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnPlayerAttack -= OnPlayerAttack;
    }
}
