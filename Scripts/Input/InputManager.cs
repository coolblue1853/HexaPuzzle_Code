using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{
    private IBoardActions _board;
    private IBoardReader _boardReader;
    private IGrid _grid;
    private BoardManager _boardManager;
    private Camera _mainCamera;
    private IGameManager _gameManager;
    private HintVisualizer _hintVisualizer;

    private BlockView _selectedBlock = null;    // 클릭한 블럭
    private Vector2 _startMousePos;             // 마우스 클릭 좌표
    private float _minDragDistance = 50f;

    [SerializeField] private LayerMask blockLayerMask;  // 블럭 마스크 (레이캐스팅 최적화)

    private float _noMoveTimer = 0f;// 힌트 노출을 위한 타이머
    public float hintDelay = 5.0f;  // 힌트 대기시간
    private bool _isHintActive = false;

    public void Init(IBoardActions board, IBoardReader boardReader, IGrid grid, BoardManager boardManager, IGameManager gameManager, HintVisualizer hintVisualizer)
    {
        _board = board;
        _boardReader = boardReader;
        _grid = grid;
        _boardManager = boardManager;
        _mainCamera = Camera.main;
        _gameManager = gameManager;
        _hintVisualizer = hintVisualizer;
    }

    void Update()
    {
        // 안전체크
        if (_board == null || _boardReader == null || _gameManager == null || _boardManager == null || _hintVisualizer == null)
            return;

        // 보드작업이 없, 힌트가 없, 게임도 안끝났다면 힌트 타이머 동작
        if (!_board.IsProcessing() && !_isHintActive && !_gameManager.IsGameEnded())
        {
            _noMoveTimer += Time.deltaTime;
            if (_noMoveTimer >= hintDelay)
            {
                _isHintActive = true;
                _noMoveTimer = 0f;
                _hintVisualizer.ShowHint();
            }
        }

        // 보드가 작업중이거나 게임이 끝났다면 타이머 초기화 및 힌트 멈춤
        if (_board.IsProcessing() || _gameManager.IsGameEnded())
        {
            _noMoveTimer = 0f;
            if (_isHintActive)
            {
                _hintVisualizer.StopHint();
                _isHintActive = false;
            }
            return;
        }

        // 클릭 인풋이 들어오면
        if (Input.GetMouseButtonDown(0))
        {
            _noMoveTimer = 0f;  // 힌트 타이머 초기화 
            if (_isHintActive)  // 힌트 숨기기 
            {
                _hintVisualizer.StopHint();
                _isHintActive = false;
            }

            _selectedBlock = GetBlockUnderMouse();  // 클릭한 블럭 반환
            if (_selectedBlock != null)
                _startMousePos = Input.mousePosition;
        }
        
        // 클릭 인풋이 끝나면
        if (Input.GetMouseButtonUp(0))
        {
            _noMoveTimer = 0f;  // 타이머 재시작
            if (_isHintActive)
            {
                _hintVisualizer.StopHint();
                _isHintActive = false;
            }

            if (_selectedBlock == null)
                return;

            // 때어질때의 지점 기점으로 방향 계산
            Vector2 endMousePos = Input.mousePosition;
            Vector2 dragVector = endMousePos - _startMousePos;

            if (dragVector.magnitude > _minDragDistance)
            {
                int direction = GetDirectionFromVector(dragVector);
                if (direction != -1)   
                {
                    HexaCoords startCoords = _selectedBlock.GetCoords();    // 시작 블럭 정보 가져오기
                    IBlock startBlockModel = _selectedBlock.GetBlockModel();
                    if (startBlockModel == null)
                    {
                        _selectedBlock = null;
                        return;
                    }

                    HexaCoords targetCoords = _grid.GetNeighbor(startCoords, direction);    // 목표 이웃 블럭 정보 가져오기
                    IBlock targetBlockModel = _boardReader.GetBlockAt(targetCoords);

                    bool canSwap = targetBlockModel != null && !_boardReader.IsObstacleAt(targetCoords);    // 스왑이 가능한지 확인

                    if (canSwap)    // 스왑이 가능하다면 스왑실시
                        HandleSwapAsync(startCoords, targetCoords);
                }
            }
            _selectedBlock = null;
        }
    }

    private async void HandleSwapAsync(HexaCoords startCoords, HexaCoords targetCoords)
    {
        if (_board == null || _board.IsProcessing())
            return;

        try
        {
            await _board.TrySwapBlocksAsync(startCoords, targetCoords);
        }
        catch (Exception ex)
        {
            Debug.LogError($"에러 발생 : {ex.Message}");
        }
    }

    private BlockView GetBlockUnderMouse()  // 마우스밑의 블럭 반환
    {
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldPos.x, worldPos.y), Vector2.zero, 0f, blockLayerMask);
        if (hit.collider != null)
            return hit.collider.GetComponent<BlockView>();
        return null;
    }

    private int GetDirectionFromVector(Vector2 dragVector)  // 6방향으로 나눠서 스왑할 방향 반환
    {
        // 그리드 방향 결정
        float angle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
        if (angle > 0f && angle <= 60f) return 0;
        if (angle > 60f && angle <= 120f) return 5;
        if (angle > 120f && angle <= 180f) return 4;
        if (angle > -180f && angle <= -120f) return 3;
        if (angle > -120f && angle <= -60f) return 2;
        if (angle > -60f && angle <= 0f) return 1;
        return -1;
    }
}