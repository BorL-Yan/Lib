using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lib
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        // Сохраняем словарь в списки перед записью на диск
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // Загружаем списки обратно в словарь при запуске
        public void OnAfterDeserialize()
        {
            this.Clear();
            if (keys.Count != values.Count) return;

            for (int i = 0; i < keys.Count; i++)
                this[keys[i]] = values[i];
        }
    }
}
