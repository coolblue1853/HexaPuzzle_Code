using System.Collections.Generic;
using System.Linq;

public class MatchFinder : IMatchFinder
{
    private readonly IBoardReader _boardReader;
    private readonly IGrid _grid;
    private readonly (int, int)[] _lineDirections = new (int, int)[] { (0, 3), (1, 4), (2, 5) }; // 선 매칭시의 방향 (우상, 좌상, 상하)

    public MatchFinder(IBoardReader boardReader, IGrid grid)
    {
        _boardReader = boardReader;
        _grid = grid;
    }

    public List<MatchGroup> FindAllMatchesOnBoard()     // 보드에 존재하는 모든 매치 확인
    {
        var allMatches = new List<MatchGroup>();    
        var checkedBlocks = new HashSet<IBlock>();  //검사 완료 확인 

        foreach (var startBlock in _boardReader.GetBlockData().Values)  // 모든 블럭 대상
        {
            CheckAndAddMatches(startBlock, allMatches, checkedBlocks);
        }

        return allMatches;
    }

    public List<MatchGroup> FindMatchesAfterSwap(IBlock swappedBlockA, IBlock swappedBlockB)    // 스왑 이후 그 주변 매치 확인
    {
        var allMatches = new List<MatchGroup>();
        var checkedBlocks = new HashSet<IBlock>();
        var blocksToCheck = new HashSet<IBlock>();  // 해당 스왑으로 매치가 생겼을 가능성이 있는 블럭 

        foreach (var block in new[] { swappedBlockA, swappedBlockB })   // 스왑 대상인 두 블럭 주변만 
        {
            if (block == null)
                continue;
            blocksToCheck.Add(block);

            for (int i = 0; i < 6; i++)
            {
                var neighbor = _boardReader.GetBlockAt(_grid.GetNeighbor(block.Coords, i)); // 추가할 항목에 A 와 B의 이웃 추가
                if (neighbor != null)
                    blocksToCheck.Add(neighbor);
            }
        }

        foreach (var block in blocksToCheck)
        {
            CheckAndAddMatches(block, allMatches, checkedBlocks);
        }

        return allMatches;
    }

    private void CheckAndAddMatches(IBlock block, List<MatchGroup> allMatchesFound, HashSet<IBlock> checkedBlocks)  // 모든 종류 매치 검사 후 리스트 추가 
    {
        // 방어 조건 및 최적화
        if (block == null || checkedBlocks.Contains(block) || _boardReader.IsObstacleAt(block.Coords))
            return;

        for (int i = 0; i < _lineDirections.Length; i++)
        {
            (int dirA, int dirB) = _lineDirections[i];  // 세부 방향 
            var matchBlocks = FindMatchesInLine(block, dirA, dirB); // 해당 방향으로 라인 채크 

            if (matchBlocks.Count >= 3) // 3개 이상이면 매치 그룹에 추가 (중복 제거)
            {
                AddMatchGroup(new MatchGroup(matchBlocks, MatchType.Line, i), allMatchesFound, checkedBlocks);
            }
        }

        var clusterBlocks = FindClusterMatch(block);    // 클러스터 형태 체크

        if (clusterBlocks.Count >= 4)   // 4개 이상이라면 매치 그룹에 추가
        {
            AddMatchGroup(new MatchGroup(clusterBlocks, MatchType.Cluster), allMatchesFound, checkedBlocks);
        }

    }

    private void AddMatchGroup(MatchGroup newGroup, List<MatchGroup> allMatches, HashSet<IBlock> checkedBlocks)  // 매치리스트에 그룹 추가 (중복방지, 중복제거)
    {
        // 새 그룹이 이미 찾은 것의 부분인지 체크 
        bool alreadyAdded = allMatches.Any(existingGroup => newGroup.Blocks.All(b => existingGroup.Blocks.Contains(b)));

        if (alreadyAdded)   // 그렇다면 넘어간다 
            return;

        // 이 그룹이 다른 그룹을 포함하는가 체크 -> 작은 리스트 제거
        allMatches.RemoveAll(existingGroup => existingGroup.Blocks.All(b => newGroup.Blocks.Contains(b)));

        allMatches.Add(newGroup);

        foreach (var block in newGroup.Blocks)
        {
            checkedBlocks.Add(block);
        }
    }

