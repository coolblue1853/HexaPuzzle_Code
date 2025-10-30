using System.Collections.Generic;

public interface IBoardModelController      // 핵심 데이터 직접 제어 (내부자 전용 권한)
{
    Dictionary<HexaCoords, IBlock> Blocks { get; }
    void SwapModelData(IBlock blockA, IBlock blockB);   // 두 블럭의 데이터만 변환 
    int GetNewBlockID();    // 새 고유번호 생성 
}