using System.Collections.Generic;

public class HexaGrid : IGrid
{
    private readonly MapData _mapData;
    private readonly int[] _tileStatuses;

    public int Width => _mapData.width;
    public int Height => _mapData.height;

    private static readonly int[][] OddQNeighborOffsets = new int[][]
    {
        // Q가 짝수일때 
        new int[] { +1, -1 }, new int[] { +1, 0 }, new int[] { 0, +1 },
        new int[] { -1, 0 }, new int[] { -1, -1 }, new int[] { 0, -1 },

        // Q가 홀수일때 
        new int[] { +1, 0 }, new int[] { +1, +1 }, new int[] { 0, +1 },
        new int[] { -1, +1 }, new int[] { -1, 0 }, new int[] { 0, -1 }
    };

    public HexaGrid(MapData mapData)
    {
        _mapData = mapData;
        _tileStatuses = _mapData.tileSet.ToArray();
    }

    public bool IsValidCoordinate(int q, int r)
    {
        return q >= 0 && q < Width && r >= 0 && r < Height;
    }

    private int GetIndex(int q, int r)
    {
        return r * Width + q;
    }

    public int GetTileStatus(int q, int r)
    {
        if (!IsValidCoordinate(q, r))
        {
            return -1;
        }
        return _tileStatuses[GetIndex(q, r)];
    }

    public void SetTileStatus(int q, int r, int status)
    {
        if (IsValidCoordinate(q, r))
        {
            _tileStatuses[GetIndex(q, r)] = status;
        }
    }

    public List<HexaCoords> GetNeighbors(HexaCoords center)
    {
        List<HexaCoords> neighbors = new List<HexaCoords>();

        for (int i = 0; i < 6; i++)
        {
            HexaCoords neighbor = GetNeighbor(center, i);   // 재활용 

            if (neighbor.Q != -1)
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    public HexaCoords GetNeighbor(HexaCoords center, int direction)
    {
        if (direction < 0 || direction > 5)
            return new HexaCoords(-1, -1);

        int parity = center.Q & 1;          // 홀수 짝수 확인 짝수면 0 홀수면 1
        int offsetStartIndex = parity * 6;  // 짝수라면 0부터, 홀수라면 6부터 시작

        int deltaQ = OddQNeighborOffsets[offsetStartIndex + direction][0];
        int deltaR = OddQNeighborOffsets[offsetStartIndex + direction][1];

        // 구조체이기 때문이 미리 생성해도 성능상 큰 이슈 x 
        HexaCoords neighbor = new HexaCoords(center.Q + deltaQ, center.R + deltaR);

        if (IsValidCoordinate(neighbor.Q, neighbor.R))
            return neighbor;

        return new HexaCoords(-1, -1);
    }

    public HexaCoords GetCoordinate(int index)
    {
        if (index >= 0 && index < _tileStatuses.Length)
        {
            int r = index / Width;
            int q = index % Width;
            return new HexaCoords(q, r);
        }
        return new HexaCoords(-1, -1);
    }
}