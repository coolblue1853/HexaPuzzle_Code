//  블럭 이동 정보 구조체
public struct BlockMoveInfo
{
    public IBlock Block;
    public HexaCoords From;
    public HexaCoords To;

    public BlockMoveInfo(IBlock block, HexaCoords from, HexaCoords to)
    {
        Block = block;
        From = from;
        To = to;
    }
}