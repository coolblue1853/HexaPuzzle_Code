using UnityEngine;

public class HexaCoordinateConverter
{
    private readonly float _hexaWidth;  // 타일 한개의 가로넓이 
    private readonly float _hexaHeight; // 타일 한개의 세로넓이 
    private readonly Vector3 _boardOffset;  // 보드 전체의 중심점
    private Bounds _boardBounds;    // 보드 경계

    public Bounds BoardBounds => _boardBounds;

    public HexaCoordinateConverter(GameObject tilePrefab, float spacingMultiplier, IGrid grid)
    {
        if (tilePrefab == null)
            return;

        float hexaSize = tilePrefab.transform.localScale.x / 2 * spacingMultiplier; // 타일 크기 계산
        _hexaWidth = 2f * hexaSize;
        _hexaHeight = Mathf.Sqrt(3f) * hexaSize;

        _boardOffset = CalculateBoardOffset(grid);
    }

    private Vector3 CalculateBoardOffset(IGrid grid)    // 보드 중신점 및 경계 계산
    {
        bool firstTileFound = false;
        _boardBounds = new Bounds();
        for (int r = 0; r < grid.Height; r++)   // 모든 칸 순회
        {
            for (int q = 0; q < grid.Width; q++)
            {
                if (grid.GetTileStatus(q, r) == 1)  // 실제 타일이라면
                {
                    Vector3 tilePos = GetRelativeWorldPosition(q, r);
                    if (!firstTileFound)    // 최초 타일이면
                    {
                        _boardBounds = new Bounds(tilePos, Vector3.one * 0.1f); // 경계 시작지점 생성 
                        firstTileFound = true;
                    }
                    else
                        _boardBounds.Encapsulate(tilePos);
                }
            }
        }

        if (firstTileFound)
            return _boardBounds.center; // 중심점을 보드의 중심으로 지정
        else
            return Vector3.zero;
    }

    private Vector3 GetRelativeWorldPosition(int q, int r) // 핵사 좌표를 월드 기준으로 변환
    {
        float x = q * _hexaWidth * 0.75f;
        float y = -r * _hexaHeight;
        if (q % 2 != 0)
            y -= _hexaHeight * 0.5f;
        return new Vector3(x, y, 0);
    }

    public Vector3 GetWorldPosition(int q, int r)  // 실제 사용하는 좌표 변환(보드 중심 이동 포함)
    {
        Vector3 relativePos = GetRelativeWorldPosition(q, r);
        return relativePos - _boardOffset;
    }
}