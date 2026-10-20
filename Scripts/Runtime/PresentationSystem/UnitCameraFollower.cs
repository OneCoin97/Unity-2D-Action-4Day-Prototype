using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class UnitCameraFollower : MonoBehaviour
{
    #region Inspector

    [SerializeField] private Unit targetUnit;
    [SerializeField] private Vector3 followOffset = new Vector3(0f,0f,-10f);

    #endregion

    private float shakeEndTime;
    private float shakePower;

    private void LateUpdate()
    {
        if (targetUnit == null)
        {
            return;
        }

        Vector3 shakeOffset = Vector3.zero;
        if (Time.time < shakeEndTime)
        {
            shakeOffset = Random.insideUnitCircle * shakePower;
        }

        transform.position = targetUnit.transform.position + followOffset + shakeOffset;
    }

    public void shake(float duration,float power)
    {
        shakeEndTime = Time.time + duration;
        shakePower = power;
    }

    public void setTarget(Unit unit)
    {
        targetUnit = unit;
    }
}
