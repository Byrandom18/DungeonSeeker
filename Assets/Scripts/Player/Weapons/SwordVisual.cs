using UnityEngine;

public class SwordVisual : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private Sword sword;
    [SerializeField] private TrailRenderer trailRenderer;
    private const string ATTACK = "Attack"; //animator trigger
    

    private void Awake()
    {
        animator = GetComponent<Animator>();
        
    }

    private void Start()
    {
        CheckAllComponents();

        sword.OnSwordSwing += Sword_OnSwordSwing;
    }


    public void StartSwing()
    {
        trailRenderer.emitting = true;
        sword.StartAttack();

    }
    public void StopSwing()
    {
        trailRenderer.emitting = false;
        sword.EndAttack();
    }


    private void CheckAllComponents()
    {
        CheckComponent(animator, "Animator");
        CheckComponent(trailRenderer, "TrailRenderer");
        CheckComponent(sword, "Sword");
    }

    private void CheckComponent<T>(T component, string componentName) where T : Component
    {
        if (component == null)
            Debug.LogError($"{componentName} is missing on {gameObject.name}");
    }

    private void Sword_OnSwordSwing(object sender, System.EventArgs e)
    {
        animator.SetTrigger(ATTACK);
        StartSwing();
    }

    
}
