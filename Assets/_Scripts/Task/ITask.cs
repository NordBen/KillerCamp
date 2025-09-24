using System;
using System.Collections;
using UnityEngine;

namespace KillerCamp.TaskSystem
{
    public interface ITask
    {
        void Execute(object owningObject);
    }

    public enum TaskState { Started, OnGoing, Finished }

    [Serializable]
    public class TimedTask : ITask
    {
        [SerializeField] private float duration;

        public event Action<ITask> OnCompleted;

        private TaskState _state;
        private float _elapsedTime;

        public TaskState CurrentState => _state;

        public void Execute(object owningObject)
        {
            if (owningObject is MonoBehaviour monoOwner)
            {
                monoOwner.StartCoroutine(DurationTask());
            }
        }

        private void Started()
        {
            _state = TaskState.Started;
        }

        private void Complete()
        {
            _state = TaskState.Finished;
            OnCompleted?.Invoke(this);
        }

        private IEnumerator DurationTask()
        {
            Started();
            while (_elapsedTime < duration)
            {
                yield return null;
                _elapsedTime += Time.deltaTime;
                _state = TaskState.OnGoing;

                Debug.Log($"Time elasped {_elapsedTime}");
            }
            Complete();
            yield break;
        }
    }
}