using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System;

public class MatchProcessor : IMatchProcessor
{
    private readonly IBoardReader _boardReader;
    private readonly IBoardModelController _modelController;
    private readonly IGrid _grid;
    private readonly IMatchFinder _matchFinder;
    private readonly IBoardModifier _boardModifier;

    public event Action<List<IBlock>> OnMatchFound;
    public event Action<HexaCoords> OnJackActivated;
    public event Action<IBlock, Action> OnSpecialBlockCreated;
    public event Action<List<BlockMoveInfo>, Action> OnBlocksDropped;
    public event Action<List<IBlock>, Action> OnNewBlocksGenerated;

    private const int MATCH_DELAY_MS = 200; // 지연시간
    private readonly (int, int)[] _lineDirections = new (int, int)[] { (0, 3), (1, 4), (2, 5) };

    public MatchProcessor(IBoardReader boardReader, IBoardModelController modelController, IGrid grid, IMatchFinder matchFinder, IBoardModifier boardModifier)
    {
        _boardReader = boardReader;
        _modelController = modelController;
        _grid = grid;
        _matchFinder = matchFinder;
        _boardModifier = boardModifier;
    }

    public async Task ProcessMatchesAsync(List<MatchGroup> matches, IBlock swappedBlockA = null, IBlock swappedBlockB = null)
    {
        var totalBlocksToRemove = new HashSet<IBlock>();    // 최종적으로 제거되는 블럭 
        var activationQueue = new Queue<IBlock>();          // 발동 대기중인 특수 블럭 
        var processedSpecials = new HashSet<IBlock>();      // 이미 큐에 넣었거나 발동된 특수블럭 
        IBlock specialBlockToCreate = null;                 // 이번턴에 새로 생성할 특수 블럭 
        IBlock blockToReplaceForSpecial = null;             // 새 특수 블럭이 생성될 자리에 있던 기존 블럭

        // 매치 그룹 분석
        var blockToMatchMap = new Dictionary<IBlock, List<MatchGroup>>();   // 각 블럭이 어느 매치그룹인지 확인
        foreach (var group in matches)
        {
            foreach (var block in group.Blocks)
            {
                if (!blockToMatchMap.ContainsKey(block))
                    blockToMatchMap[block] = new List<MatchGroup>();
                blockToMatchMap[block].Add(group);
            }
        }

        var processedGroups = new HashSet<MatchGroup>();            // 이미 처리된 그룹 
        foreach (var (block, competingGroups) in blockToMatchMap)   // 블록별 최고 그룹 선택
        {
            if (competingGroups.Count == 0 || processedGroups.Contains(competingGroups[0]))
                continue;

            MatchGroup bestGroup;
            if (competingGroups.Count == 1)
                bestGroup = competingGroups[0];
            else
            {
                bestGroup = competingGroups[0];
                for (int i = 1; i < competingGroups.Count; i++)
                {
                    MatchGroup currentGroup = competingGroups[i];
                    if (currentGroup.Count > bestGroup.Count)   // 크기가 큰 순서
                        bestGroup = currentGroup;
                    else if (currentGroup.Count == bestGroup.Count && currentGroup.Type == MatchType.Cluster && bestGroup.Type == MatchType.Line)   // 같다면 클러스터 우선
                        bestGroup = currentGroup;
                }
            }

            foreach (var b in bestGroup.Blocks) // 최고(가장 많은 수의 블럭) 그룹을 제거 목록 및 발동 큐에 추가
            {
                if (totalBlocksToRemove.Add(b)) // 추가 성공시 처음 추가된 블럭 
                {
                    if (b.Type == BlockType.Line && processedSpecials.Add(b))
                        activationQueue.Enqueue(b);
                }
            }

            if (specialBlockToCreate == null && bestGroup.Type == MatchType.Line && bestGroup.Count >= 4)   // 새 특수블럭 생성 조건
            {
                if (swappedBlockA != null && bestGroup.Blocks.Contains(swappedBlockA)) // 직접 움직인 자리에 특수블럭 생성 ㅇㅊ우선 
                    blockToReplaceForSpecial = swappedBlockA;
                else if (swappedBlockB != null && bestGroup.Blocks.Contains(swappedBlockB))
                    blockToReplaceForSpecial = swappedBlockB;
                else
                    blockToReplaceForSpecial = bestGroup.Blocks[bestGroup.Blocks.Count / 2]; // 연쇄 반응으로 생겼다면 중간에 생성

                if (blockToReplaceForSpecial != null)   // 특수블럭 정보 생성
                    specialBlockToCreate = new Block(_modelController.GetNewBlockID(), blockToReplaceForSpecial.ColorType, blockToReplaceForSpecial.Coords, BlockType.Line, bestGroup.Direction);
            }

            foreach (var group in competingGroups)
                processedGroups.Add(group);
        }

        // 특수블럭이 생성된 자리의 블럭은 제거 목록에서 제외 
        if (specialBlockToCreate != null && blockToReplaceForSpecial != null)   
        {
            totalBlocksToRemove.Remove(blockToReplaceForSpecial);
        }


        // 특수 블럭 연쇄 발동 처리
        int cascadeDepth = 0;   // 무한 루프 방지 
        while (activationQueue.Count > 0 && cascadeDepth < 100)
        {
            cascadeDepth++;
            IBlock currentSpecial = activationQueue.Dequeue();  // 큐에서 특수 블럭을 꺼내기 

            List<IBlock> blastedBlocks = GetBlocksInLine(currentSpecial.Coords, currentSpecial.Direction);  // 파괴할 특수 블럭 목록 

            foreach (var blastedBlock in blastedBlocks)
            {
                if (totalBlocksToRemove.Add(blastedBlock))  // 추가 시도 -> 성공시 처음 제거되는 블럭 
                {

                    if (blastedBlock.Type == BlockType.Line && processedSpecials.Add(blastedBlock)) // 터지는게 또다시 특수 블럭이라면 
                    {
                        activationQueue.Enqueue(blastedBlock); // 추가 
                    }

                    if (blastedBlock == blockToReplaceForSpecial)   // 터지는 자리가 새 특수 블럭 생성 자리라면 -> 생성 취소
                    {
                        specialBlockToCreate = null;
                        blockToReplaceForSpecial = null;
                    }
                }
            }
        }

        // 변경사항 적용
        if (totalBlocksToRemove.Count > 0 || specialBlockToCreate != null)  // 제거 블럭이 있거, 특수 블럭 생성이 있다면
        {
            var activatedJacks = new HashSet<HexaCoords>();
            foreach (var block in totalBlocksToRemove)      // 잭 발동 이벤트 발생
            {
                for (int i = 0; i < 6; i++)
                {
                    HexaCoords neighborCoords = _grid.GetNeighbor(block.Coords, i);
                    if (_boardReader.IsObstacleAt(neighborCoords))
                        activatedJacks.Add(neighborCoords);
                }
            }
            if (activatedJacks.Count > 0)
            {
                foreach (var jackCoords in activatedJacks)
                    OnJackActivated?.Invoke(jackCoords);
            }

            if (specialBlockToCreate != null)       // 새 특수 블럭 생성
            {
                if (_modelController.Blocks.ContainsKey(specialBlockToCreate.Coords) && _modelController.Blocks[specialBlockToCreate.Coords] != blockToReplaceForSpecial)
                {
                    Debug.LogError($"특수 블럭 생성 오류 발생");
                }
                else
                {
                    _modelController.Blocks[specialBlockToCreate.Coords] = specialBlockToCreate;    // 데이터 추가
                    var tcs = new TaskCompletionSource<bool>(); 
                    OnSpecialBlockCreated?.Invoke(specialBlockToCreate, () => tcs.TrySetResult(true)); // 특수블럭 생성 및 이벤트 발생
                    await tcs.Task;
                }
            }

            List<IBlock> blocksToRemoveList = totalBlocksToRemove.ToList(); // 최종 제거 목록 실제 제거 및 이벤트 발생
            if (blocksToRemoveList.Count > 0)
            {
                foreach (var block in blocksToRemoveList)
                    _modelController.Blocks.Remove(block.Coords);
                OnMatchFound?.Invoke(blocksToRemoveList);   // 제거 이벤트 발생
                await Task.Delay(MATCH_DELAY_MS);
            }
        }

        // 중력 적용 및 애니메이션 대기
        List<BlockMoveInfo> droppedBlockInfo = _boardModifier.ApplyGravity();
        if (droppedBlockInfo.Count > 0)
        {
            var dropTCS = new TaskCompletionSource<bool>();
            OnBlocksDropped?.Invoke(droppedBlockInfo, () => dropTCS.TrySetResult(true));
            await dropTCS.Task;
        }

        // 리필 적용 및 애니메이션 대기
        List<IBlock> newBlocks = _boardModifier.RefillBoard();
        if (newBlocks.Count > 0)
        {
            var refillTCS = new TaskCompletionSource<bool>();
            OnNewBlocksGenerated?.Invoke(newBlocks, () => refillTCS.TrySetResult(true));
            await refillTCS.Task;
        }

        // 연쇄반응 확인 및 재귀
        List<MatchGroup> chainedMatches = _matchFinder.FindAllMatchesOnBoard();
        if (chainedMatches.Count > 0)
        {
            await ProcessMatchesAsync(chainedMatches);
        }
    }

