using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitComponents;

[DisallowMultipleComponent]
public abstract class Unit : MonoBehaviour
{
    public event Action<Unit> deathCompleted;

    [Header("Spawn")]
    [SerializeField, Min(0f)] private float spawnDuration = 0.5f;

    [Header("Death")]
    [SerializeField, Min(0f)] private float deathDuration = 1f;

    [field: Header("Unit Data")]
    [field: SerializeField]
    public UnitData unitData { get; private set; } = new UnitData();

    [field: Header("Runtime State")]
    [field: SerializeField]
    public UnitStateController unitStateController { get; private set; } = new UnitStateController();
    
    [field: Header("Skill Controller")]
    [field: SerializeField]
    
    public UnitSkillController unitSkillController { get; private set; } = new UnitSkillController();

    #region UnitComponent Dictionary

    private readonly Dictionary<Type, UnitComponent> unitComponents = new Dictionary<Type, UnitComponent>();
    private Coroutine spawnCoroutine;
    private Coroutine deathCoroutine;

    public IReadOnlyDictionary<Type, UnitComponent> UnitComponents => unitComponents;

    protected void rebuildUnitComponentDictionary()
    {
        unitComponents.Clear();
        UnitComponent[] components = GetComponents<UnitComponent>();

        foreach (UnitComponent component in components)
        {
            registerUnitComponent(component);
        }

        foreach (UnitComponent component in components)
        {
            component.initialize(this);
        }
    }

    private void registerUnitComponent(UnitComponent component)
    {
        if (component == null)
        {
            return;
        }

        Type componentType = component.GetType();
        while (componentType != null && componentType != typeof(UnitComponent) &&
               typeof(UnitComponent).IsAssignableFrom(componentType))
        {
            if (unitComponents.TryGetValue(componentType,out UnitComponent registeredComponent) &&
                registeredComponent != component)
            {
                Debug.LogError($"{name}에 {componentType.Name} 타입 컴포넌트가 두 개 이상 등록되어 있습니다.",this);
            }
            else
            {
                unitComponents[componentType] = component;
            }

            componentType = componentType.BaseType;
        }
    }

    public bool tryGetUnitComponent<T>(out T component) where T : UnitComponent
    {
        if (unitComponents.TryGetValue(typeof(T),out UnitComponent unitComponent))
        {
            component = unitComponent as T;
            return component != null;
        }

        component = null;
        return false;
    }

    public T getUnitComponent<T>() where T : UnitComponent
    {
        if (tryGetUnitComponent(out T component))
        {
            return component;
        }

        Debug.LogError($"{name}에 {typeof(T).Name} 컴포넌트가 없습니다.",this);
        return null;
    }

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        unitData.maxHp = Mathf.Max(1,unitData.maxHp);
        unitData.hp = Mathf.Clamp(unitData.hp,0,unitData.maxHp);
        unitStateController.clear();
        unitSkillController.initialize(this);
        rebuildUnitComponentDictionary();
        unitStateController.actionStateChanged += onActionStateChanged;
    }

    protected virtual void OnEnable()
    {
        unitSkillController.initialize(this);
        if (unitComponents.Count == 0)
        {
            rebuildUnitComponentDictionary();
        }

        startSpawnState();
    }

    protected virtual void OnDisable()
    {
        stopSpawnState();
        stopDeathProcess();
        unitSkillController.reset();
    }

    protected virtual void Update()
    {
        unitStateController.updateState();
    }

    protected virtual void OnDestroy()
    {
        stopSpawnState();
        stopDeathProcess();
        unitStateController.actionStateChanged -= onActionStateChanged;
        deathCompleted = null;
        unitSkillController.reset();
        unitStateController.clear();
        unitComponents.Clear();
    }

    protected virtual void onDeath()
    {
        stopDeathProcess();
        deathCoroutine = StartCoroutine(processDeath());
    }

    protected virtual void onSpawn()
    {
    }

    private void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
    {
        if (nextStateType == UnitActionStateType.Spawn)
        {
            onSpawn();
            return;
        }

        if (nextStateType == UnitActionStateType.Dead)
        {
            onDeath();
        }
    }

    private IEnumerator processDeath()
    {
        yield return new WaitForSeconds(deathDuration);

        deathCoroutine = null;
        deathCompleted?.Invoke(this);
    }

    private void startSpawnState()
    {
        stopSpawnState();
        unitData.hp = unitData.maxHp;
        unitStateController.reset();
        unitStateController.stateData.isSpawn = true;
        spawnCoroutine = StartCoroutine(processSpawnState());
    }

    private IEnumerator processSpawnState()
    {
        if (spawnDuration > 0f)
        {
            yield return new WaitForSeconds(spawnDuration);
        }
        else
        {
            yield return null;
        }

        unitStateController.stateData.isSpawn = false;
        spawnCoroutine = null;
    }

    private void stopSpawnState()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        unitStateController.stateData.isSpawn = false;
    }

    private void stopDeathProcess()
    {
        if (deathCoroutine == null)
        {
            return;
        }

        StopCoroutine(deathCoroutine);
        deathCoroutine = null;
    }

    #endregion
}
