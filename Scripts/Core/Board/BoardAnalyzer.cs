using System.Collections.Generic;
using System.Linq;

public class BoardAnalyzer : IBoardAnalyzer
{

    private readonly IBoardReader _boardReader;
    private readonly IBoardModelController _modelController;
    private readonly IGrid _grid;
    private readonly IMatchFinder _matchFinder;

    public BoardAnalyzer(IBoardReader boardReader, IBoardModelController modelController, IGrid grid, IMatchFinder matchFinder)
    {
        _boardReader = boardReader;
        _modelController = modelController;
        _grid = grid;
        _matchFinder = matchFinder;
    }

    public List<IBlock> GetHint()
    {
        return FindAvailableMove();
    }

    public bool HasAvailableMoves()
    {
        List<IBlock> move = FindAvailableMove();
        if (move == null)
            return false;
        return true;
    }

    private List<IBlock> FindAvailableMove()    // 움직일 수 있는 최초의 경우를 반환 
    {
        var currentBlocks = _modelController.Blocks.Values.ToList();    // 현재의 모든 블럭 
        foreach (var blockA in currentBlocks)
        {
            if (blockA == null || _boardReader.IsObstacleAt(blockA.Coords))
                continue;

            for (int i = 0; i < 3; i++) // 3방향 확인 
            {
                HexaCoords neighborCoords = _grid.GetNeighbor(blockA.Coords, i);
                IBlock blockB = _boardReader.GetBlockAt(neighborCoords);

                if (blockB == null || blockA == blockB || _boardReader.IsObstacleAt(neighborCoords))
                    continue;

                _modelController.SwapModelData(blockA, blockB);                                // 데이터만 임시 스왑 
                List<MatchGroup> matches = _matchFinder.FindMatchesAfterSwap(blockA, blockB);  // 임시스왑한 상태에서 매치 확인 

                if (matches.Count > 0)
                {
                    List<HexaCoords> matchedCoords = matches[0].Blocks.Select(b => b.Coords).ToList();
                    _modelController.SwapModelData(blockA, blockB); // 데이터를 원래 상태로 복구 

                    var hintBlocks = new List<IBlock>();
                    foreach (HexaCoords coord in matchedCoords) // 복귀된 상태에서 좌표 조회 (바꿀 블럭 힌트가 아니라 직선 형태로 표기 해야 함)
                    {
                        IBlock blockAtLocation = _boardReader.GetBlockAt(coord);
                        if (blockAtLocation != null)
                            hintBlocks.Add(blockAtLocation);
                    }
                    if (hintBlocks.Count > 0)
                        return hintBlocks;
                }
                else
                    _modelController.SwapModelData(blockA, blockB); // 미 발견시 데이터 복구 

            }
        }
        return null;
    }
}
