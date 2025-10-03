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

    public enum TaskState { Unassigned, Started, OnGoing, Finished }

    [Serializable]
    public class BaseTask : ITask, INetworkSerializable
    {
        public event Action<ITask> OnComplete;
        
        protected TaskState state;
        public TaskState CurrentState => state;
        public bool HasStarted { get; protected set; }

        public bool HasFinished { get; protected set; }

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

        public void Reset()
        {
            HasStarted = false;
            state = TaskState.Unassigned;
            HasFinished = false;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            
        }
    }

    [Serializable]
    public class TimedTask : BaseTask
    {
        [SerializeField] private float taskDuration;
        
        public float Duration => taskDuration;
        
        private int _elapsedTime;
        
        public TimedTask() {}
        public TimedTask(float duration)
        {
            taskDuration = duration;
        }

        protected override void OnExecute(object owningObject)
        {
            if (owningObject is MonoBehaviour monoOwner)
            {
                monoOwner.StartCoroutine(DurationTask());
            }
        }
        
        protected virtual void OnTicked()
        {
            Debug.Log($"Time elasped {_elapsedTime}");
        }

        private IEnumerator DurationTask()
        {
            state = TaskState.OnGoing;
            while (_elapsedTime < taskDuration)
            {
                yield return new WaitForSecondsRealtime(1f);
                _elapsedTime++;
                OnTicked();
            }
        }
    }

    [Serializable]
    public class KillerTask : TimedTask
    {
        [SerializeField] private TaskObject taskToSabotage;

        public KillerTask() {}
        public KillerTask(float duration) : base(duration) {}

        public TaskObject TaskToSabotage { get => taskToSabotage; set => taskToSabotage = value; }

        public override void OnCompleted()
        {
            base.OnCompleted();
            taskToSabotage.TaskType.Reset();
        }
    }
}