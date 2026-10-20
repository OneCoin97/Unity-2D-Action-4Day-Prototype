#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnitComponents
{
    // 에디터에서 스킬 피격 범위를 확인한다.
    public sealed partial class UnitSkillLauncher
    {
        #region Editor Inspector

        [Header("Hit Box Preview")]
        [SerializeField] private bool showHitBoxPreview = true;
        [SerializeField] private bool showActiveHitBoxes = true;
        [SerializeField] private SkillDataObject previewSkillData;
        [SerializeField] private bool previewFacingLeft;
        [SerializeField] private Color previewHitBoxColor = new Color(1f,0.2f,0.1f,0.25f);
        [SerializeField] private Color activeHitBoxColor = new Color(1f,0.9f,0.1f,0.35f);

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (!showHitBoxPreview)
            {
                return;
            }

            if (Application.isPlaying && showActiveHitBoxes && drawActiveHitBoxGizmos())
            {
                return;
            }

            SkillDataObject skillDataObject = getPreviewSkillData();
            if (skillDataObject == null || skillDataObject.hitData == null)
            {
                return;
            }

            drawPreviewHitBoxGizmo(skillDataObject);
        }

        private SkillDataObject getPreviewSkillData()
        {
            if (Application.isPlaying && unit != null && unit.unitSkillController.currentSkill != null)
            {
                return unit.unitSkillController.currentSkill;
            }

            return previewSkillData;
        }

        private void drawPreviewHitBoxGizmo(SkillDataObject skillDataObject)
        {
            SkillHitData hitData = skillDataObject.hitData;
            int direction = previewFacingLeft ? -1 : 1;
            if (Application.isPlaying && unit != null)
            {
                direction = unit.unitStateController.stateData.getFacingDirection();
            }

            Vector3 startPosition = transform.position;
            Vector3 hitBoxOffset = new Vector3(hitData.offset.x * direction,hitData.offset.y);
            Vector3 hitBoxPosition = startPosition +
                                     Vector3.right * direction * hitData.range +
                                     hitBoxOffset;

            Handles.color = new Color(previewHitBoxColor.r,previewHitBoxColor.g,previewHitBoxColor.b,1f);
            Handles.DrawDottedLine(startPosition,hitBoxPosition,4f);
            drawHitShape(hitBoxPosition,hitData.shapeType,hitData.size,previewHitBoxColor);

            string attackType = hitData.attackType.ToString();
            string shapeSize = hitData.shapeType == SkillHitShapeType.Box
                ? $"{Mathf.Abs(hitData.size.x):0.##} x {Mathf.Abs(hitData.size.y):0.##}"
                : $"Radius {getCircleRadius(hitData.size):0.##}";
            Handles.Label(hitBoxPosition + Vector3.up * 0.2f,$"{skillDataObject.name}\n{attackType} / {shapeSize}");
        }

        private bool drawActiveHitBoxGizmos()
        {
            bool hasActiveHitBox = false;
            foreach (UnitSkillEngine skillEngine in activeSkillEngines)
            {
                if (skillEngine == null || !skillEngine.gameObject.activeInHierarchy)
                {
                    continue;
                }

                hasActiveHitBox = true;
                BoxCollider2D boxCollider = skillEngine.GetComponent<BoxCollider2D>();
                if (boxCollider != null && boxCollider.enabled)
                {
                    drawColliderBounds(boxCollider.bounds,activeHitBoxColor);
                    continue;
                }

                CircleCollider2D circleCollider = skillEngine.GetComponent<CircleCollider2D>();
                if (circleCollider != null && circleCollider.enabled)
                {
                    drawCircle(circleCollider.bounds.center,circleCollider.bounds.extents.x,activeHitBoxColor);
                }
            }

            return hasActiveHitBox;
        }

        private void drawHitShape(Vector3 position,SkillHitShapeType shapeType,Vector2 size,Color color)
        {
            if (shapeType == SkillHitShapeType.Box)
            {
                Vector3 boxSize = new Vector3(
                    Mathf.Max(0.01f,Mathf.Abs(size.x)),
                    Mathf.Max(0.01f,Mathf.Abs(size.y)),
                    0f);
                Gizmos.color = color;
                Gizmos.DrawCube(position,boxSize);
                Gizmos.color = new Color(color.r,color.g,color.b,1f);
                Gizmos.DrawWireCube(position,boxSize);
                return;
            }

            drawCircle(position,getCircleRadius(size),color);
        }

        private void drawColliderBounds(Bounds bounds,Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawCube(bounds.center,bounds.size);
            Gizmos.color = new Color(color.r,color.g,color.b,1f);
            Gizmos.DrawWireCube(bounds.center,bounds.size);
        }

        private void drawCircle(Vector3 position,float radius,Color color)
        {
            Handles.color = color;
            Handles.DrawSolidDisc(position,Vector3.forward,radius);
            Handles.color = new Color(color.r,color.g,color.b,1f);
            Handles.DrawWireDisc(position,Vector3.forward,radius);
        }

        private float getCircleRadius(Vector2 size)
        {
            return Mathf.Max(0.01f,Mathf.Max(Mathf.Abs(size.x),Mathf.Abs(size.y)) * 0.5f);
        }
    }
}
#endif
