using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private float _attackCooldown;
    private bool _canAttack = true;

    private void Start()
    {
        GameInput.Instance.OnPlayerAttack += OnPlayerAttack;
    }

    private void OnPlayerAttack(object sender, System.EventArgs e)
    {
        if (!_canAttack || !PlayerStats.Instance.IsAlive) return;

        WeaponBase weapon = ActiveWeapon.Instance.GetActiveWeapon();

        weapon.Attack();
        _attackCooldown = weapon.Cooldown;

        StartCoroutine(AttackCooldownRoutine(weapon));
    }

    private IEnumerator AttackCooldownRoutine(WeaponBase weapon)
    {
        _canAttack = false;
        GameInput.Instance.CanAttack = false;

        bool lockRotation = weapon.WeaponData != null && weapon.WeaponData.LockRotationOnSwing;
        if (lockRotation)
            ActiveWeapon.Instance.RotationEnabled = false;

        ActiveWeapon.Instance.NotifyAttackStarted();

        yield return new WaitForSeconds(_attackCooldown);

        if (lockRotation)
            ActiveWeapon.Instance.RotationEnabled = true;

        _attackCooldown = 0;
        _canAttack = true;
        GameInput.Instance.CanAttack = true;

        ActiveWeapon.Instance.NotifyAttackEnded();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnPlayerAttack -= OnPlayerAttack;
    }
}
