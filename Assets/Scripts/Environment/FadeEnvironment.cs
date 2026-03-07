using System.Collections;
using UnityEngine;

public class FadeEnvironment : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _fadeTime = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float _transparencyAmount = 0.6f;
    private float _originTransparencyAmount = 1f;
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision is BoxCollider2D)
        {
            StartCoroutine(FadeRoutine(_spriteRenderer.color.a, _transparencyAmount));
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && collision is BoxCollider2D)
        {
            StartCoroutine(FadeRoutine(_spriteRenderer.color.a, _originTransparencyAmount));
        }
    }

    private IEnumerator FadeRoutine(float startTransparencyAmount, float targetTransparencyAmount)
    {
        float elapsedTime = 0f;
        while (elapsedTime < _fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startTransparencyAmount, targetTransparencyAmount, elapsedTime / _fadeTime);
            _spriteRenderer.color = new Color(_spriteRenderer.color.r, _spriteRenderer.color.g, _spriteRenderer.color.b, newAlpha);
            yield return null;
        }
    }
}
