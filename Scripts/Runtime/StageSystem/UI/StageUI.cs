using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StageUI : MonoBehaviour
{
    #region Inspector

    [SerializeField] private Text resultText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button restartButton;

    #endregion

    private StageController stageController;

    private void Awake()
    {
        stageController = StageController.instance;
        resultText.gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        stageController.gameStarted += onGameStarted;
        stageController.gameOver += onGameOver;
        stageController.gameClear += onGameClear;
    }

    private void OnDisable()
    {
        stageController.gameStarted -= onGameStarted;
        stageController.gameOver -= onGameOver;
        stageController.gameClear -= onGameClear;
    }

    private void onGameStarted()
    {
        resultText.gameObject.SetActive(false);
        startButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
    }

    private void onGameOver()
    {
        showResult("GAME OVER");
    }

    private void onGameClear()
    {
        showResult("GAME CLEAR");
    }

    private void showResult(string result)
    {
        resultText.text = result;
        resultText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
    }
}
