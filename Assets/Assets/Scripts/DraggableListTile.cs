using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityDragDropLists
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DraggableListTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        public TMP_Text TitleText;
        public TMP_Text NumberText;
        public CanvasGroup CanvasGroup;
        public LayoutElement LayoutElement;
        public Image BackgroundImage;

        public ListItemData Data { get; private set; }
        public DropListView OwnerList { get; private set; }

        private DualListManager _manager;
        private Canvas _rootCanvas;
        private RectTransform _rectTransform;
        private Vector2 _dragOffset;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            if (CanvasGroup == null)
            {
                CanvasGroup = GetComponent<CanvasGroup>();
            }

            if (LayoutElement == null)
            {
                LayoutElement = GetComponent<LayoutElement>();
            }
        }

        public void Setup(ListItemData data, DropListView ownerList, DualListManager manager, Canvas rootCanvas)
        {
            Data = data;
            OwnerList = ownerList;
            _manager = manager;
            _rootCanvas = rootCanvas;
            RefreshVisuals();
        }

        public void SetOwnerList(DropListView newOwnerList)
        {
            OwnerList = newOwnerList;
        }

        public void RefreshVisuals()
        {
            if (Data == null)
            {
                return;
            }

            if (TitleText != null)
            {
                TitleText.text = Data.text;
            }

            if (NumberText != null)
            {
                NumberText.text = Data.number.ToString();
            }
        }

        public float GetPreferredHeight()
        {
            if (LayoutElement != null && LayoutElement.preferredHeight > 0f)
            {
                return LayoutElement.preferredHeight;
            }

            return _rectTransform != null ? _rectTransform.rect.height : 56f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_manager == null || OwnerList == null)
            {
                return;
            }

            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponentInParent<Canvas>();
            }

            if (_rootCanvas == null)
            {
                return;
            }

            RectTransform canvasRect = _rootCanvas.transform as RectTransform;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointer))
            {
                _dragOffset = _rectTransform.localPosition - (Vector3)localPointer;
            }
            else
            {
                _dragOffset = Vector2.zero;
            }

            _manager.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_manager == null || _rootCanvas == null)
            {
                return;
            }

            RectTransform canvasRect = _rootCanvas.transform as RectTransform;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointer))
            {
                _rectTransform.localPosition = localPointer + _dragOffset;
            }

            _manager.Drag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_manager != null)
            {
                _manager.EndDrag(this, eventData);
            }
        }
    }
}
