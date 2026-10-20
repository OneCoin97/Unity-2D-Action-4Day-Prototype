using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Slider))]
public sealed class UnitWorldHealthBarView : MonoBehaviour
{
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void setHealth(int hp,int maxHp)
    {
        slider.value = Mathf.Clamp01((float)hp / maxHp);
    }
}
