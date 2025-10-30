using UnityEngine;
using System.Collections;

public class LevelInitializer : MonoBehaviour
{
    public string targetLevelName = "7";    // 시작 스테이지 실제로는 외부에서 주입
    public BoardManager boardManager;
    public InputManager inputManager;
    public BackgroundScaler backgroundScaler;
    [Range(3, 6)] public int blockColorCount = 5;   // 사용할 블럭 색상
    private MapLoader mapLoader;

    private void Start()
    {
        mapLoader = new MapLoader();

        if (ObjectPooler.Instance == null)
            return;

        if (backgroundScaler == null)
            backgroundScaler = FindObjectOfType<BackgroundScaler>();

        StartCoroutine(InitializeGame());
    }

    public void RestartGame()   // 게임 재시작(라운드 재시작)
    {
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.CleanupAll();

        if (boardManager != null)
            boardManager.CleanupBoard();

        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()    // 게임 시작
    {
        MapData levelData = null;

        yield return mapLoader.LoadMap(targetLevelName, (loadedData) => {levelData = loadedData;}); // 맵 데이터를 가져온다


        // 안전 체크
        if (levelData == null)
            yield break;

        IGameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            yield break;

        if (boardManager == null || inputManager == null)
            yield break;

        // 의존성 주입
        IGrid grid = new HexaGrid(levelData);   // 그리드 생성
        Board board = new Board(levelData);     // 보드 생성
        IMatchFinder matchFinder = new MatchFinder(board, grid);
        IBoardGenerator boardGenerator = new BoardGenerator(grid, blockColorCount, matchFinder, levelData);
        IBoardModifier modifier = new BoardModifier(board, grid, boardGenerator, board);
        IBoardAnalyzer analyzer = new BoardAnalyzer(board, board, grid, matchFinder);
        IMatchProcessor matchProcessor = new MatchProcessor(board, board, grid, matchFinder, modifier);

        board.Init(matchFinder, boardGenerator, analyzer, matchProcessor);  // 보드에 작업자들 연결(주입)

        gameManager.InitGameManager(board, levelData.goalCount, levelData.moveLeftCount);       // 게임 상태 초기화
        boardManager.Init(grid, board, levelData, blockColorCount, gameManager);                // 시각적 초기화
        inputManager.Init(board, board, grid, boardManager, gameManager, boardManager.HintVisualizer);  // 입력 처리 상태 초기화
        board.FillInitBoard();          // 최초 블럭 채우기
        boardManager.FillInitBlocks();  // 최초로 채운값을 화면에 표현

        if (backgroundScaler != null)   // 배경 사이즈 변경
            backgroundScaler.InitBackgroundScaler();

        gameManager.StartGame();    // 게임 시작
    }
}