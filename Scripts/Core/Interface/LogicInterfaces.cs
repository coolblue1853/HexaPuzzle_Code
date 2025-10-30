using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public interface IMatchFinder   // 매치되는 블록 그룹 찾기 
{
    List<MatchGroup> FindMatchesAfterSwap(IBlock swappedBlockA, IBlock swappedBlockB);  // 블럭 2개 스왑시 주변에서 생긴 매치 확인 
    List<MatchGroup> FindAllMatchesOnBoard();   // 보드 전체를 훑어서 매치를 확인 
}

public interface IBoardGenerator    // 보드를 최초로 만들고 수정 
{
    Dictionary<HexaCoords, IBlock> GenerateInitBlocks(out int idCounter);   // 최초의 보드를 채우는 블럭 생성 
    void ValidateAndFixBoard(ref Dictionary<HexaCoords, IBlock> blocks, IBoardReader boardView);    // 보드 생성 직후 매치가 있다면 없도록 수정 
    int GenerateValidColorForRefill(HexaCoords coords, IBoardReader boardView); // 리필시 내려오자마자 터지지 않도록 유효 색상 검증 
}

public interface IBoardModifier // 보드 정리 (이동, 리필)
{
    List<BlockMoveInfo> ApplyGravity(); // 블럭 중력 적용 
    List<IBlock> RefillBoard();         // 새 블럭 생성 이후 채워넣기
}

public interface IBoardAnalyzer // 보드 분석 
{
    List<IBlock> GetHint();     // 힌트 안내 
    bool HasAvailableMoves();   // 힌트가 하나라도 있는지 검사 
}

public interface IMatchProcessor    // 연쇄 반응 끝날때까지 확인 
{
    // 매치 발생 -> 끝까지 감독 ( 매치 블럭제거, 특수 블럭 생성, 장애물 활성화, 중력과 리필 적용, 연쇄 확인) 
    Task ProcessMatchesAsync(List<MatchGroup> matches, IBlock swappedBlockA = null, IBlock swappedBlockB = null);

    event Action<List<IBlock>> OnMatchFound;        // 매치를 찾아서 해당 블럭을 파괴함을 알림 
    event Action<HexaCoords> OnJackActivated;       // 좌표의 잭이 맞았음을 알림 
    event Action<IBlock, Action> OnSpecialBlockCreated;         // 좌표에 특수 블럭을 만들것을 알림 
    event Action<List<BlockMoveInfo>, Action> OnBlocksDropped;  // 블럭을 아래로 떨어뜨림을 알림 (중력)
    event Action<List<IBlock>, Action> OnNewBlocksGenerated;    // 맨 위에 새 블럭 리필을 알림 
}