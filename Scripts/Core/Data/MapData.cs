using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public int width = 7;                       // 가로 갯수
    public int height = 6;                      // 세로 갯수
    public List<int> tileSet = new List<int>(); // 타일 위치
    public List<TilePosition> jackPositions = new List<TilePosition>(); // 장애물 위치
    public int goalCount = 0;                   // 목표값 
    public int moveLeftCount = 0;               // 남은 이동 횟수

    public int GetTileValue(int q, int r)
    {
        // 타일 유효성 체크후 타일 인덱스 반환 (좌표 -> 1차원 인덱스)
        if (q >= 0 && q < width && r >= 0 && r < height)
            return tileSet[r * width + q];

        return 0;
    }
}
