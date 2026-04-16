using UnityEngine;
using UnityEngine.EventSystems;
using CardBattle.Core;

public class EnemyTargetHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private EnemyBattleUnit enemyBattleUnit;
    [SerializeField] private GameObject targetRing;
    [SerializeField] private float hoverMultiplier = 1.2f;

    private bool isSelectable;
    private Vector3 baseScale;

    private void Awake()
    {
        if (targetRing != null)
            baseScale = targetRing.transform.localScale;
    }

    public void SetSelectable(bool value)
    {
        bool canSelect = value && enemyBattleUnit != null && enemyBattleUnit.IsAlive;
        isSelectable = canSelect;

        if (targetRing != null)
        {
            targetRing.SetActive(canSelect);
            targetRing.transform.localScale = baseScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelectable || targetRing == null)
            return;

        targetRing.transform.localScale = baseScale * hoverMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelectable || targetRing == null)
            return;

        targetRing.transform.localScale = baseScale;
    }
}