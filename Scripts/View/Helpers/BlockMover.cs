using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(BlockView))]
public class BlockMover : MonoBehaviour, IPoolable
{
    private Coroutine _moveCoroutine;

    public void MoveTo(Vector3 targetPosition, float duration, Action onComplete = null) // 원하는 위치까지 블럭 이동
    {
        StopMove();
        _moveCoroutine = StartCoroutine(MoveCoroutine(targetPosition, duration, onComplete));
    }

    private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration,  Action onComplete)   // 실제 이동 코루틴
    {
        if (duration <= 0.01f)  // 이동 시간이 너무 짧다면 즉시 이동
        {
            transform.position = targetPosition;
            onComplete?.Invoke();
            _moveCoroutine = null;
            yield break;
        }

        Vector3 startPosition = transform.position;
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float easedT = t * t * (3f - 2f * t);   // 스무스스탭 보간
            transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            yield return null;
        }

        transform.position = targetPosition;
        onComplete?.Invoke();
        _moveCoroutine = null;
    }

    public void StopMove() // 이동 코루틴 징행중이라면 취소 
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }

    public void OnSpawn()
    {

    }

    public void OnDespawn() // 블럭이 중간에 사라지면 즉시 애니메이션종료
    {
        StopMove(); 
    }
}