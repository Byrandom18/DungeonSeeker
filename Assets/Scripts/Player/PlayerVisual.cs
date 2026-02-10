using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private SpriteRenderer _sprite;
    private Vector2 _originScale;
    private Animator _animator;

    private const string IS_MOVING = "IsMoving";


    void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _originScale = transform.localScale;
        _animator = GetComponent<Animator>();
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
