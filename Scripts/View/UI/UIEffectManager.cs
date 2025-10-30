using UnityEngine;
using System.Collections;

public class UIEffectManager
{
    private readonly GameObject _jackEffectPrefab;  // 잭 활성화 FX 
    private readonly GameObject _flyingJackPrefab;  // 날아갈 잭 오브젝트
    private readonly RectTransform _targetGoalIcon; // 목표 지점
    private readonly HexaCoordinateConverter _converter;
    private readonly Transform _parentTransform;
    private readonly MonoBehaviour _coroutineRunner;
    private readonly IGameManager _gameManager;

    private readonly float _goalIconBounceScale;
    private readonly float _goalIconBounceDuration;
    private Coroutine _bounceIconCoroutine;

    private const float Flying_DURATION = 0.7f;

    public UIEffectManager(GameObject jackEffectPrefab, GameObject flyingJackPrefab, RectTransform targetGoalIcon,
                 HexaCoordinateConverter converter, Transform parentTransform, MonoBehaviour coroutineRunner,
                 float goalIconBounceScale, float goalIconBounceDuration, IGameManager gameManager)
    {
        _jackEffectPrefab = jackEffectPrefab;
        _flyingJackPrefab = flyingJackPrefab;
        _targetGoalIcon = targetGoalIcon;
        _converter = converter;
        _parentTransform = parentTransform;
        _coroutineRunner = coroutineRunner;
        _goalIconBounceScale = goalIconBounceScale;
        _goalIconBounceDuration = goalIconBounceDuration;
        _gameManager = gameManager;
    }

    public void Subscribe(IBoardEvents boardEvents)
    {
        boardEvents.OnJackActivated += HandleJackActivated;
    }

    public void Unsubscribe(IBoardEvents boardEvents)
    {
        if (boardEvents != null)
            boardEvents.OnJackActivated -= HandleJackActivated;
    }

    public void StopAllCoroutines()
    {
        if (_bounceIconCoroutine != null)
        {
            _coroutineRunner.StopCoroutine(_bounceIconCoroutine);
            _bounceIconCoroutine = null;
        }
    }

    private void HandleJackActivated(HexaCoords jackCoords)
    {
        _gameManager.NotifyJackAnimationStarted();

        Vector3 originalStartPos = _converter.GetWorldPosition(jackCoords.Q, jackCoords.R);
        if (_jackEffectPrefab != null)
        {
            Vector3 explosionPos = originalStartPos;
            explosionPos.z = -2f;
            GameObject effectInstance = ObjectPooler.Instance.Spawn(_jackEffectPrefab, explosionPos, Quaternion.identity, _parentTransform);

            AutoDestroyEffect autoDestroy = effectInstance.GetComponent<AutoDestroyEffect>();
            if (autoDestroy != null)    // FX 파괴시 날아가는 이미지 함수 콜백
            {
                autoDestroy.SetOnFinishedCallback((effectPos) => {
                    Vector3 flyingStartPos = new Vector3(effectPos.x, effectPos.y, originalStartPos.z);
                    _coroutineRunner.StartCoroutine(FlyJackToGoalCoroutine(flyingStartPos));
                });
            }
            else
                _coroutineRunner.StartCoroutine(FlyJackToGoalCoroutine(originalStartPos));
        }
        else
            _coroutineRunner.StartCoroutine(FlyJackToGoalCoroutine(originalStartPos));
    }

    private IEnumerator FlyJackToGoalCoroutine(Vector3 startPos)    // 목표 지점으로 날아가는 잭 이미지
    {
        if (_flyingJackPrefab == null || _targetGoalIcon == null)
        {
            _gameManager.CollectGoalObject(0);
            yield break;
        }

        Vector3[] corners = new Vector3[4]; // 모서리를 통해 중앙 좌표 (목적지 찾기)
        _targetGoalIcon.GetWorldCorners(corners);
        Vector3 targetPos = (corners[0] + corners[2]) / 2f;

        GameObject flyingJack = ObjectPooler.Instance.Spawn( _flyingJackPrefab, startPos, Quaternion.identity, _parentTransform);

        float timer = 0f;

        while (timer < Flying_DURATION) // 날아가는 효과
        {
            if (flyingJack == null) yield break;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / Flying_DURATION);
            float easedT = 1 - (1 - t) * (1 - t);
            flyingJack.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        if (flyingJack != null)
        {
            flyingJack.transform.position = targetPos;
            ObjectPooler.Instance.Despawn(flyingJack);
        }

        if (_bounceIconCoroutine != null)
            _coroutineRunner.StopCoroutine(_bounceIconCoroutine);

        _bounceIconCoroutine = _coroutineRunner.StartCoroutine(BounceGoalIconCoroutine()); // 잭 도착시 효과 실행

        _gameManager.CollectGoalObject(0);
    }

    private IEnumerator BounceGoalIconCoroutine()   // 점수 획득시 UI가 커졌다 작아지는 효과
    {
        if (_targetGoalIcon == null || _goalIconBounceDuration <= 0f)
        {
            _bounceIconCoroutine = null;
            yield break;
        }

        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * _goalIconBounceScale;
        float halfDuration = _goalIconBounceDuration / 2f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            float easedT = 1 - (1 - t) * (1 - t);
            if (_targetGoalIcon == null)
                yield break;
            _targetGoalIcon.localScale = Vector3.LerpUnclamped(originalScale, targetScale, easedT);
            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDuration);
            float easedT = 1 - (1 - t) * (1 - t);
            if (_targetGoalIcon == null)
                yield break;
            _targetGoalIcon.localScale = Vector3.LerpUnclamped(targetScale, originalScale, easedT);
            yield return null;
        }

        if (_targetGoalIcon != null)
        {
            _targetGoalIcon.localScale = originalScale;
        }
        _bounceIconCoroutine = null;
    }
}