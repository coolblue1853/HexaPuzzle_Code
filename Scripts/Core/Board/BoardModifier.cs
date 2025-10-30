using System.Collections.Generic;

public class BoardModifier : IBoardModifier
{
    private readonly IBoardModelController _modelController;
    private readonly IGrid _grid;
    private readonly IBoardGenerator _boardGenerator;
    private readonly IBoardReader _boardReader;

    public BoardModifier(IBoardModelController modelController, IGrid grid, IBoardGenerator boardGenerator, IBoardReader boardReader)
    {
        _modelController = modelController;
        _grid = grid;
        _boardGenerator = boardGenerator;
        _boardReader = boardReader;
    }

    public List<BlockMoveInfo> ApplyGravity()
    {
        var moveInfos = new List<BlockMoveInfo>();  // 최종 결과물 

        for (int q = 0; q < _grid.Width; q++)
        {
            var emptySlotsInColumn = new Queue<HexaCoords>();   // 해당 열의 빈칸 스캔 
            for (int r = _grid.Height - 1; r >= 0; r--)
            {
                HexaCoords currentCoords = new HexaCoords(q, r);

                if (_grid.GetTileStatus(q, r) != 1 || _boardReader.IsObstacleAt(currentCoords)) // 장애물 확인시 
                {
                    emptySlotsInColumn.Clear();     // 해당 칸 위로는 떨어질 수 없음 
                    continue;
                }

                IBlock block = _modelController.Blocks.ContainsKey(currentCoords) ? _modelController.Blocks[currentCoords] : null;

                if (block == null)
                    emptySlotsInColumn.Enqueue(currentCoords);
                else if (emptySlotsInColumn.Count > 0)      // 블럭이 있고 아래에 빈칸이 있다면 
                {
                    HexaCoords targetCoords = emptySlotsInColumn.Dequeue();
                    HexaCoords originalCoords = block.Coords;

                    // 모델 데이터 업데이트 
                    _modelController.Blocks.Remove(originalCoords); 
                    _modelController.Blocks[targetCoords] = block;
                    block.Coords = targetCoords;

                    moveInfos.Add(new BlockMoveInfo(block, originalCoords, targetCoords));
                    emptySlotsInColumn.Enqueue(originalCoords); // 블럭이 내려간 자리를 빈칸에 추가 
                }
                else    // 블럭이 있지만 아래 빈칸이 없음 
                    emptySlotsInColumn.Clear();
            }
        }

        moveInfos.Sort((infoA, infoB) =>    // 행은 아래쪽부터, 열은 왠쪽부터 이동 
        {
            int compare = infoA.To.R.CompareTo(infoB.To.R);
            if (compare != 0)
                return compare;
            return infoA.To.Q.CompareTo(infoB.To.Q);

        });

        return moveInfos;
    }

    public List<IBlock> RefillBoard()
    {
        var newBlocks = new List<IBlock>();

        for (int q = 0; q < _grid.Width; q++)
        {
            int fillCount = 0;  // 채워야하는 빈칸의 수 
            for (int r = 0; r < _grid.Height; r++)
            {
                HexaCoords coords = new HexaCoords(q, r); 
                if (_grid.GetTileStatus(q, r) == 1 && !_modelController.Blocks.ContainsKey(coords) && !_boardReader.IsObstacleAt(coords))
                {
                    fillCount++;
                }
            }

            int filled = 0; // 채워진 빈칸의 수 
            for (int r = 0; r < _grid.Height && filled < fillCount; r++)
            {
                HexaCoords coords = new HexaCoords(q, r);
                if (_grid.GetTileStatus(q, r) == 1 && !_modelController.Blocks.ContainsKey(coords) && !_boardReader.IsObstacleAt(coords))
                {
                    int newColor = _boardGenerator.GenerateValidColorForRefill(coords, _boardReader);   // 색상 확인 
                    IBlock newBlock = new Block(_modelController.GetNewBlockID(), newColor, coords);    // 새 ID 생성 
                    _modelController.Blocks[coords] = newBlock; // 데이터 업데이트 
                    newBlocks.Add(newBlock);    // 애니메이션 목록 추가 
                    filled++;
                }
            }
        }

        if (newBlocks.Count > 0)    // 미관용 정렬 
        {
            newBlocks.Sort((blockA, blockB) =>
            {
                int compare = blockA.Coords.R.CompareTo(blockB.Coords.R);
                if (compare != 0)
                    return compare;
                return blockA.Coords.Q.CompareTo(blockB.Coords.Q);
            });
        }

        return newBlocks;
    }

}
