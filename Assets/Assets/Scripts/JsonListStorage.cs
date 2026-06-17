using System;
using System.IO;
using UnityEngine;

namespace UnityDragDropLists
{
    public static class JsonListStorage
    {
        public static string GetPersistentPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public static string GetStreamingAssetsPath(string fileName)
        {
            return Path.Combine(Application.streamingAssetsPath, fileName);
        }

        public static void SaveToPersistent(DualListSaveData data, string fileName)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string json = JsonUtility.ToJson(data, true);
            string path = GetPersistentPath(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        public static bool TryLoadPersistent(string fileName, out DualListSaveData data)
        {
            return TryLoadFromPath(GetPersistentPath(fileName), out data);
        }

        public static bool TryLoadStreamingAssets(string fileName, out DualListSaveData data)
        {
            return TryLoadFromPath(GetStreamingAssetsPath(fileName), out data);
        }

        public static bool TryLoadFromPath(string path, out DualListSaveData data)
        {
            data = null;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                data = JsonUtility.FromJson<DualListSaveData>(json);
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to load list data from '{path}'. Error: {exception.Message}");
                data = null;
                return false;
            }
        }
    }
}
