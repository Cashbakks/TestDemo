using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityDragDropLists
{
    public sealed class DropListView : MonoBehaviour
    {
        [Header("Identity")]
        public string ListTitle = "List";

        [Header("References")]
        public RectTransform RootRect;
        public RectTransform ContentRoot;
        public TMP_Text HeaderText;
        public ScrollRect ScrollRect;

        [Header("Optional sorting controls")]
        public Toggle SortByTextToggle;
        public Toggle SortByNumberToggle;
        public TMP_Text SortByTextLabel;
        public TMP_Text SortByNumberLabel;

        [Header("Runtime data")]
        public List<ListItemData> Items = new List<ListItemData>();

        private DualListManager _manager;
        private bool _initialized;

        public void Initialize(DualListManager manager)
        {
            _manager = manager;

            if (RootRect == null)
            {
                RootRect = transform as RectTransform;
            }

            if (SortByTextToggle != null)
            {
                SortByTextToggle.onValueChanged.RemoveListener(OnSortByTextToggleChanged);
                SortByTextToggle.onValueChanged.AddListener(OnSortByTextToggleChanged);
            }

            if (SortByNumberToggle != null)
            {
                SortByNumberToggle.onValueChanged.RemoveListener(OnSortByNumberToggleChanged);
                SortByNumberToggle.onValueChanged.AddListener(OnSortByNumberToggleChanged);
            }

            _initialized = true;
            RefreshHeader();
            RefreshSortLabels();
        }

        public bool ContainsScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            RectTransform targetRect = RootRect != null ? RootRect : transform as RectTransform;
            return targetRect != null && RectTransformUtility.RectangleContainsScreenPoint(targetRect, screenPosition, eventCamera);
        }

        public void PutPlaceholderAtPointer(GameObject placeholder, Vector2 screenPosition, Camera eventCamera)
        {
            if (placeholder == null || ContentRoot == null)
            {
                return;
            }

            if (placeholder.transform.parent != ContentRoot)
            {
                placeholder.transform.SetParent(ContentRoot, false);
            }

            int targetIndex = GetPointerIndex(screenPosition, eventCamera, placeholder.transform);
            placeholder.transform.SetSiblingIndex(targetIndex);
        }

        public int GetPointerIndex(Vector2 screenPosition, Camera eventCamera, Transform ignoredChild)
        {
            if (ContentRoot == null)
            {
                return 0;
            }

            int index = 0;

            for (int i = 0; i < ContentRoot.childCount; i++)
            {
                Transform child = ContentRoot.GetChild(i);
                if (child == ignoredChild)
                {
                    continue;
                }

                RectTransform childRect = child as RectTransform;
                if (childRect == null)
                {
                    continue;
                }

                Vector2 childScreenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, childRect.position);
                if (screenPosition.y > childScreenCenter.y)
                {
                    return index;
                }

                index++;
            }

            return index;
        }

        public void RefreshHeader()
        {
            if (HeaderText != null)
            {
                int count = Items != null ? Items.Count : 0;
                HeaderText.text = $"{ListTitle} ({count})";
            }
        }

        public void RefreshSortLabels()
        {
            if (SortByTextLabel != null && SortByTextToggle != null)
            {
                SortByTextLabel.text = SortByTextToggle.isOn ? "String ↑" : "String ↓";
            }

            if (SortByNumberLabel != null && SortByNumberToggle != null)
            {
                SortByNumberLabel.text = SortByNumberToggle.isOn ? "Number ↑" : "Number ↓";
            }
        }

        private void OnSortByTextToggleChanged(bool ascending)
        {
            if (!_initialized || _manager == null)
            {
                return;
            }

            RefreshSortLabels();
            _manager.SortList(this, SortMode.Text, ascending);
        }

        private void OnSortByNumberToggleChanged(bool ascending)
        {
            if (!_initialized || _manager == null)
            {
                return;
            }

            RefreshSortLabels();
            _manager.SortList(this, SortMode.Number, ascending);
        }
    }
}
