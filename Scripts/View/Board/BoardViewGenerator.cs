using UnityEngine;

public class BoardViewGenerator
{
    private readonly GameObject _tilePrefab;
    private readonly GameObject _jackPrefab;
    private readonly MapData _mapData;
    private readonly IGrid _grid;
    private readonly HexaCoordinateConverter _converter;
    private readonly Transform _parentTransform;

    public BoardViewGenerator(GameObject tilePrefab, GameObject jackPrefab, MapData mapData, IGrid grid, HexaCoordinateConverter converter, Transform parentTransform)
    {
        _tilePrefab = tilePrefab;   // 타일 원본 
        _jackPrefab = jackPrefab;   // 잭 원본 
        _mapData = mapData;         // 맵 데이터 
        _grid = grid;               // 그리드 정보 
        _converter = converter;     // 좌표 변환기 
        _parentTransform = parentTransform; // 부모 오브젝트(타일들의)
    }

    public void GenerateBoardVisuals()  // 보드 시각 생성 함수
    {
        GenerateBoardTiles();   // 타일 생성 
        SpawnObstacles();       // 장애물 생성 (잭)
    }

    private void GenerateBoardTiles()  // 타일 시각적 생성함수
    {
        int tileCount = 0;
        for (int r = 0; r < _grid.Height; r++)  // 전체 순회 
        {
            for (int q = 0; q < _grid.Width; q++)
            {
                if (_grid.GetTileStatus(q, r) == 1) // 실제 사용 칸이라면 타일 생성
                {
                    Vector3 worldPos = _converter.GetWorldPosition(q, r);
                    GameObject tileObj = Object.Instantiate(_tilePrefab, worldPos, Quaternion.identity, _parentTransform);
                    tileObj.name = $"Tile_{q}_{r}";
                    tileCount++;
                }
            }
        }
    }

    private void SpawnObstacles()   // 장애물 시각적 생성
    {
        if (_jackPrefab == null || _mapData?.jackPositions == null || _mapData.jackPositions.Count == 0)
        {
            return;
        }

        int spawnedCount = 0;
        foreach (TilePosition jackPos in _mapData.jackPositions)
        {
            if (_grid.GetTileStatus(jackPos.q, jackPos.r) == 1) // 잭 위치가 실제 사용칸이라면
            {
                Vector3 worldPos = _converter.GetWorldPosition(jackPos.q, jackPos.r);
                worldPos.z = -0.5f; // 타일 보다 앞에 보이도록 설정
                GameObject jackInstance = Object.Instantiate(_jackPrefab, worldPos, Quaternion.identity, _parentTransform);
                jackInstance.name = $"Jack_{jackPos.q}_{jackPos.r}";
                spawnedCount++;
            }
        }
    }
}