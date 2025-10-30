using UnityEngine;
using System;

public class AutoDestroyEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private GameObject flyingPivot;    // 날아가기 시작하는 지점
    private Action<Vector3> _onEffectFinishedCallback;

    // 외부에서 도달시 콜백 등록
    public void SetOnFinishedCallback(Action<Vector3> callback)
    {
        _onEffectFinishedCallback = callback;
    }

    public void ResetCallback()
    {
        _onEffectFinishedCallback = null;
    }

    public void DestroyEffect() // 이펙트 재생이 끝났다면
    {
        _onEffectFinishedCallback?.Invoke(flyingPivot != null ? flyingPivot.transform.position : transform.position);
        ObjectPooler.Instance.Despawn(gameObject);
    }

    public void OnSpawn()
    {
        ResetCallback();
    }

    public void OnDespawn()
    {
        ResetCallback();
    }
}