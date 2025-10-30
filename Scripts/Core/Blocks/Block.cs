public class Block : IBlock
{
    public int BlockID { get; private set; }
    public int ColorType { get; private set; }
    public HexaCoords Coords { get; set; }
    public BlockType Type { get; private set; }
    public int Direction { get; private set; }

    public Block(int id, int colorType, HexaCoords coords, BlockType type = BlockType.Normal, int direction = -1)
    {
        BlockID = id;
        ColorType = colorType;
        Coords = coords;
        Type = type;
        Direction = direction;
    }
}