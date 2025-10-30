using UnityEngine;
using System.Collections;
using System;

public class GameManager : MonoBehaviour, IGameManager
{
    public static GameManager Instance { get; private set; }

    private int _currentGoalCount;
    private int _targetGoalCount;
    private int _leftMoveCount;

    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private LevelInitializer levelInitializer;

    private IBoard _board;
    private bool _isGameEnded = false;
    private bool _pendingMoveDecrement = false; // 연쇄가 끝나기 전까지 이동횟수 차감 대기
    private int _pendingJackAnimations = 0;     // 잭이 이동중이라면 게임오버 대기

    public event Action<int> OnGoalUpdated;
    public event Action<int> OnMoveCountUpdated;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        if (levelInitializer == null)
            levelInitializer = FindObjectOfType<LevelInitializer>();

        if (gameClearPanel != null)
            gameClearPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_board != null)
        {
            _board.OnBoardUpdateComplete -= HandleBoardUpdateComplete;
            _board.OnMatchSuccess -= HandleMatchSuccess; 
        }
    }

    public bool IsGameEnded()
    {
        return _isGameEnded;
    }

    public void InitGameManager(IBoard board, int targetCount, int moveCount)   // 새로운 래밸 시작시 호출
    {
        // 이백트 중복 등록 방지
        if (_board != null)
        {
            _board.OnBoardUpdateComplete -= HandleBoardUpdateComplete;
            _board.OnMatchSuccess -= HandleMatchSuccess; 
        }
        _board = board;
        if (_board != null)
        {
            _board.OnBoardUpdateComplete += HandleBoardUpdateComplete;
            _board.OnMatchSuccess += HandleMatchSuccess;
        }

        // 목표 설정 및 게임매니저 초기화
        SetGoalCount(targetCount);
        SetMoveCount(moveCount);
        _isGameEnded = false;
        _pendingMoveDecrement = false;
        _pendingJackAnimations = 0;

        OnGoalUpdated?.Invoke(_targetGoalCount - _currentGoalCount);
        OnMoveCountUpdated?.Invoke(_leftMoveCount);
    }

    private void HandleMatchSuccess()   // 이동횟수 차감 준비 
    {
        if (_isGameEnded)
            return;
        _pendingMoveDecrement = true;
    }

    public void NotifyJackAnimationStarted()    // 잭 애니메이션 추적 
    {
        if (_isGameEnded)
            return;
        _pendingJackAnimations++;
    }

    private void HandleBoardUpdateComplete()    // 이동 횟수 차감 및 게임오버 판정
    {
        if (_pendingMoveDecrement)
        {
            _pendingMoveDecrement = false; 
            if (_leftMoveCount > 0) // 이동횟수가 0이상이면 차감
            {
                _leftMoveCount--; 
                OnMoveCountUpdated?.Invoke(_leftMoveCount);
            }
        }

        if (_isGameEnded)
            return;

        if (_leftMoveCount <= 0 && _pendingJackAnimations == 0) // 이동횟수가 0 이고 진행중인 애니메이션이 없는지 체크
            EndGame(false);
    }

    public void StartGame()
    {
        _isGameEnded = false;
    }

    public void EndGame(bool success)
    {
        if (_isGameEnded)
            return;
        _isGameEnded = true;

        if (success)    // 게임 종료 및 스테이지 성공시 
        {
            if (gameClearPanel != null)
                gameClearPanel.SetActive(true);
        }
        else            // 게임 종료 및 스테이지 실패시
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
    }

    public void SetGoalCount(int count)
    {
        _targetGoalCount = Mathf.Max(0, count);
        _currentGoalCount = 0;
    }
    public void SetMoveCount(int count)
    {
        _leftMoveCount = Mathf.Max(0, count);
    }

    public void CollectGoalObject(int type)
    {
        // 잭 카운터 감소
        if (_pendingJackAnimations > 0)
            _pendingJackAnimations--;

        if (_isGameEnded) return;

        _currentGoalCount++;
        int remainingGoals = Mathf.Max(0, _targetGoalCount - _currentGoalCount);
        OnGoalUpdated?.Invoke(remainingGoals);

        // 목표 달성 시 즉시 게임 클리어
        if (_currentGoalCount >= _targetGoalCount)
        {
            EndGame(true);
            return;
        }

        //  승리 못한 상태로, 마지막 잭이 도착했고, 이동 횟수도 0이면 게임 오버
        if (_pendingJackAnimations == 0 && _leftMoveCount <= 0)
        {
            EndGame(false);
        }
    }

    public void RetryGame()
    {
        if (gameClearPanel != null)
            gameClearPanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (levelInitializer != null)
            levelInitializer.RestartGame();
    }
}