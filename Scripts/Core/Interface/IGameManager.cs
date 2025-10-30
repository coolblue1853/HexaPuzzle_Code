using System;

public interface IGameManager
{
    void StartGame();           // 게임시작 
    void EndGame(bool success); // 게임 종료 
    void SetGoalCount(int count);   // 목표물 설정 
    void CollectGoalObject(int type);   // 목표 수집 안내
    void InitGameManager(IBoard board, int targetCount, int moveCount); // 게임매니저 초기화 및 레벨 정보 재설정
    bool IsGameEnded(); // 게임이 종료 상태인지 체크
    void NotifyJackAnimationStarted();  // 잭 애니메이션이 시작되었는지 안내 -> 게임오버 보류

    event Action<int> OnGoalUpdated;        // 목표 개수 변동 안내
    event Action<int> OnMoveCountUpdated;   // 이동 횟수 변동 안내
}