public interface IBlock
{
    int BlockID { get; }    // 고유번호 
    int ColorType { get; }
    BlockType Type { get; }
    int Direction { get; }

    HexaCoords Coords { get; set; }
}