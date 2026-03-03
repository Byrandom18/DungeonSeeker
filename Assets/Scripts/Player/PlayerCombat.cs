using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private float _attackCooldown;
    private bool _canAttack = true;

    private void Start()
    {
        GameInput.Instance.OnPlayerAttack += GameInput_OnPlayerAttack;
    }

    private void GameInput_OnPlayerAttack(object sender, System.EventArgs e)
    {
        if (_canAttack && PlayerStats.Instance.IsAlive)
        {
            ActiveWeapon.Instance.GetActiveWeapon().Attack();
            _attackCooldown += ActiveWeapon.Instance.GetActiveWeapon().Cooldown;
            StartCoroutine(AttackCD());
        }
        
    }

    private IEnumerator AttackCD()
    {
        _canAttack = false;
        // some weapons needs for freeze rotation on animation
        ActiveWeapon.Instance.RotationEnabled = ActiveWeapon.Instance.GetActiveWeapon().RotationEnabled;
        yield return new WaitForSeconds(_attackCooldown);
        ActiveWeapon.Instance.RotationEnabled = true;
        _attackCooldown = 0;
        _canAttack = true;
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnPlayerAttack -= GameInput_OnPlayerAttack;
    }
}
