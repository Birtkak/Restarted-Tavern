using RestartedTavern.Client.Logic;
using RestartedTavern.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestartedTavern.Client.Table
{
    public enum WidgetKind
    {
        HandCard,
        OpponentHandCard,
        Unit,
        TavernDweller,
    }

    /// <summary>
    /// Something on the table the player can point at: a card in hand, a unit, a Tavern Dweller (which stands for
    /// its player, like LoR's Nexus). It only reports pointer events to the <see cref="TableView"/>.
    /// </summary>
    public sealed class CardWidget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public TableView Table;
        public WidgetKind Kind;
        public CardView View;
        /// <summary>A Tavern Dweller widget: its player (the target for "target player" and attacks).</summary>
        public PlayerId Player;
        public RectTransform Rect => (RectTransform)transform;

        public Vector2 HomePosition;
        public float HomeRotation;
        public int HomeSibling;

        private Outline _glow;
        private CanvasGroup _group;

        public ObjectId Id => View?.Id ?? ObjectId.None;

        public void SetGlow(Color? color)
        {
            if (color == null)
            {
                if (_glow != null) _glow.enabled = false;
                return;
            }
            if (_glow == null)
            {
                var frame = transform.Find("Frame") ?? transform.Find("Back") ?? transform;
                _glow = Ui.AddOutline(frame.gameObject, Color.white, 5f);
            }
            _glow.enabled = true;
            _glow.effectColor = color.Value;
        }

        public void SetRaycasts(bool on)
        {
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = on;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            if (e.button == PointerEventData.InputButton.Right) Table.OnRightClick(this);
            else if (e.button == PointerEventData.InputButton.Left) Table.OnClick(this);
        }

        public void OnPointerEnter(PointerEventData e) => Table.OnHover(this, true);
        public void OnPointerExit(PointerEventData e) => Table.OnHover(this, false);
        public void OnBeginDrag(PointerEventData e) => Table.OnBeginDrag(this, e);
        public void OnDrag(PointerEventData e) => Table.OnDrag(this, e);
        public void OnEndDrag(PointerEventData e) => Table.OnEndDrag(this, e);
    }
}
