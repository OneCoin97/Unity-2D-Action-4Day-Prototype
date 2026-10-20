using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitModel))]
[RequireComponent(typeof(UnitInstaller))]
public sealed class UnitController : MonoBehaviour
{
    public event Action<Unit> unitDeathCompleted;

    private readonly Dictionary<string,Stack<Unit>> unitPool = new Dictionary<string,Stack<Unit>>();
    private readonly Dictionary<Unit,string> rentedUnits = new Dictionary<Unit,string>();
    private readonly List<Unit> activeUnits = new List<Unit>();

    private UnitModel unitModel;
    private UnitInstaller unitInstaller;

    private void Awake()
    {
        unitModel = GetComponent<UnitModel>();
        unitInstaller = GetComponent<UnitInstaller>();
        unitModel.loadUnitList();
    }

    public bool tryCreatePlayer(string unitName,out Unit unit)
    {
        if (!tryGetUnit(unitName,out unit))
        {
            return false;
        }

        unitInstaller.installPlayer(unit);
        return true;
    }

    public IEnumerator createStageUnits(StageDataObject stageDataObject,float spawnInterval,Action<Unit> unitCreated)
    {
        bool isFirstUnit = true;
        foreach (StageUnitData stageUnitData in stageDataObject.unitList)
        {
            if (stageUnitData == null || stageUnitData.count <= 0)
            {
                continue;
            }

            for (int i = 0; i < stageUnitData.count; i++)
            {
                if (!isFirstUnit && spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }

                if (!tryGetUnit(stageUnitData.unitName,out Unit unit))
                {
                    continue;
                }

                if (!unitInstaller.tryInstall(unit))
                {
                    Debug.LogWarning($"{stageUnitData.unitName} 설치 위치에서 Ground를 찾을 수 없습니다.",this);
                    release(unit);
                    continue;
                }

                unitCreated(unit);
                isFirstUnit = false;
            }
        }
    }

    public void releaseAll()
    {
        while (activeUnits.Count > 0)
        {
            release(activeUnits[activeUnits.Count - 1]);
        }
    }

    private void onUnitDeathCompleted(Unit unit)
    {
        release(unit);
        unitDeathCompleted?.Invoke(unit);
    }

    private void release(Unit unit)
    {
        if (!rentedUnits.Remove(unit,out string unitName))
        {
            return;
        }

        unit.deathCompleted -= onUnitDeathCompleted;
        activeUnits.Remove(unit);
        unit.gameObject.SetActive(false);
        unit.transform.SetParent(transform);
        getPool(unitName).Push(unit);
    }

    private bool tryGetUnit(string unitName,out Unit unit)
    {
        if (!unitModel.tryGetUnitPrefab(unitName,out Unit unitPrefab))
        {
            Debug.LogError($"Resources/Units에서 {unitName} 유닛을 찾을 수 없습니다.",this);
            unit = null;
            return false;
        }

        Stack<Unit> targetUnitPool = getPool(unitName);
        unit = targetUnitPool.Count > 0
            ? targetUnitPool.Pop()
            : createUnit(unitPrefab);

        rentedUnits.Add(unit,unitName);
        activeUnits.Add(unit);
        unit.deathCompleted += onUnitDeathCompleted;
        return true;
    }

    private Unit createUnit(Unit unitPrefab)
    {
        Unit unit = Instantiate(unitPrefab,transform);
        unit.gameObject.SetActive(false);
        return unit;
    }

    private Stack<Unit> getPool(string unitName)
    {
        if (!unitPool.TryGetValue(unitName,out Stack<Unit> targetUnitPool))
        {
            targetUnitPool = new Stack<Unit>();
            unitPool.Add(unitName,targetUnitPool);
        }

        return targetUnitPool;
    }

    private void OnDestroy()
    {
        foreach (Unit unit in activeUnits)
        {
            if (unit != null)
            {
                unit.deathCompleted -= onUnitDeathCompleted;
            }
        }

        unitDeathCompleted = null;
    }
}
