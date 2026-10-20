using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitModel : MonoBehaviour
{
    private const string unitResourcePath = "Units";

    private readonly Dictionary<string,Unit> unitPrefabDictionary = new Dictionary<string,Unit>();

    public void loadUnitList()
    {
        unitPrefabDictionary.Clear();

        GameObject[] unitPrefabList = Resources.LoadAll<GameObject>(unitResourcePath);
        foreach (GameObject unitPrefabObject in unitPrefabList)
        {
            if (!unitPrefabObject.TryGetComponent(out Unit unitPrefab))
            {
                continue;
            }

            string unitName = unitPrefab.unitData.unitName;
            if (string.IsNullOrWhiteSpace(unitName))
            {
                Debug.LogWarning($"{unitPrefabObject.name}의 Unit Name이 비어 있습니다.",unitPrefabObject);
                continue;
            }

            if (unitPrefabDictionary.ContainsKey(unitName))
            {
                Debug.LogWarning($"Resources/{unitResourcePath}에 Unit Name이 {unitName}인 유닛이 두 개 이상 있습니다.",unitPrefabObject);
                continue;
            }

            unitPrefabDictionary.Add(unitName,unitPrefab);
        }
    }

    public bool tryGetUnitPrefab(string unitName,out Unit unitPrefab)
    {
        return unitPrefabDictionary.TryGetValue(unitName,out unitPrefab);
    }
}