    private List<IBlock> GetBlocksInLine(HexaCoords coords, int directionIndex) // 특수블럭 - 라인파괴할때의 모든 블럭 반환 
    {
        var blocks = new List<IBlock>();
        if (directionIndex < 0 || directionIndex >= _lineDirections.Length) // 유효 방향 확인 
        {
            return blocks;
        }

        (int dirA, int dirB) = _lineDirections[directionIndex];

        HexaCoords currentCoords = coords;  // 탐색 시작 위치 
        while (true)    // A방향으로 더이상 이동하지 못할때까지 
        {
            currentCoords = _grid.GetNeighbor(currentCoords, dirA);
            if (!_grid.IsValidCoordinate(currentCoords.Q, currentCoords.R) || _grid.GetTileStatus(currentCoords.Q, currentCoords.R) != 1 || _boardReader.IsObstacleAt(currentCoords))
                break;
            IBlock block = _boardReader.GetBlockAt(currentCoords);
            if (block != null)
                blocks.Add(block);
        }
        currentCoords = coords;
        while (true)    // B방향으로 더이상 이동하지 못할때까지
        {
            currentCoords = _grid.GetNeighbor(currentCoords, dirB);
            if (!_grid.IsValidCoordinate(currentCoords.Q, currentCoords.R) || _grid.GetTileStatus(currentCoords.Q, currentCoords.R) != 1 || _boardReader.IsObstacleAt(currentCoords))
                break;
            IBlock block = _boardReader.GetBlockAt(currentCoords);
            if (block != null)
                blocks.Add(block);
        }
        return blocks;
    }
}