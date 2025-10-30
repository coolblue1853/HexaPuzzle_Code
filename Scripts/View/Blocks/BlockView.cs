using UnityEngine;

// 블럭에 컴포넌트로 붙는 View,  데이터와 <> 시각
public class BlockView : MonoBehaviour
{
    private IBlock _blockModel;

    public void Init(IBlock blockModel)
    {
        _blockModel = blockModel;
    }

    public HexaCoords GetCoords()
    {
        return _blockModel.Coords;
    }

    public IBlock GetBlockModel()   // 원본 블럭 데이터 반환
    {
        return _blockModel;
    }
}