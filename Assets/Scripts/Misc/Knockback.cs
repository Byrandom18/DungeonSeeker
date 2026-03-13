using UnityEngine;

public class Knockback : MonoBehaviour
{
    public float KnockbackForce = 2f;
    public float KnockbackMovingTimerMax = 0.3f;

    private float _knockbackMovingTimer;

    private Rigidbody2D _rb;

    public bool IsGettingKnockedback { get; private set; }


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        _knockbackMovingTimer -= Time.deltaTime;
        if (IsGettingKnockedback && _knockbackMovingTimer < 0)
            StopKnockBackMovement();
    }

    public void GetKnockedBack(Vector3 sourcePosition, float knockbackMultiplier, float resist)
    {
        if (resist > 100) resist = 100;
        float finalForce = KnockbackForce * knockbackMultiplier * (1 - resist / 100);
        IsGettingKnockedback = true;
        Vector3 selfPosition = transform.position;
        _knockbackMovingTimer = KnockbackMovingTimerMax;
        Vector2 difference = (selfPosition - sourcePosition).normalized * finalForce;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(difference, ForceMode2D.Impulse);
    }


    public void StopKnockBackMovement()
    {
        _rb.linearVelocity = Vector2.zero;
        IsGettingKnockedback = false;
    }
}
