using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityDragDropLists
{
    public sealed class DualListManager : MonoBehaviour
    {
        [Header("Scene references")]
        public Canvas RootCanvas;
        public DraggableListTile TilePrefab;
        public DropListView LeftList;
        public DropListView RightList;
        public TMP_Text StatusText;
        public Button SaveButton;
        public Button LoadButton;
        public Button ResetButton;

        [Header("Data")]
        public string SaveFileName = "dual_lists_data.json";
        public bool LoadSavedFileOnStart = true;

        private readonly List<DropListView> _lists = new List<DropListView>();
        private GameObject _placeholder;
        private DropListView _placeholderList;
        private int _dragStartSiblingIndex;

        private void Awake()
        {
            if (RootCanvas == null)
            {
                RootCanvas = GetComponentInParent<Canvas>();
            }

            RegisterList(LeftList);
            RegisterList(RightList);

            if (SaveButton != null)
            {
                SaveButton.onClick.AddListener(SaveToJson);
            }

            if (LoadButton != null)
            {
                LoadButton.onClick.AddListener(LoadFromJsonButton);
            }

            if (ResetButton != null)
            {
                ResetButton.onClick.AddListener(ResetDemoData);
            }
        }

        private void Start()
        {
            bool loaded = false;

            if (LoadSavedFileOnStart)
            {
                loaded = TryLoadFromPersistentJson(showMessage: false);
            }

            if (!loaded)
            {
                loaded = TryLoadDefaultStreamingJson();
            }

            if (!loaded)
            {
                ApplyData(CreateFallbackDemoData());
                SetStatus("Demo data generated. JSON was not found yet.");
            }
        }

        private void RegisterList(DropListView list)
        {
            if (list == null || _lists.Contains(list))
            {
                return;
            }

            _lists.Add(list);
            list.Initialize(this);
        }

        public void BeginDrag(DraggableListTile tile, PointerEventData eventData)
        {
            if (tile == null || tile.OwnerList == null || tile.OwnerList.ContentRoot == null)
            {
                return;
            }

            _dragStartSiblingIndex = tile.transform.GetSiblingIndex();
            CreatePlaceholder(tile);
            _placeholderList = tile.OwnerList;

            RectTransform tileRect = tile.transform as RectTransform;
            if (tileRect != null)
            {
                tileRect.SetParent(RootCanvas.transform, true);
                tileRect.SetAsLastSibling();
            }

            if (tile.CanvasGroup != null)
            {
                tile.CanvasGroup.blocksRaycasts = false;
                tile.CanvasGroup.alpha = 0.88f;
            }
        }

        public void Drag(DraggableListTile tile, PointerEventData eventData)
        {
            if (tile == null || _placeholder == null)
            {
                return;
            }

            DropListView targetList = GetListUnderPointer(eventData.position, eventData.pressEventCamera);
            if (targetList == null)
            {
                targetList = _placeholderList != null ? _placeholderList : tile.OwnerList;
            }

            if (targetList == null)
            {
                return;
            }

            _placeholderList = targetList;
            targetList.PutPlaceholderAtPointer(_placeholder, eventData.position, eventData.pressEventCamera);
        }

        public void EndDrag(DraggableListTile tile, PointerEventData eventData)
        {
            if (tile == null)
            {
                DestroyPlaceholder();
                return;
            }

            DropListView targetList = _placeholderList != null ? _placeholderList : tile.OwnerList;
            int targetIndex = _placeholder != null ? _placeholder.transform.GetSiblingIndex() : _dragStartSiblingIndex;

            if (targetList == null)
            {
                targetList = tile.OwnerList;
            }

            MoveTileData(tile, targetList, targetIndex);

            if (targetList != null && targetList.ContentRoot != null)
            {
                tile.transform.SetParent(targetList.ContentRoot, false);
                tile.transform.SetSiblingIndex(Mathf.Clamp(targetIndex, 0, targetList.ContentRoot.childCount));
                tile.SetOwnerList(targetList);
            }

            if (tile.CanvasGroup != null)
            {
                tile.CanvasGroup.blocksRaycasts = true;
                tile.CanvasGroup.alpha = 1f;
            }

            DestroyPlaceholder();
            RefreshHeaders();
            SetStatus($"Moved '{tile.Data.text}' to {targetList.ListTitle}.");
        }

        public DropListView GetListUnderPointer(Vector2 screenPosition, Camera eventCamera)
        {
            for (int i = 0; i < _lists.Count; i++)
            {
                DropListView list = _lists[i];
                if (list != null && list.ContainsScreenPoint(screenPosition, eventCamera))
                {
                    return list;
                }
            }

            return null;
        }

        public void SortList(DropListView list, SortMode mode, bool ascending)
        {
            if (list == null || list.Items == null)
            {
                return;
            }

            IEnumerable<ListItemData> query;
            switch (mode)
            {
                case SortMode.Number:
                    query = ascending
                        ? list.Items.OrderBy(item => item.number)
                        : list.Items.OrderByDescending(item => item.number);
                    break;
                case SortMode.Text:
                default:
                    query = ascending
                        ? list.Items.OrderBy(item => item.text, StringComparer.CurrentCultureIgnoreCase)
                        : list.Items.OrderByDescending(item => item.text, StringComparer.CurrentCultureIgnoreCase);
                    break;
            }

            list.Items = query.ToList();
            RefreshListVisuals(list);
            RefreshHeaders();

            string direction = ascending ? "ascending" : "descending";
            string field = mode == SortMode.Number ? "number" : "string";
            SetStatus($"Sorted {list.ListTitle} by {field}, {direction}.");
        }

        public void SaveToJson()
        {
            DualListSaveData data = CaptureData();
            JsonListStorage.SaveToPersistent(data, SaveFileName);
            SetStatus($"Saved: {JsonListStorage.GetPersistentPath(SaveFileName)}");
        }


        public void LoadFromJsonButton()
        {
            if (!TryLoadFromPersistentJson(showMessage: true))
            {
                SetStatus($"Save file not found: {JsonListStorage.GetPersistentPath(SaveFileName)}");
            }
        }

        public void ResetDemoData()
        {
            ApplyData(CreateFallbackDemoData());
            SetStatus("Demo data reset. Press Save to write it into JSON.");
        }

        public DualListSaveData CaptureData()
        {
            return new DualListSaveData
            {
                leftItems = LeftList != null ? new List<ListItemData>(LeftList.Items) : new List<ListItemData>(),
                rightItems = RightList != null ? new List<ListItemData>(RightList.Items) : new List<ListItemData>()
            };
        }

        public void ApplyData(DualListSaveData data)
        {
            if (data == null)
            {
                data = CreateFallbackDemoData();
            }

            if (LeftList != null)
            {
                LeftList.Items = data.leftItems ?? new List<ListItemData>();
                EnsureIds(LeftList.Items);
                RefreshListVisuals(LeftList);
            }

            if (RightList != null)
            {
                RightList.Items = data.rightItems ?? new List<ListItemData>();
                EnsureIds(RightList.Items);
                RefreshListVisuals(RightList);
            }

            RefreshHeaders();
        }

        private bool TryLoadFromPersistentJson(bool showMessage)
        {
            if (!JsonListStorage.TryLoadPersistent(SaveFileName, out DualListSaveData data))
            {
                return false;
            }

            ApplyData(data);
            if (showMessage)
            {
                SetStatus($"Loaded: {JsonListStorage.GetPersistentPath(SaveFileName)}");
            }

            return true;
        }

        private bool TryLoadDefaultStreamingJson()
        {
            if (!JsonListStorage.TryLoadStreamingAssets("default_lists.json", out DualListSaveData data))
            {
                return false;
            }

            ApplyData(data);
            SetStatus("Loaded default JSON from StreamingAssets.");
            return true;
        }

        private void MoveTileData(DraggableListTile tile, DropListView targetList, int targetIndex)
        {
            if (tile == null || tile.Data == null || tile.OwnerList == null || targetList == null)
            {
                return;
            }

            DropListView sourceList = tile.OwnerList;
            sourceList.Items.Remove(tile.Data);

            int safeIndex = Mathf.Clamp(targetIndex, 0, targetList.Items.Count);
            targetList.Items.Insert(safeIndex, tile.Data);
        }

        private void RefreshListVisuals(DropListView list)
        {
            if (list == null || list.ContentRoot == null || TilePrefab == null)
            {
                return;
            }

            for (int i = list.ContentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(list.ContentRoot.GetChild(i).gameObject);
            }

            for (int i = 0; i < list.Items.Count; i++)
            {
                DraggableListTile tile = Instantiate(TilePrefab, list.ContentRoot);
                tile.gameObject.SetActive(true);
                tile.Setup(list.Items[i], list, this, RootCanvas);
            }

            list.RefreshHeader();
        }

        private void RefreshHeaders()
        {
            if (LeftList != null)
            {
                LeftList.RefreshHeader();
            }

            if (RightList != null)
            {
                RightList.RefreshHeader();
            }
        }

        private void CreatePlaceholder(DraggableListTile tile)
        {
            DestroyPlaceholder();

            _placeholder = new GameObject("Drag Placeholder", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            RectTransform placeholderRect = _placeholder.GetComponent<RectTransform>();
            LayoutElement layoutElement = _placeholder.GetComponent<LayoutElement>();
            Image image = _placeholder.GetComponent<Image>();

            layoutElement.preferredHeight = tile.GetPreferredHeight();
            layoutElement.flexibleWidth = 1f;
            image.color = new Color(1f, 1f, 1f, 0.12f);
            image.raycastTarget = false;

            _placeholder.transform.SetParent(tile.OwnerList.ContentRoot, false);
            _placeholder.transform.SetSiblingIndex(tile.transform.GetSiblingIndex());
            placeholderRect.localScale = Vector3.one;
        }

        private void DestroyPlaceholder()
        {
            if (_placeholder != null)
            {
                Destroy(_placeholder);
                _placeholder = null;
            }

            _placeholderList = null;
        }

        private void EnsureIds(List<ListItemData> items)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(items[i].id))
                {
                    items[i].id = Guid.NewGuid().ToString("N");
                }
            }
        }

        private DualListSaveData CreateFallbackDemoData()
        {
            return new DualListSaveData
            {
                leftItems = new List<ListItemData>
                {
                    new ListItemData("Apple", 17),
                    new ListItemData("Orange", 4),
                    new ListItemData("Banana", 29),
                    new ListItemData("Mango", 12),
                    new ListItemData("Kiwi", 8)
                },
                rightItems = new List<ListItemData>
                {
                    new ListItemData("Falcon", 22),
                    new ListItemData("Wolf", 6),
                    new ListItemData("Bear", 41),
                    new ListItemData("Fox", 15)
                }
            };
        }

        private void SetStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.text = message;
            }

            Debug.Log(message);
        }
    }
}
