using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardGenerator : IBoardGenerator
{
    private readonly IGrid _grid;
    private readonly int _colorCount;
    private readonly IMatchFinder _matchFinder;
    private readonly MapData _mapdata;

    public BoardGenerator(IGrid grid, int colorCount, IMatchFinder matchFinder, MapData mapData)
    {
        _grid = grid;
        _colorCount = colorCount;
        _matchFinder = matchFinder;
        _mapdata = mapData;
    }

    public Dictionary<HexaCoords, IBlock> GenerateInitBlocks(out int idCounter)
    {
        idCounter = 0;      // 고유번호
        var blocks = new Dictionary<HexaCoords, IBlock>();

        for (int r = 0; r < _grid.Height; r++)
        {
            for (int q = 0; q < _grid.Width; q++)
            {
                if (_grid.GetTileStatus(q, r) == 1)
                {
                    HexaCoords coords = new HexaCoords(q, r);

                    if (IsObstacleAt(coords))
                        continue;

                    int randomColor = Random.Range(0, _colorCount);
                    blocks[coords] = new Block(idCounter++, randomColor, coords);   // 랜덤색상으로 최초보드 생성
                }
            }
        }

        return blocks;
    }

    public int GenerateValidColorForRefill(HexaCoords coords, IBoardReader boardView)
    {
        List<int> possibleColors = new List<int>(_colorCount);
        for (int i = 0; i < _colorCount; i++)   // 모든 색상 추가
            possibleColors.Add(i);

        int maxTry = _colorCount * 2;   // 최대 시도 횟수
        while (maxTry-- > 0)
        {
            int potentialColor = possibleColors[Random.Range(0, possibleColors.Count)];
            if (!CheckImmediateMatch(coords, potentialColor, boardView))    // 랜덤하게 받아온 컬러로 매치가 발생하지 않음을 확인 
                return potentialColor;

            if (possibleColors.Count > 1)       // 해당 컬러 삭제
                possibleColors.Remove(potentialColor);
            else
                return potentialColor;
        }
        return Random.Range(0, _colorCount);    // 시도내 불가능시 랜덤 컬러 반환
    }

    public void ValidateAndFixBoard(ref Dictionary<HexaCoords, IBlock> blocks, IBoardReader boardView)
    {
        int maxTry = 100;
        while (maxTry-- > 0)
        {
            List<MatchGroup> allMatches = _matchFinder.FindAllMatchesOnBoard(); // 현재의 모든 매치 
            if (allMatches.Count == 0)
                return;

            foreach (MatchGroup matchGroup in allMatches)
            {
                if (matchGroup.Count < 3 || matchGroup.Blocks.Any(b => b.Type != BlockType.Normal))
                    continue;

                IBlock blockToChange = matchGroup.Blocks[matchGroup.Blocks.Count / 2];  // 바꿀 블럭 선택 (중간)

                if (blockToChange.Type == BlockType.Normal)
                {
                    int oldColor = blockToChange.ColorType;
                    List<int> possibleColors = new List<int>();
                    for (int c = 0; c < _colorCount; c++)   // 원래 색 제외 
                    {
                        if (c != oldColor)
                            possibleColors.Add(c);

                        int newColor = (possibleColors.Count == 0) ? oldColor : possibleColors[Random.Range(0, possibleColors.Count)];  // 랜덤하게 뽑아서 블럭에 넣기
                        blocks[blockToChange.Coords] = new Block(blockToChange.BlockID, newColor, blockToChange.Coords, BlockType.Normal);
                    }
                }
            }
        }
    }


    private bool CheckImmediateMatch(HexaCoords coords, int potentialColor, IBoardReader boardView)
    {
        var lineDirections = new (int, int)[] { (0, 3), (1, 4), (2, 5) };

        // 라인 확인 
        foreach ((int dirA, int dirB) in lineDirections)
        {
            int count = 1;
            HexaCoords current = coords;
            for (int i = 0; i < 2; i++)     // A 방향 
            {
                HexaCoords neighborCoords = _grid.GetNeighbor(current, dirA);
                IBlock neighborBlock = boardView.GetBlockAt(neighborCoords);

                if (neighborBlock != null && neighborBlock.Type == BlockType.Normal && neighborBlock.ColorType == potentialColor)
                {
                    count++;
                    current = neighborCoords;
                }
                else
                    break;
            }
            current = coords;
            for (int i = 0; i < 2; i++)     // B 방향 
            {
                HexaCoords neighborCoords = _grid.GetNeighbor(current, dirB);
                IBlock neighborBlock = boardView.GetBlockAt(neighborCoords);
                if (neighborBlock != null && neighborBlock.Type == BlockType.Normal && neighborBlock.ColorType == potentialColor)
                {
                    count++;
                    current = neighborCoords;
                }
                else
                    break;
            }

            if (count >= 3)
                return true;
        }

        // 클러스터 확인 
        bool[] hasColorNeighbor = new bool[6];
        for (int i = 0; i < 6; i++)
        {
            HexaCoords neighborCoords = _grid.GetNeighbor(coords, i);
            IBlock neighbor = boardView.GetBlockAt(neighborCoords);
            if (neighbor != null && neighbor.Type == BlockType.Normal && neighbor.ColorType == potentialColor)
            {
                hasColorNeighbor[i] = true;
            }
        }
        for (int i = 0; i < 6; i++)
        {
            if (hasColorNeighbor[i] && hasColorNeighbor[(i + 1) % 6]) // i 방향과 그 옆의 이웃 
                return true;
        }

        return false;
    }

    private bool IsObstacleAt(HexaCoords coords)
    {
        if (_mapdata?.jackPositions == null)
            return false;
        return _mapdata.jackPositions.Any(jackPos => jackPos.q == coords.Q && jackPos.r == coords.R)
;
    }
}
