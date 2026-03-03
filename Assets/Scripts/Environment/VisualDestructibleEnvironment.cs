using UnityEngine;

public class VisualDestructibleEnvironment : MonoBehaviour
{
    [SerializeField] private DestructibleEnvironment _environment;
    [SerializeField] private GameObject _deathVFXPrefab;

    private void Start()
    {
        _environment.OnDestroyEnvironment += DestructibleEnvironment_OnDestroyEnvironment;
    }

    private void DestructibleEnvironment_OnDestroyEnvironment(object sender, System.EventArgs e)
    {
        ShowDeathVFX();
    }

    private void ShowDeathVFX()
    {
        Instantiate(_deathVFXPrefab, _environment.transform.position, Quaternion.identity);
    }

    private void OnDestroy()
    {
        _environment.OnDestroyEnvironment -= DestructibleEnvironment_OnDestroyEnvironment;
    }
}
