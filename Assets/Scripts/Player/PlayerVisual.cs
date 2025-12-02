using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Vector2 originScale;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        originScale = transform.localScale;
    }

    public void UpdateSpriteDirection(bool flipRight)
    {
        if (!flipRight)
        {
            sprite.transform.localScale = new Vector3(-originScale.x, originScale.y, 1);
        }
        else if (flipRight)
        {
            sprite.transform.localScale = new Vector3(originScale.x, originScale.y, 1);
        }
    }
}
