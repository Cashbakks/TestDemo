using System;
using System.Collections.Generic;

namespace UnityDragDropLists
{
    [Serializable]
    public class ListItemData
    {
        public string id;
        public string text;
        public int number;

        public ListItemData() { }

        public ListItemData(string text, int number)
        {
            this.id = Guid.NewGuid().ToString("N");
            this.text = text;
            this.number = number;
        }
    }

    [Serializable]
    public class DualListSaveData
    {
        public List<ListItemData> leftItems = new List<ListItemData>();
        public List<ListItemData> rightItems = new List<ListItemData>();
    }

    public enum SortMode
    {
        Text,
        Number
    }
}
