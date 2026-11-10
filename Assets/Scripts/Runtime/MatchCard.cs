using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SherrysGame
{
    public sealed class MatchCard : MonoBehaviour, IBeginDragHandler, IDragHandler,
        IEndDragHandler, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public int PairId { get; private set; }
        public bool IsKey { get; private set; }
        public bool Matched { get; private set; }
        private SherrysGameController game;
        private RectTransform rect;
        private CanvasGroup group;
        private Outline outline;
        private bool selected;
        private bool hovered;
        private bool hinted;
        private bool dragging;
        private float blockClickUntil;
        private Coroutine shake;
        private Vector2 restingPosition;
        private string caption;

        public void Initialize(SherrysGameController owner, int id, bool isKey)
        {
            game = owner;
            PairId = id;
            IsKey = isKey;
            rect = (RectTransform)transform;
            caption = isKey ? PairCatalog.All[id].Key : PairCatalog.All[id].Value.ToString();
            GameUI.Surface(rect, isKey ? GameUI.Card : new Color32(47, 43, 77, 255));
            group = gameObject.AddComponent<CanvasGroup>();
            outline = gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2, -2);
            outline.useGraphicAlpha = true;
            RefreshOutline();
            Text label = GameUI.Label(caption, rect, isKey ? 24 : 28,
                isKey ? GameUI.White : GameUI.Lavender, TextAnchor.MiddleCenter);
            if (!isKey) label.fontStyle = FontStyle.Bold;
            GameUI.Stretch(label.rectTransform, 10);
            var size = GameUI.Size(rect, 68, 1);
            size.minHeight = 36;
        }

        public void SetSelected(bool value)
        {
            selected = value;
            RefreshOutline();
        }

        public void SetHinted(bool value)
        {
            hinted = value;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (outline == null) return;
            outline.enabled = selected || hovered || hinted;
            outline.effectColor = hinted ? new Color32(255, 211, 131, 255) :
                selected ? GameUI.Mint : new Color32(125, 144, 186, 255);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Matched || eventData.button != PointerEventData.InputButton.Left) return;
            dragging = true;
            blockClickUntil = Time.unscaledTime + .2f;
            group.alpha = .35f;
            group.blocksRaycasts = false;
            game.BeginDrag(this, caption, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging) game.MoveDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            blockClickUntil = Time.unscaledTime + .2f;
            group.alpha = Matched ? 0 : 1;
            group.blocksRaycasts = !Matched;
            game.EndDrag(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (Matched || eventData.pointerDrag == null) return;
            MatchCard source = eventData.pointerDrag.GetComponent<MatchCard>();
            if (source != null && source != this && source.game == game && source.IsKey != IsKey)
                game.Match(source, this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Matched && !dragging && Time.unscaledTime >= blockClickUntil &&
                eventData.button == PointerEventData.InputButton.Left)
                game.Select(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = !Matched;
            RefreshOutline();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            RefreshOutline();
        }

        public void Disappear()
        {
            Matched = true;
            group.blocksRaycasts = false;
            if (shake != null)
            {
                StopCoroutine(shake);
                rect.anchoredPosition = restingPosition;
                shake = null;
            }
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float startAlpha = group.alpha;
            for (float time = 0; time < .24f; time += Time.unscaledDeltaTime)
            {
                float t = time / .24f;
                group.alpha = Mathf.Lerp(startAlpha, 0, t);
                rect.localScale = Vector3.one * Mathf.Lerp(1, .9f, t);
                yield return null;
            }
            group.alpha = 0;
        }

        public void Reject()
        {
            if (shake != null)
            {
                StopCoroutine(shake);
                rect.anchoredPosition = restingPosition;
            }
            restingPosition = rect.anchoredPosition;
            shake = StartCoroutine(Shake());
        }

        private IEnumerator Shake()
        {
            for (float time = 0; time < .28f; time += Time.unscaledDeltaTime)
            {
                float offset = Mathf.Sin(time * 65) * 7 * (1 - time / .28f);
                rect.anchoredPosition = restingPosition + new Vector2(offset, 0);
                yield return null;
            }
            rect.anchoredPosition = restingPosition;
            shake = null;
        }
    }
}
