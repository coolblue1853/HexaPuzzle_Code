using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // UI 관련 이벤트 구독
    public TextMeshProUGUI remainingGoalText;
    public TextMeshProUGUI remainingMoveText;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoalUpdated += UpdateGoalText;
            GameManager.Instance.OnMoveCountUpdated += UpdateMoveText;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoalUpdated -= UpdateGoalText;
            GameManager.Instance.OnMoveCountUpdated -= UpdateMoveText;
        }
    }

    private void UpdateGoalText(int remainingGoals)
    {
        if (remainingGoalText != null)
        {
            remainingGoalText.text = remainingGoals.ToString();
        }
    }

    private void UpdateMoveText(int remainingMoves)
    {
        if (remainingMoveText != null)
        {
            remainingMoveText.text = remainingMoves.ToString();
        }
    }
}