using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HintVisualizer
{
    private readonly GameObject _hintBorderPrefab;
    private readonly float _hintPulseDuration;
    private readonly HexaCoordinateConverter _converter;
    private readonly Transform _parentTransform;
    private readonly MonoBehaviour _coroutineRunner;

    private IBoardReader _boardReader;
    private List<GameObject> _activeHintBorders = new List<GameObject>();
    private Coroutine _hintCoroutine;

    public HintVisualizer(GameObject hintBorderPrefab, float hintPulseDuration, HexaCoordinateConverter converter, Transform parentTransform, MonoBehaviour coroutineRunner)
    {
        _hintBorderPrefab = hintBorderPrefab;
        _hintPulseDuration = hintPulseDuration;
        _converter = converter;
        _parentTransform = parentTransform;
        _coroutineRunner = coroutineRunner;
    }

    public void Init(IBoardReader boardReader)
    {
        _boardReader = boardReader;
    }

    public void ShowHint()  // 힌트 활성화
    {
        if (_boardReader == null || _hintBorderPrefab == null)
            return;

        StopHint();

        List<IBlock> hintBlocks = _boardReader.GetHint();

        if (hintBlocks == null || hintBlocks.Count == 0)    // 힌트가 없다면 중지
            return;

        foreach (IBlock block in hintBlocks)
        {
            if (block == null) continue;
            Vector3 pos = _converter.GetWorldPosition(block.Coords.Q, block.Coords.R);
            pos.z = -1.5f;

            GameObject border = ObjectPooler.Instance.Spawn(_hintBorderPrefab, pos, Quaternion.identity, _parentTransform);
            border.name = $"Hint_{block.Coords.Q}_{block.Coords.R}";
            _activeHintBorders.Add(border);
        }

        if (_hintPulseDuration > 0)
            _hintCoroutine = _coroutineRunner.StartCoroutine(PulseHintCoroutine()); // 힌트가 생성되었다면 반짝이는 이펙트 코루틴 시작
    }

    public void StopHint()  // 힌트 비 활성화
    {
        if (_hintCoroutine != null)
        {
            _coroutineRunner.StopCoroutine(_hintCoroutine);
            _hintCoroutine = null;
        }

        if (_activeHintBorders.Count > 0)
        {
            foreach (GameObject border in _activeHintBorders)
            {
                if (border != null)
                    ObjectPooler.Instance.Despawn(border);
            }
            _activeHintBorders.Clear();
        }
    }

    private IEnumerator PulseHintCoroutine()    // 힌트가 반짝이는 효과
    {
        while (true)
        {
            float timer = 0f;
            while (timer < _hintPulseDuration / 2f)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / (_hintPulseDuration / 2f));
                float alpha = Mathf.Lerp(0.2f, 1.0f, t);
                SetHintAlphas(alpha);
                yield return null;
            }
            timer = 0f;
            while (timer < _hintPulseDuration / 2f)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / (_hintPulseDuration / 2f));
                float alpha = Mathf.Lerp(1.0f, 0.2f, t);
                SetHintAlphas(alpha);
                yield return null;
            }
        }
    }
    private void SetHintAlphas(float alpha)
    {
        foreach (GameObject border in _activeHintBorders)
        {
            if (border == null) continue;
            SpriteRenderer sr = border.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
}