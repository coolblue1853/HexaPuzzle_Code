using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public interface IBoard : IBoardReader, IBoardActions, IBoardEvents { }

public interface IBoardReader   // 읽기, 확인 기능 
{
    Dictionary<HexaCoords, IBlock> GetBlockData();  // 보드 위의 모든 블럭 정보 반환 
    IBlock GetBlockAt(HexaCoords hexaCoords);       // 특정 좌표에 어떤 블럭이 있는지 반환 
    bool IsObstacleAt(HexaCoords hexaCoords);      // 특정 좌표에 장애물이 있는지 반환 
    bool HasAvailableMoves();                       // 보드에 움직일 수 있는 매치가 하나라도 있는지 반환 
    List<IBlock> GetHint();                         // 힌트 목록 반환 
}

public interface IBoardActions // 보드 조작 및 상태 변경 
{
    bool IsProcessing();    // 지금 보드가 처리중인지 반환 
    void FillInitBoard();   // 처음 시작시 보드를 채우라는 명령 
    Task<bool> TrySwapBlocksAsync(HexaCoords coordsA, HexaCoords coordsB); // 두 블록 스왑시도 
}

public interface IBoardEvents // 결과 통보 (이벤트ㅎ)
{
    event Action<IBlock, Action> OnSpecialBlockCreated;         // 특수블럭 생성 안내 
    event Action<IBlock, IBlock, Action> OnBlocksSwapped;       // 블럭이 스왑 될떄 안내 
    event Action<List<IBlock>> OnMatchFound;                    // 매치되어 사라질때 목록 안내 
    event Action<IBlock, IBlock, Action> OnSwapFailed;          // 시왑 실패후 되돌아갈때 안내 
    event Action<List<BlockMoveInfo>, Action> OnBlocksDropped;  // 블럭이 중력에 의해 아래로 떨어졌을때 안내 
    event Action<List<IBlock>, Action> OnNewBlocksGenerated;    // 새 블럭이 생성될때 안내 
    event Action<Dictionary<HexaCoords, IBlock>, Action> OnBoardRegenerated;    // 힌트가 없어서 재배치될때 안내 
    event Action OnBoardUpdateComplete;                         // 모든 보드 작업이 끝난 최종 상태 안내 
    event Action<HexaCoords> OnJackActivated;                   // 잭인더 박스 활성화시 안내 
    event Action OnMatchSuccess;                                // 최초 매치 성공시 안내 ( 이동횟수 차감 )

    void UnsubscribeAllEvents();    // 모든 이벤트 정리 
}
