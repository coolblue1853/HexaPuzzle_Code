using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BoardViewAnimator  // 보드의 시각 처리 담당
{
    // 블럭 프리팹 목록
    private readonly List<GameObject> _blockPrefabs;
    private readonly List<GameObject> _effectPrefabs;
    private readonly List<GameObject> _linePrefabs;
    private readonly HexaCoordinateConverter _converter;
    private readonly Transform _parentTransform;

    private Dictionary<HexaCoords, GameObject> _blockGameObjects;
    private IBoardReader _boardReader;

    private const float SWAP_DURATION = 0.2f;   // 스왑 시간
    private const float DROP_DURATION = 0.3f;   // 낙하 시간
    private const float REFILL_DURATION = 0.3f; // 리필 시간
    private const float REFILL_SPAWN_OFFSET_Y = 5f; // 리필 생성시 Y축 오프셋

    //  애니메이션이 모두 종료되었을때 이벤트를 호출하도록하는 그룹
    private class AnimationGroup
    {
        private readonly Action _onAllComplete;
        private int _counter;
        private bool _isFinalized;

        public AnimationGroup(Action onAllCompleteCallback) // 모두 호출이 완료되면 onAllCompleteCallback 실시
        {
            _onAllComplete = onAllCompleteCallback;
            _counter = 0;
            _isFinalized = false;
        }

        public Action GetCallbackToken()    // 토큰 발행
        {
            _counter++;
            return () => {  // 토큰 회수및 모두 종료 체크
                _counter--;
                CheckCompletion();
            };
        }

        public void Complete()  // 모든 애니메이션 실행했음을 알림
        {
            _isFinalized = true;
            CheckCompletion();
        }

        private void CheckCompletion()  // 모든 애니메이션이 종료 상태인지 체크
        {
            if (_isFinalized && _counter <= 0)
                _onAllComplete?.Invoke();
        }
    }

    public BoardViewAnimator(List<GameObject> blockPrefabs, List<GameObject> lineBlasterPrefabs, List<GameObject> effectPrefabs, HexaCoordinateConverter converter, Transform parentTransform)
    {
        _blockPrefabs = blockPrefabs;
        _linePrefabs = lineBlasterPrefabs;
        _effectPrefabs = effectPrefabs;
        _converter = converter;
        _parentTransform = parentTransform;
        _blockGameObjects = new Dictionary<HexaCoords, GameObject>();
    }

    public void Subscribe(IBoardEvents boardEvents, IBoardReader boardReader)   // 이벤트 구독
    {
        _boardReader = boardReader;
        boardEvents.OnBlocksSwapped += HandleBlocksSwapped;
        boardEvents.OnMatchFound += HandleMatchFound;
        boardEvents.OnSwapFailed += HandleSwapFailed;
        boardEvents.OnBlocksDropped += HandleBlocksDropped;
        boardEvents.OnNewBlocksGenerated += HandleNewBlocksGenerated;
        boardEvents.OnSpecialBlockCreated += HandleSpecialBlockCreated;
        boardEvents.OnBoardRegenerated += HandleBoardRegenerated;
    }

    public void Unsubscribe(IBoardEvents boardEvents)   // 이벤트 해제
    {
        if (boardEvents == null)
            return;
        boardEvents.OnBlocksSwapped -= HandleBlocksSwapped;
        boardEvents.OnMatchFound -= HandleMatchFound;
        boardEvents.OnSwapFailed -= HandleSwapFailed;
        boardEvents.OnBlocksDropped -= HandleBlocksDropped;
        boardEvents.OnNewBlocksGenerated -= HandleNewBlocksGenerated;
        boardEvents.OnSpecialBlockCreated -= HandleSpecialBlockCreated;
        boardEvents.OnBoardRegenerated -= HandleBoardRegenerated;
    }

    public void Cleanup()   // 보드 위 모든 블럭 제거 및 초기화
    {
        foreach (GameObject obj in _blockGameObjects.Values)
        {
            if (obj != null)
                ObjectPooler.Instance.Despawn(obj);
        }
        _blockGameObjects.Clear();
    }

    public void FillInitBlocks() // 초기 블럭 데이터로 실제 생성 및 배치 
    {
        Dictionary<HexaCoords, IBlock> blockData = _boardReader.GetBlockData();
        _blockGameObjects.Clear();
        foreach (var pair in blockData) // 모든 블럭 순회 
        {
            HexaCoords coords = pair.Key;
            IBlock blockModel = pair.Value;
            Vector3 worldPos = _converter.GetWorldPosition(coords.Q, coords.R);
            worldPos.z = -1f;   // 타일 위에 위치하도록

            if (blockModel.ColorType < 0 || blockModel.ColorType >= _blockPrefabs.Count)    // 색상 유효성 검사
                continue;
            GameObject prefabToUse = _blockPrefabs[blockModel.ColorType];
            if (prefabToUse == null)
                continue;

            GameObject blockInstance = ObjectPooler.Instance.Spawn(prefabToUse, worldPos, Quaternion.identity, _parentTransform);

            blockInstance.name = $"Block_{coords.Q}_{coords.R} (Color {blockModel.ColorType})";
            _blockGameObjects[coords] = blockInstance;

            BlockView blockView = blockInstance.GetComponent<BlockView>();
            if (blockView != null)
                blockView.Init(blockModel);
        }
    }

    public GameObject GetBlockObjectAt(HexaCoords coords)
    {
        _blockGameObjects.TryGetValue(coords, out GameObject blockInstance);
        return blockInstance;
    }

    private void HandleBlocksSwapped(IBlock blockA, IBlock blockB, Action onModelContinue)  // 블럭 스왑 이벤트처리
    {
        var animGroup = new AnimationGroup(onModelContinue);
        HexaCoords oldCoordsA = blockB.Coords;   // 교체 되는 두 원본 블럭
        HexaCoords oldCoordsB = blockA.Coords;
        if (!_blockGameObjects.TryGetValue(oldCoordsA, out GameObject objA) || objA == null)
        {
            animGroup.Complete();
            return;
        }
        if (!_blockGameObjects.TryGetValue(oldCoordsB, out GameObject objB) || objB == null)
        {
            animGroup.Complete();
            return;
        }

        _blockGameObjects[oldCoordsB] = objA;   // 딕셔너리 업데이트
        _blockGameObjects[oldCoordsA] = objB;

        Vector3 newWorldPosA = _converter.GetWorldPosition(oldCoordsB.Q, oldCoordsB.R); // 각각의 월드 좌표 계산
        Vector3 newWorldPosB = _converter.GetWorldPosition(oldCoordsA.Q, oldCoordsA.R);
        newWorldPosA.z = -1f;
        newWorldPosB.z = -1f;

        BlockMover moverA = objA.GetComponent<BlockMover>();
        BlockMover moverB = objB.GetComponent<BlockMover>();

        // 애니메이션 시작
        moverA.MoveTo(newWorldPosA, SWAP_DURATION, animGroup.GetCallbackToken());  
        moverB.MoveTo(newWorldPosB, SWAP_DURATION, animGroup.GetCallbackToken());
        animGroup.Complete();
    }

    private void HandleMatchFound(List<IBlock> matchedBlocks)   // 매치 블럭 제거 및 폭발 이펙트
    {
        List<GameObject> objectsToDespawn = new List<GameObject>();
        List<HexaCoords> coordsToRemove = new List<HexaCoords>();

        foreach (var blockModel in matchedBlocks)   // 사라질 블럭 전체 순회
        {
            if (blockModel == null)
                continue;

            if (_blockGameObjects.TryGetValue(blockModel.Coords, out GameObject objToRemove) && objToRemove != null)
            {
                Vector3 effectPosition = objToRemove.transform.position;    // 폭발 위치 계산
                effectPosition.z = -2f;
                int colorType = blockModel.ColorType;
                GameObject effectPrefab = null;
                if (_effectPrefabs != null && colorType >= 0 && colorType < _effectPrefabs.Count)
                {
                    effectPrefab = _effectPrefabs[colorType];
                }

                if (effectPrefab != null)   // 폭발 효과 생성
                {
                    ObjectPooler.Instance.Spawn(effectPrefab, effectPosition, Quaternion.identity, _parentTransform);
                }

                objectsToDespawn.Add(objToRemove);  // 폭발 효과 반환
                coordsToRemove.Add(blockModel.Coords);
            }
        }

        // 딕셔너리에서 좌표 항목 제거 및 오브젝트 반환
        foreach (var coords in coordsToRemove)
            _blockGameObjects.Remove(coords);
        foreach (var obj in objectsToDespawn)
        {
            if (obj != null)
                ObjectPooler.Instance.Despawn(obj);
        }
    }

    private void HandleSwapFailed(IBlock blockA, IBlock blockB, Action onModelContinue)
    {
        HandleBlocksSwapped(blockA, blockB, onModelContinue);
    }

    private void HandleBlocksDropped(List<BlockMoveInfo> moveInfos, Action onModelContinue) // 블럭 낙하 애니메이션
    {
        var animGroup = new AnimationGroup(onModelContinue);
        if (moveInfos == null || moveInfos.Count == 0)
        {
            animGroup.Complete(); return;
        }

        var movingObjects = new Dictionary<HexaCoords, GameObject>();   // 움직일 전체 블럭 목록
        foreach (var info in moveInfos)
        {
            if (info.Block == null)
                continue;
            if (_blockGameObjects.TryGetValue(info.From, out GameObject objToMove) && objToMove != null)    // 원래 좌표의 오브젝트 찾기
            {
                movingObjects[info.From] = objToMove;
            }
        }
        foreach (var fromCoords in movingObjects.Keys)  // 원래 좌표 항목 삭제
        {
            _blockGameObjects.Remove(fromCoords);
        }
        // 모든 이동 정보 재 순회
        foreach (var info in moveInfos)
        {
            if (!movingObjects.TryGetValue(info.From, out GameObject objToMove))
                continue;

            _blockGameObjects[info.To] = objToMove; // 새 좌표로 등록
            Vector3 targetPos = _converter.GetWorldPosition(info.To.Q, info.To.R);  // 목표 월드좌표 계산
            targetPos.z = -1f;

            BlockView blockView = objToMove.GetComponent<BlockView>();  // 뷰의 데이터 업데이트
            if (blockView != null)
                blockView.Init(info.Block);

            BlockMover mover = objToMove.GetComponent<BlockMover>();    // 애니메이션 시작
            mover.MoveTo(targetPos, DROP_DURATION, animGroup.GetCallbackToken());
        }
        animGroup.Complete();
    }

    private void HandleNewBlocksGenerated(List<IBlock> newBlocks, Action onModelContinue)   // 새로운 블럭 생성 (리필)
    {
        var animGroup = new AnimationGroup(onModelContinue);
        if (newBlocks == null || newBlocks.Count == 0)
        {
            animGroup.Complete(); return;
        }

        foreach (var newBlockModel in newBlocks)    // 새블럭 목록 순회
        {
            if (newBlockModel == null)      // 데이터가 없으면 건너 뜀
                continue;
            if (_blockGameObjects.ContainsKey(newBlockModel.Coords))    // 이미 오브젝트가 있다면 건너 뜀
                continue;
            if (newBlockModel.ColorType < 0 || newBlockModel.ColorType >= _blockPrefabs.Count)  // 색상이 유효하지 않으면 건너 뜀
                continue;

            GameObject prefabToUse = _blockPrefabs[newBlockModel.ColorType];
            if (prefabToUse == null)
                continue;

            Vector3 endPos = _converter.GetWorldPosition(newBlockModel.Coords.Q, newBlockModel.Coords.R);
            endPos.z = -1f;
            Vector3 startPos = _converter.GetWorldPosition(newBlockModel.Coords.Q, 0) + Vector3.up * REFILL_SPAWN_OFFSET_Y;

            GameObject blockInstance = ObjectPooler.Instance.Spawn(prefabToUse, startPos, Quaternion.identity, _parentTransform);   // 블럭 프리팹 생성
            blockInstance.name = $"Block_{newBlockModel.Coords.Q}_{newBlockModel.Coords.R} (New Color {newBlockModel.ColorType})";

            // 새 블럭을 딕셔너리에 등록
            _blockGameObjects[newBlockModel.Coords] = blockInstance;
            BlockView blockView = blockInstance.GetComponent<BlockView>();
            if (blockView != null)
            {
                blockView.Init(newBlockModel);
            }
            // 애니메이션 실행
            BlockMover mover = blockInstance.GetComponent<BlockMover>();
            mover.MoveTo(endPos, REFILL_DURATION, animGroup.GetCallbackToken());
        }
        animGroup.Complete();
    }

    private void HandleSpecialBlockCreated(IBlock specialBlock, Action onModelContinue) // 특수 블럭 생성
    {
        // 프리팹 유효 검사
        if (specialBlock.ColorType < 0 || specialBlock.ColorType >= _linePrefabs.Count || _linePrefabs[specialBlock.ColorType] == null)
        {
            onModelContinue?.Invoke();
            return;
        }

        GameObject prefab = _linePrefabs[specialBlock.ColorType];
        Vector3 pos = _converter.GetWorldPosition(specialBlock.Coords.Q, specialBlock.Coords.R);
        pos.z = -1f;
        float angle = 0;
        switch (specialBlock.Direction) // 라인에 따라 각도 설정 (1자형)
        {
            case 0: angle = 30f; break;
            case 1: angle = 150f; break;
            case 2: angle = 90f; break;
        }
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        if (_blockGameObjects.TryGetValue(specialBlock.Coords, out GameObject oldObj) && oldObj != null)    // 생설될 자리의 기본 오브젝트 확인
        {
            ObjectPooler.Instance.Despawn(oldObj);
        }

        GameObject newSpecialBlcok = ObjectPooler.Instance.Spawn(prefab, pos, rotation, _parentTransform); ;
        newSpecialBlcok.name = $"Line_{specialBlock.Coords.Q}_{specialBlock.Coords.R}";

        // 뷰에 반영
        BlockView blockView = newSpecialBlcok.GetComponent<BlockView>();
        if (blockView != null)
            blockView.Init(specialBlock);

        _blockGameObjects[specialBlock.Coords] = newSpecialBlcok;

        onModelContinue?.Invoke();
    }

    private void HandleBoardRegenerated(Dictionary<HexaCoords, IBlock> newBlockData, Action onModelContinue)    // 보드 완전재생성 (셔플)
    {
        Cleanup();  // 블럭 전체 제거

        var animGroup = new AnimationGroup(onModelContinue);
        if (newBlockData == null)
        {
            animGroup.Complete();
            return;
        }

        // 위부터 아래로, 왼쪽부터 오른쪽으로 정렬 
        var newBlocks = newBlockData.Values
            .OrderBy(b => b.Coords.R)
            .ThenBy(b => b.Coords.Q)
            .ToList();

        foreach (var newBlockModel in newBlocks)    // 모든 새 블록 순회
        {
            GameObject prefabToUse;
            Quaternion rotation = Quaternion.identity;
            Vector3 endPos = _converter.GetWorldPosition(newBlockModel.Coords.Q, newBlockModel.Coords.R);
            endPos.z = -1f;

            if (newBlockModel.Type == BlockType.Line) // 특수 블럭인 경우  
            {
                if (newBlockModel.ColorType < 0 || newBlockModel.ColorType >= _linePrefabs.Count)
                    continue;
                prefabToUse = _linePrefabs[newBlockModel.ColorType];
                float angle = 0;
                switch (newBlockModel.Direction)
                {
                    case 0: angle = 30f; break;
                    case 1: angle = 150f; break;
                    case 2: angle = 90f; break;
                }
                rotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                if (newBlockModel.ColorType < 0 || newBlockModel.ColorType >= _blockPrefabs.Count)
                    continue;
                prefabToUse = _blockPrefabs[newBlockModel.ColorType];
            }

            if (prefabToUse == null)
                continue;

            // 새 블럭의 시작 위치 계산
            Vector3 startPos = _converter.GetWorldPosition(newBlockModel.Coords.Q, 0) + Vector3.up * REFILL_SPAWN_OFFSET_Y;

            GameObject blockInstance = ObjectPooler.Instance.Spawn(prefabToUse, startPos, rotation, _parentTransform);
            blockInstance.name = $"Block_{newBlockModel.Coords.Q}_{newBlockModel.Coords.R} (Regen C{newBlockModel.ColorType} T:{newBlockModel.Type})";
            _blockGameObjects[newBlockModel.Coords] = blockInstance;

            BlockView blockView = blockInstance.GetComponent<BlockView>();
            if (blockView != null) blockView.Init(newBlockModel);
            // 애니메이션 실행
            BlockMover mover = blockInstance.GetComponent<BlockMover>();
            mover.MoveTo(endPos, REFILL_DURATION, animGroup.GetCallbackToken());
        }

        animGroup.Complete();
    }
}