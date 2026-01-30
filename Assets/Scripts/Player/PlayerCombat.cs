using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    private float attackCooldown;
    private bool canAttack = true;

    private void Start()
    {
        GameInput.Instance.OnPlayerAttack += GameInput_OnPlayerAttack;
    }

    private void GameInput_OnPlayerAttack(object sender, System.EventArgs e)
    {
        if (canAttack)
        {
            ActiveWeapon.Instance.GetActiveWeapon().Attack();
            attackCooldown += ActiveWeapon.Instance.GetActiveWeapon().cooldown;
            StartCoroutine(AttackCD());
        }
        
    }

    private IEnumerator AttackCD()
    {
        canAttack = false;
        // some weapons needs for freeze rotation on animation
        ActiveWeapon.Instance.rotationEnabled = ActiveWeapon.Instance.GetActiveWeapon().rotationEnabled;
        yield return new WaitForSeconds(attackCooldown);
        ActiveWeapon.Instance.rotationEnabled = true;
        attackCooldown = 0;
        canAttack = true;
    }
}
