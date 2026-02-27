using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;

    private const string IS_MOVING = "IsMoving";
    private const string DEATH = "Death";

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _originScale = transform.localScale;
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        PlayerStats.Instance.OnPlayerDeath += PlayerStats_OnPlayerDeath;
    }

    private void PlayerStats_OnPlayerDeath(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(DEATH);
    }

    private void Update()
    {
        _animator.SetBool(IS_MOVING, PlayerMovement.Instance.IsRunning());
    }

    public void UpdateSpriteDirection(bool flipRight)
    {
        if (!flipRight)
        {
            _sprite.transform.localScale = new Vector3(-_originScale.x, _originScale.y, 1);
        }
        else if (flipRight)
        {
            _sprite.transform.localScale = new Vector3(_originScale.x, _originScale.y, 1);
        }
    }


}
