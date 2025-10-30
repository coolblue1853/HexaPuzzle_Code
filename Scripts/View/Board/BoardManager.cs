using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject tilePrefab;
    public GameObject jackPrefab;
    public List<GameObject> blockPrefabs;
    public List<GameObject> effectPrefabs;
    public GameObject jackEffectPrefab;
    public List<GameObject> lineBlasterPrefabs;
    public GameObject flyingJackPrefab;
    public GameObject hintBorderPrefab;

    [Header("UI")]
    public RectTransform targetGoalIcon;
    public Camera uiCamera;

    [Header("Settings")]
    public float goalIconBounceScale = 1.5f;
    public float goalIconBounceDuration = 0.3f;
    public float hintPulseDuration = 1.0f;
    public float cameraPadding = 2.0f;
    [SerializeField] private float spacingMultiplier = 1.05f;

    private IGrid _grid;
    private IBoard _board;
    private MapData _mapData;

    private HexaCoordinateConverter _converter;
    private CameraManager _cameraManager;
    private BoardViewGenerator _viewGenerator;
    private BoardViewAnimator _viewAnimator;
    private UIEffectManager _uiEffectManager;
    private HintVisualizer _hintVisualizer;

    public HintVisualizer HintVisualizer => _hintVisualizer;

    // 보드매니저 초기화
    public void Init(IGrid grid, IBoard board, MapData mapData, int colorCount, IGameManager gameManager)
    {
        _grid = grid;
        _board = board;
        _mapData = mapData;

        _converter = new HexaCoordinateConverter(tilePrefab, spacingMultiplier, _grid);

        _cameraManager = new CameraManager(Camera.main, uiCamera, cameraPadding);
        _cameraManager.AdjustCameraView(_converter.BoardBounds);

        _viewGenerator = new BoardViewGenerator(tilePrefab, jackPrefab, _mapData, _grid, _converter, this.transform);
        _viewGenerator.GenerateBoardVisuals();

        _viewAnimator = new BoardViewAnimator(blockPrefabs, lineBlasterPrefabs, effectPrefabs, _converter, this.transform);
        _viewAnimator.Subscribe(_board, _board);

        _uiEffectManager = new UIEffectManager(jackEffectPrefab, flyingJackPrefab, targetGoalIcon, _converter, this.transform, this, goalIconBounceScale, goalIconBounceDuration, gameManager);
        _uiEffectManager.Subscribe(_board);

        _hintVisualizer = new HintVisualizer(hintBorderPrefab, hintPulseDuration, _converter, this.transform, this);
        _hintVisualizer.Init(_board);
    }

    public void CleanupBoard()  // 보드의 뷰와 데이터 모두 정리
    {
        //  모든 이펙트 및 애니메이션 중지 및 이벤트 구독 해지
        if (_board != null)
        {
            _viewAnimator?.Unsubscribe(_board);
            _uiEffectManager?.Unsubscribe(_board);
            _board.UnsubscribeAllEvents();
        }

        StopAllCoroutines();
        _uiEffectManager?.StopAllCoroutines();
        _hintVisualizer?.StopHint();
        _viewAnimator?.Cleanup();

        foreach (Transform child in transform)  // 타일은 삭제 그 외는 풀에 반환
        {
            if (child.name.StartsWith("Tile_") || child.name.StartsWith("Jack_"))
                Destroy(child.gameObject);
            else
                ObjectPooler.Instance.Despawn(child.gameObject);
        }
    }

    public void FillInitBlocks()
    {
        _viewAnimator?.FillInitBlocks();
    }

    public GameObject GetBlockObjectAt(HexaCoords coords)
    {
        return _viewAnimator?.GetBlockObjectAt(coords);
    }
}