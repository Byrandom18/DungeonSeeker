using UnityEngine;

public class SwordVisual : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private Sword _sword;
    [SerializeField] private TrailRenderer _trailRenderer;
    private const string ATTACK = "Attack"; //animator trigger
    

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        
    }

    private void Start()
    {
        CheckAllComponents();

        _sword.OnSwordSwing += Sword_OnSwordSwing;
    }


    public void StartSwing()
    {
        _trailRenderer.emitting = true;
        _sword.StartAttack();

    }
    public void StopSwing()
    {
        _trailRenderer.emitting = false;
        _sword.EndAttack();
    }


    private void CheckAllComponents()
    {
        CheckComponent(_animator, "Animator");
        CheckComponent(_trailRenderer, "TrailRenderer");
        CheckComponent(_sword, "Sword");
    }

    private void CheckComponent<T>(T component, string componentName) where T : Component
    {
        if (component == null)
            Debug.LogError($"{componentName} is missing on {gameObject.name}");
    }

    private void Sword_OnSwordSwing(object sender, System.EventArgs e)
    {
        _animator.SetTrigger(ATTACK);
        StartSwing();
    }

    
}
