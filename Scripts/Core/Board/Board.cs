using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class Board : IBoard, IBoardModelController
{
    private Dictionary<HexaCoords, IBlock> _blocks;
    private readonly MapData _mapData;
    private int _idCounter = 0;

    private IMatchFinder _matchFinder;
    private IBoardGenerator _boardGenerator;
    private IBoardAnalyzer _analyzer;
    private IMatchProcessor _matchProcessor;

    public event Action<IBlock, IBlock, Action> OnBlocksSwapped;
    public event Action<List<IBlock>> OnMatchFound;
    public event Action<IBlock, IBlock, Action> OnSwapFailed;
    public event Action<List<BlockMoveInfo>, Action> OnBlocksDropped;
    public event Action<List<IBlock>, Action> OnNewBlocksGenerated;
    public event Action<IBlock, Action> OnSpecialBlockCreated;
    public event Action OnBoardUpdateComplete;
    public event Action<HexaCoords> OnJackActivated;
    public event Action<Dictionary<HexaCoords, IBlock>, Action> OnBoardRegenerated;
    public event Action OnMatchSuccess;

    private bool _isProcessingMove = false;

    public Board(MapData mapData)
    {
        _mapData = mapData;
        _blocks = new Dictionary<HexaCoords, IBlock>();
    }

    public void Init(IMatchFinder matchFinder, IBoardGenerator boardGenerator, IBoardAnalyzer analyzer, IMatchProcessor matchProcessor)
    {
        _matchFinder = matchFinder;
        _boardGenerator = boardGenerator;
        _analyzer = analyzer;
        _matchProcessor = matchProcessor;

        _matchProcessor.OnMatchFound += HandleMatchFound;
        _matchProcessor.OnJackActivated += HandleJackActivated;
        _matchProcessor.OnSpecialBlockCreated += HandleSpecialBlockCreated;
        _matchProcessor.OnBlocksDropped += HandleBlocksDropped;
        _matchProcessor.OnNewBlocksGenerated += HandleNewBlocksGenerated;
    }

    public void UnsubscribeAllEvents()
    {
        OnBlocksSwapped = null;
        OnMatchFound = null;
        OnSwapFailed = null;
        OnBlocksDropped = null;
        OnNewBlocksGenerated = null;
        OnSpecialBlockCreated = null;
        OnBoardUpdateComplete = null;
        OnJackActivated = null;
        OnBoardRegenerated = null;
        OnMatchSuccess = null;

        if (_matchProcessor != null)
        {
            _matchProcessor.OnMatchFound -= HandleMatchFound;
            _matchProcessor.OnJackActivated -= HandleJackActivated;
            _matchProcessor.OnSpecialBlockCreated -= HandleSpecialBlockCreated;
            _matchProcessor.OnBlocksDropped -= HandleBlocksDropped;
            _matchProcessor.OnNewBlocksGenerated -= HandleNewBlocksGenerated;
        }
    }

    // 중계자 목록들
    private void HandleMatchFound(List<IBlock> matches)
    {
        OnMatchFound?.Invoke(matches);
    }

    private void HandleJackActivated(HexaCoords coords)
    {
        OnJackActivated?.Invoke(coords);
    }

    private void HandleSpecialBlockCreated(IBlock block, Action callback)
    {
        OnSpecialBlockCreated?.Invoke(block, callback);
    }

    private void HandleBlocksDropped(List<BlockMoveInfo> moves, Action callback)
    {
        OnBlocksDropped?.Invoke(moves, callback);
    }

    private void HandleNewBlocksGenerated(List<IBlock> blocks, Action callback)
    {
        OnNewBlocksGenerated?.Invoke(blocks, callback);
    }

    public void FillInitBoard() // 초기 보드 생성 (시작 매치 없고 최소 1개 이동가능 보장)
    {
        int safetyBreak = 10;
        while (safetyBreak-- > 0)
        {

            _blocks = _boardGenerator.GenerateInitBlocks(out _idCounter);
            _boardGenerator.ValidateAndFixBoard(ref _blocks, this);

            if (_analyzer.HasAvailableMoves())
                break;
        }
    }

    public async Task<bool> TrySwapBlocksAsync(HexaCoords coordsA, HexaCoords coordsB)  // 두 블럭 위치 변경 시도
    {
        if (_isProcessingMove) // 이미 이동이 동작중이라면 취소
            return false;
        _isProcessingMove = true;

        IBlock blockA = GetBlockAt(coordsA);
        IBlock blockB = GetBlockAt(coordsB);

        // 유효성 검사
        if (blockA == null || blockB == null || blockA == blockB || IsObstacleAt(coordsA) || IsObstacleAt(coordsB))
        {
            _isProcessingMove = false;
            OnBoardUpdateComplete?.Invoke();
            return false;
        }

        SwapModelData(blockA, blockB);  // 먼저 데이터 스왑

        var swapAnimationTCS = new TaskCompletionSource<bool>();
        OnBlocksSwapped?.Invoke(blockA, blockB, () => swapAnimationTCS.TrySetResult(true)); // 스왑 애니메이션 대기
        await swapAnimationTCS.Task;


        List<MatchGroup> matches = _matchFinder.FindMatchesAfterSwap(blockA, blockB);   // 매치 확인 

        if (matches.Count > 0)  // 매치가 하나라도 있다면 스왑
        {
            OnMatchSuccess?.Invoke();


            await _matchProcessor.ProcessMatchesAsync(matches, blockA, blockB); // 연쇄반응 초ㅓ리
            await CheckForDeadlockAndShuffleAsync();    // 더이상 이동할게 없다면 셔플 실시 
            OnBoardUpdateComplete?.Invoke();
            return true;
        }
        else                    // 매치가 없다면 되돌리기
        {
            SwapModelData(blockA, blockB);  // 다시 모델 되돌리기 
            var swapBackAnimationTCS = new TaskCompletionSource<bool>();
            OnSwapFailed?.Invoke(blockA, blockB, () => swapBackAnimationTCS.TrySetResult(true)); //되돌리기 애니메이션 수행 및 대기
            await swapBackAnimationTCS.Task;

            _isProcessingMove = false;
            OnBoardUpdateComplete?.Invoke();

            return false;
        }
    }

    private void SwapModelData(IBlock blockA, IBlock blockB)
    {
        HexaCoords coordsA = blockA.Coords;
        HexaCoords coordsB = blockB.Coords;
        _blocks[coordsA] = blockB;
        _blocks[coordsB] = blockA;
        blockA.Coords = coordsB;
        blockB.Coords = coordsA;
    }


    private async Task CheckForDeadlockAndShuffleAsync()    // 교착상태 확인 및 셔플
    {
        try
        {
            if (!HasAvailableMoves())   // 움직일수 없다면 셔플 시작
            {
                int safetyBreak = 10;
                while (!HasAvailableMoves() && safetyBreak-- > 0)   // 교착상태가 없을때까지 셔플시작
                {

                    _blocks = _boardGenerator.GenerateInitBlocks(out _idCounter);   // 새 보드 생성
                    _boardGenerator.ValidateAndFixBoard(ref _blocks, this);

                    var regenerateTCS = new TaskCompletionSource<bool>();
                    OnBoardRegenerated?.Invoke(GetBlockData(), () => regenerateTCS.TrySetResult(true)); // 재생성 보드 뷰 반영 대기
                    await regenerateTCS.Task;


                    List<MatchGroup> chainedMatches = _matchFinder.FindAllMatchesOnBoard();
                    if (chainedMatches.Count > 0)   // 셔플 직후 매칭 확인
                    {
                        await _matchProcessor.ProcessMatchesAsync(chainedMatches);  // 연쇄반응 재처리
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"에러 발생 : {ex.Message}");
        }
        finally
        {
            _isProcessingMove = false;  // 작업완료
        }
    }

    public List<IBlock> GetHint()
    {
        return _analyzer.GetHint();
    }

    public bool HasAvailableMoves()
    {
        return _analyzer.HasAvailableMoves();
    }

    public bool IsObstacleAt(HexaCoords coords)
    {
        if (_mapData?.jackPositions == null)
            return false;
        return _mapData.jackPositions.Any(jackPos => jackPos.q == coords.Q && jackPos.r == coords.R);
    }

    public Dictionary<HexaCoords, IBlock> GetBlockData()
    {
        return new Dictionary<HexaCoords, IBlock>(_blocks);
    }

    public IBlock GetBlockAt(HexaCoords coords)
    {
        _blocks.TryGetValue(coords, out IBlock block);
        return block;
    }

    public bool IsProcessing() => _isProcessingMove;

    Dictionary<HexaCoords, IBlock> IBoardModelController.Blocks => _blocks;
    int IBoardModelController.GetNewBlockID() => _idCounter++;
    void IBoardModelController.SwapModelData(IBlock blockA, IBlock blockB) => SwapModelData(blockA, blockB);
}