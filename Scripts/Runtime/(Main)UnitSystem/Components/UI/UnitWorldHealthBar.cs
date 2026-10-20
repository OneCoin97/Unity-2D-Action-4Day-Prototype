using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    public sealed class UnitWorldHealthBar : UnitComponent
    {
        #region Inspector

        [Header("Health Bar")]
        [SerializeField] private UnitWorldHealthBarView healthBarPrefab;

        [Header("Position")]
        [SerializeField] private Vector2 healthBarOffset = new Vector2(0f,2f);

        #endregion

        private UnitWorldHealthBarView healthBar;
        private int previousHp = -1;
        private int previousMaxHp = -1;

        protected override void onInitialize()
        {
            if (healthBarPrefab == null)
            {
                return;
            }

            healthBar = Instantiate(healthBarPrefab,transform,false);
            healthBar.transform.localPosition = healthBarOffset;
            updateHealth();
        }

        private void Update()
        {
            if (healthBar == null)
            {
                return;
            }

            if (previousHp != unit.unitData.hp || previousMaxHp != unit.unitData.maxHp)
            {
                updateHealth();
            }
        }

        private void updateHealth()
        {
            previousHp = unit.unitData.hp;
            previousMaxHp = unit.unitData.maxHp;
            healthBar.setHealth(previousHp,previousMaxHp);
        }
    }
}
