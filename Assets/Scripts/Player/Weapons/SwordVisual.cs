using UnityEngine;

public class SwordVisual : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private Sword sword;

    private const string ATTACK = "Attack"; //animator trigger

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        sword.OnSwordSwing += Sword_OnSwordSwing;
    }

    private void Sword_OnSwordSwing(object sender, System.EventArgs e)
    {
        animator.SetTrigger(ATTACK);
    }
}
