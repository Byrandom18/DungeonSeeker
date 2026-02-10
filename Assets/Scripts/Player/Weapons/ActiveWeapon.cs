using UnityEngine;

public class ActiveWeapon : MonoBehaviour
{
    public bool RotationEnabled = true;


    public static ActiveWeapon Instance { get; private set; }
    [SerializeField] private Sword _sword;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        FollowMousePosition();
    }

    public Sword GetActiveWeapon()
    {
        return _sword;
    }

    private void FollowMousePosition()
    {
        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerPosition = PlayerMovement.Instance.GetPlayerScreenPosition();

        // Вычисляем направление от объекта к курсору
        Vector3 direction = mousePos - playerPosition;
        // Вычисляем угол в радианах и конвертируем в градусы
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Поворачиваем объект
        if (RotationEnabled) //changing in PlayerCombat
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        
    }
}
