using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitComponents;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitController))]
public sealed class StageController : MonoBehaviour
{
    private static StageController currentInstance;

    public static StageController instance
    {
        get
        {
            if (currentInstance == null)
            {
                currentInstance = FindFirstObjectByType<StageController>();
                if (currentInstance == null)
                {
                    GameObject stageControllerObject = new GameObject(nameof(StageController));
                    currentInstance = stageControllerObject.AddComponent<StageController>();
                }
            }

            return currentInstance;
        }
    }

    private UnitController unitController;

    public event Action gameStarted;
    public event Action gameOver;
    public event Action gameClear;

    #region Inspector

    [Header("Stage")]
    [SerializeField] private List<StageDataObject> stageDataList = new List<StageDataObject>();
    [SerializeField, Min(0f)] private float spawnInterval = 0.5f;

    [Header("Player")]
    [SerializeField] private string playerUnitName = "Player";
    [SerializeField] private UnitCameraFollower cameraFollower;

    [field: Header("Runtime")]
    [field: SerializeField] public int currentStageIndex { get; private set; } = -1;
    [field: SerializeField] public int remainingEnemyCount { get; private set; }

    #endregion

    private readonly List<Unit> spawnedEnemyUnits = new List<Unit>();
    
    private Unit playerUnit;
    private Coroutine stageProcessCoroutine;
   

    private void Awake()
    {
        unitController = GetComponent<UnitController>();
        unitController.unitDeathCompleted += onUnitDeathCompleted;
    }

    public void startGame()
    {
        if (stageDataList.Count == 0)
        {
            Debug.LogError("Stage Data List가 비어 있습니다.",this);
            return;
        }

        stopStageProcess();
        releaseAllUnits();

        currentStageIndex = -1;
        remainingEnemyCount = 0;

        if (!spawnPlayer())
        {
            return;
        }

        gameStarted?.Invoke();
        stageProcessCoroutine = StartCoroutine(processStage());
    }

    public void restartGame()
    {
        startGame();
    }

    private IEnumerator processStage()
    {
        for (int stageIndex = 0; stageIndex < stageDataList.Count; stageIndex++)
        {
            StageDataObject stageDataObject = stageDataList[stageIndex];
            if (stageDataObject == null)
            {
                continue;
            }

            currentStageIndex = stageIndex;
            yield return unitController.createStageUnits(stageDataObject,spawnInterval,registerEnemyUnit);

            if (stageIndex < stageDataList.Count - 1)
            {
                float stageEndTime = Time.time + stageDataObject.stageDuration;
                while (Time.time < stageEndTime)
                {
                    if (remainingEnemyCount == 0)
                    {
                        break;
                    }

                    yield return null;
                }
            }
        }

        while (remainingEnemyCount > 0)
        {
            yield return null;
        }

        stageProcessCoroutine = null;
        finishGameClear();
    }

    private void registerEnemyUnit(Unit unit)
    {
        spawnedEnemyUnits.Add(unit);
        remainingEnemyCount = spawnedEnemyUnits.Count;
    }

    private bool spawnPlayer()
    {
        if (!unitController.tryCreatePlayer(playerUnitName,out playerUnit))
        {
            return false;
        }

        cameraFollower.setTarget(playerUnit);
        if (playerUnit.tryGetUnitComponent(out UnitCameraShakeController cameraShakeController))
        {
            cameraShakeController.setCameraFollower(cameraFollower);
        }

        return true;
    }

    private void onUnitDeathCompleted(Unit deadUnit)
    {
        if (deadUnit == playerUnit)
        {
            playerUnit = null;
            cameraFollower.setTarget(null);
            finishGameOver();
            return;
        }

        spawnedEnemyUnits.Remove(deadUnit);
        remainingEnemyCount = spawnedEnemyUnits.Count;
    }

    private void releaseAllUnits()
    {
        unitController.releaseAll();
        playerUnit = null;
        cameraFollower.setTarget(null);
        spawnedEnemyUnits.Clear();
        remainingEnemyCount = 0;
    }

    private void finishGameOver()
    {
        stopStageProcess();
        releaseAllUnits();
        gameOver?.Invoke();
    }

    private void finishGameClear()
    {
        gameClear?.Invoke();
    }

    private void stopStageProcess()
    {
        if (stageProcessCoroutine == null)
        {
            return;
        }

        StopCoroutine(stageProcessCoroutine);
        stageProcessCoroutine = null;
    }

    private void OnDestroy()
    {
        stopStageProcess();
        unitController.unitDeathCompleted -= onUnitDeathCompleted;

        gameStarted = null;
        gameOver = null;
        gameClear = null;
        if (currentInstance == this)
        {
            currentInstance = null;
        }
    }
}