    private List<IBlock> FindMatchesInLine(IBlock startBlock, int dirA, int dirB)   // 라인 형태의 매치 확인
    {
        if (startBlock == null || _boardReader.IsObstacleAt(startBlock.Coords))
            return new List<IBlock>();

        var matchGroup = new List<IBlock> { startBlock };   // 매칭 목록
        int colorToMatch = startBlock.ColorType;
        HexaCoords currentCoords = startBlock.Coords;   // 현재 위치

        while (true)    // A 방향 확인
        {
            HexaCoords nextCoords = _grid.GetNeighbor(currentCoords, dirA); // 한칸 옆 좌표
            IBlock nextBlock = _boardReader.GetBlockAt(nextCoords);

            // 유효, 장애물 아님, 색깔 같음 
            if (nextBlock != null && !_boardReader.IsObstacleAt(nextCoords) && nextBlock.ColorType == colorToMatch)
            {
                matchGroup.Add(nextBlock);
                currentCoords = nextCoords;
            }
            else
            {
                break;
            }
        }

        currentCoords = startBlock.Coords;  // 재 초기화
        while (true)    // 똑같이 B 방향 확인
        {
            HexaCoords nextCoords = _grid.GetNeighbor(currentCoords, dirB);
            IBlock nextBlock = _boardReader.GetBlockAt(nextCoords);

            if (nextBlock != null && !_boardReader.IsObstacleAt(nextCoords) && nextBlock.ColorType == colorToMatch)
            {
                matchGroup.Add(nextBlock);
                currentCoords = nextCoords;
            }
            else
            {
                break;
            }
        }
        return matchGroup;
    }

    private List<IBlock> FindClusterMatch(IBlock centerBlock)   // 클러스터 형태의 매치 확인
    {
        if (centerBlock == null || _boardReader.IsObstacleAt(centerBlock.Coords))
            return new List<IBlock>();

        int colorToMatch = centerBlock.ColorType;
        bool[] hasColorNeighbor = new bool[6];      // 같은 색이 있는가 
        IBlock[] neighborBlocks = new IBlock[6];    // 같은 색 블럭 자체 

        for (int i = 0; i < 6; i++) // 중심으로 부터 6방향 확인
        {
            HexaCoords neighborCoords = _grid.GetNeighbor(centerBlock.Coords, i);
            IBlock neighbor = _boardReader.GetBlockAt(neighborCoords);
            if (neighbor != null && !_boardReader.IsObstacleAt(neighborCoords) && neighbor.ColorType == colorToMatch)   // 조건 확인
            {
                hasColorNeighbor[i] = true;
                neighborBlocks[i] = neighbor;
            }
        }

        int longestTry = 0; // 가장 긴 연속 이웃 수
        List<IBlock> longestTryBlocks = new List<IBlock>();

        for (int i = 0; i < 6; i++) // 총 6번 시도
        {
            if (!hasColorNeighbor[i])   // 시작이 같은 색이 아니면 탈락
                continue;

            int currentTry = 0;
            List<IBlock> currentTryBlocks = new List<IBlock>();

            for (int j = 0; j < 6; j++)
            {
                int index = (i + j) % 6;    // 순환 인덱스 계산
                if (hasColorNeighbor[index])
                {
                    currentTry++;
                    currentTryBlocks.Add(neighborBlocks[index]);
                }
                else                       // 한번이라도 다른색이 나오면 즉시 중지
                    break;

            }

            if (currentTry > longestTry)    // 최신화
            {
                longestTry = currentTry;
                longestTryBlocks = currentTryBlocks;
            }
        }

        if (longestTry >= 3)
        {
            longestTryBlocks.Add(centerBlock);
            return longestTryBlocks.Distinct().ToList();
        }

        return new List<IBlock>();
    }
}
