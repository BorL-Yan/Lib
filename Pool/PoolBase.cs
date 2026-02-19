using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lib
{
    public class PoolBase<T>
    {
        #region fields

        private readonly Func<T> preloadFunc;
        private readonly Action<T> getAction;
        private readonly Action<T> returnAction;
        private Queue<T> pool = new Queue<T>();
        private readonly List<T> activeObjects = new List<T>();
        
        public int PoolSize => pool.Count + activeObjects.Count;

        #endregion

        #region constructor

        public PoolBase(Func<T> preloadFunc, Action<T> getAction, Action<T> returnAction, uint preladCount)
        {
            if (preloadFunc == null)
            {
                Debug.LogError("PoolBase: preloadFunc is null.");
                return;
            }
            this.preloadFunc = preloadFunc;
            this.getAction = getAction;
            this.returnAction = returnAction;

            for (int i = 0; i < preladCount; i++)
                Return(preloadFunc());
        }
        

        #endregion

        #region public metods

        public T Get()
        {
            T itme = pool.Count > 0 ? pool.Dequeue() : preloadFunc();
            getAction?.Invoke(itme);
            activeObjects.Add(itme);
            return itme;
        }

        public void Return(T itme)
        {
            returnAction?.Invoke(itme);
            pool.Enqueue(itme); 
            activeObjects.Remove(itme);
        }

        public void ReturnAllActive()
        {
            foreach (var item in activeObjects.ToArray())
            {
                Return(item);
            }
        }
        
        public int ReturnActiveCount() => activeObjects.Count;

        #endregion
        
    }
}