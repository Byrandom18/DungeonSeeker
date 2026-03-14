using UnityEngine;

public class ActiveWeapon : MonoBehaviour
{
    public bool RotationEnabled = true;


    public static ActiveWeapon Instance { get; private set; }
    [SerializeField] private WeaponBase[] _weapons;
    private int _currentIndex = 0;

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < _weapons.Length; i++)
            _weapons[i].gameObject.SetActive(i == _currentIndex);
    }

    private void Update()
    {
        FollowMousePosition();
        HandleWeaponSwitch();
    }

    public WeaponBase GetActiveWeapon()
    {
        return _weapons[_currentIndex];
    }

    public void SetWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Length) return;
        _weapons[_currentIndex].gameObject.SetActive(false);
        _currentIndex = index;
        _weapons[_currentIndex].gameObject.SetActive(true);
    }

    private void HandleWeaponSwitch()
    {
        // 1, 2, 3...
        for (int i = 0; i < _weapons.Length; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                SetWeapon(i);

        //// Или колесо мыши
        //float scroll = Input.GetAxis("Mouse ScrollWheel");
        //if (scroll > 0f) SetWeapon((_currentIndex + 1) % _weapons.Length);
        //if (scroll < 0f) SetWeapon((_currentIndex - 1 + _weapons.Length) % _weapons.Length);
    }

    private void FollowMousePosition()
    {
        Vector3 mousePos = GameInput.Instance.GetMousePosition();
        Vector3 playerPosition = PlayerMovement.Instance.GetPlayerScreenPosition();

        Vector3 direction = mousePos - playerPosition;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (RotationEnabled) //changing in PlayerCombat
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        
    }
}
