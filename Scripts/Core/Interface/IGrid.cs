using System.Collections.Generic;

public interface IGrid
{
    // 헥사 그리드 인터페이터 (벌집 모양)

    int Width { get; }
    int Height { get; }

    bool IsValidCoordinate(int q, int r);                   // 유효 좌표 확인 
    int GetTileStatus(int q, int r);                        // 타일 상태(빈칸인지 아닌지) 반환 
    void SetTileStatus(int q, int r, int status);           // 타일 상태 설정 

    List<HexaCoords> GetNeighbors(HexaCoords center);       // 중심으로 부터 이웃 타일 반환 
    HexaCoords GetCoordinate(int index);                    // 1차원 인덱스를 헥사 좌표로 반환 
    HexaCoords GetNeighbor(HexaCoords center, int direction);// 특정 방향의 이웃 가져오기 (단일) 
}
