using System.Collections;
using UnityEngine;

namespace Lib
{
    public interface ICoroutineRunner
    {
        Coroutine Run(IEnumerator coroutine);
        void Stop(IEnumerator routine);
        void Stop(Coroutine routine);
    }

    public class CoroutineRunner : SingletonSceneAutoCreated<CoroutineRunner>, ICoroutineRunner
    {
        public Coroutine Run(IEnumerator coroutine) => StartCoroutine(coroutine);
        public void Stop(IEnumerator coroutine) => StopCoroutine(coroutine);
        public void Stop(Coroutine routine) => StopCoroutine(routine);
    }

}