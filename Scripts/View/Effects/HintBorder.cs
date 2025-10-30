using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HintBorder : MonoBehaviour, IPoolable 
{
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;

    // 오리지널 컬러로 초기화
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _originalColor = _spriteRenderer.color;
    }

    public void OnSpawn()
    {
        
        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;
    }

    public void OnDespawn()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;
    }
}