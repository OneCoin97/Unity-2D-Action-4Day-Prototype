using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrainingDummyUnit : Unit
{
    [SerializeField] private bool useSuperArmor;

    protected override void onSpawn()
    {
        unitStateController.stateData.isSuperArmor = useSuperArmor;
    }
}
