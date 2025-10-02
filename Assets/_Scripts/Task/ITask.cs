using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace KillerCamp.TaskSystem
{
    public interface ITask
    {
        abstract void Execute(object owningObject);
        TaskState CurrentState { get; }
        bool HasStarted { get; }
        bool HasFinished { get; }
    }

    public enum TaskState { Started, OnGoing, Finished }

    [Serializable]
    public class BaseTask : ITask, INetworkSerializable
    {
        public event Action<ITask> OnComplete;
        
        protected TaskState state;
        public TaskState CurrentState => state;
        public bool HasStarted { get; private set; }

        public bool HasFinished { get; private set; }


        public void Execute(object owningObject)
        {
            OnStarted();
            OnExecute(owningObject);
            OnCompleted();
        }

        protected virtual void OnExecute(object owningObject) { }
        
        public virtual void OnStarted()
        {
            HasStarted = true;
            state = TaskState.Started;
        }

        public virtual void OnCompleted()
        {
            HasFinished = true;
            state = TaskState.Finished;
            OnComplete?.Invoke(this);
            TaskManager.instance.ServerCompleteTaskRpc();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            
        }
    }

    [Serializable]
    public class TimedTask : BaseTask
    {
        [SerializeField] private float taskDuration;
        
        private int _elapsedTime;

        protected override void OnExecute(object owningObject)
        {
            if (owningObject is MonoBehaviour monoOwner)
            {
                monoOwner.StartCoroutine(DurationTask());
            }
        }

        private IEnumerator DurationTask()
        {
            state = TaskState.OnGoing;
            while (_elapsedTime < taskDuration)
            {
                yield return new WaitForSecondsRealtime(1f);
                _elapsedTime++;
                Debug.Log($"Time elasped {_elapsedTime}");
            }
        }
    }
}