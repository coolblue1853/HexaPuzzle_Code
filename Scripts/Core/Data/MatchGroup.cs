using System.Collections.Generic;

public enum MatchType
{
    Line,       // 직선형태 
    Cluster,    // 중심과 연속된 3개이상의 같은 블럭 
}

public class MatchGroup
{
    public readonly List<IBlock> Blocks;
    public readonly MatchType Type;
    public readonly int Direction;
    public int Count => Blocks.Count;

    public MatchGroup(List<IBlock> blocks, MatchType type, int direction = -1)
    {
        Blocks = blocks;
        Type = type;
        Direction = direction;
    }
}