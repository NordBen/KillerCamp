using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace KillerCamp.TaskSystem
{
    public interface ITask
    {
        void Execute(object owningObject);
        TaskState CurrentState { get; }
        bool HasStarted { get; }
        bool HasFinished { get; }
        float FireReward { get; }
    }

    public enum TaskState { Unassigned, Assigned, Started, OnGoing, Finished }

    [Serializable]
    public class BaseTask : ITask, INetworkSerializable
    {
        public event Action OnComplete;
        
        protected TaskState state;
        public TaskState CurrentState => state;
        public void SetState(TaskState newState) => state = newState;
        
        public bool HasStarted { get; protected set; }
        public bool HasFinished { get; protected set; }
        
        [SerializeField] private float fireReward = 40f;
        public float FireReward { get => fireReward; set => fireReward = value; }

        public async void Execute(object owningObject)
        {
            OnStarted();
            await OnExecute(owningObject);
            OnCompleted();
        }

        protected virtual async Task OnExecute(object owningObject) { }
        
        public virtual void OnStarted()
        {
            HasStarted = true;
            state = TaskState.Started;
        }

        public virtual void OnCompleted()
        {
            HasFinished = true;
            OnComplete?.Invoke();
        }

        public void Reset()
        {
            HasStarted = false;
            state = TaskState.Unassigned;
            HasFinished = false;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref state);
            serializer.SerializeValue(ref fireReward);
        }
    }

    [Serializable]
    public class TimedTask : BaseTask
    {
        [SerializeField] private int taskDuration = 5;
        
        public int Duration => taskDuration;
        
        private bool coroutineFinished;
        private int _elapsedTime;
        
        public event Action<int> OnTickedEvent;
        
        public TimedTask() {}
        
        public TimedTask(int duration)
        {
            taskDuration = duration;
        }

        protected override async Task OnExecute(object owningObject)
        {
            if (owningObject is MonoBehaviour monoOwner)
            {
                monoOwner.StartCoroutine(DurationTask());
                while (!coroutineFinished)
                    await Task.Yield();
            }
        }
        
        protected virtual void OnTicked()
        {
            Debug.Log($"Time elasped {_elapsedTime}");
            OnTickedEvent?.Invoke(_elapsedTime);
        }
        
        private IEnumerator DurationTask()
        {
            coroutineFinished = false;
            _elapsedTime = 0;
            state = TaskState.OnGoing;
            while (_elapsedTime < taskDuration)
            {
                yield return new WaitForSecondsRealtime(1f);
                _elapsedTime++;
                OnTicked();
            }
            coroutineFinished = true;
        }
    }

    [Serializable]
    public class KillerTask : TimedTask
    {
        [SerializeField] private TaskObject taskToSabotage;

        public KillerTask() {}
        public KillerTask(int duration) : base(duration) {}

        public TaskObject TaskToSabotage { get => taskToSabotage; set => taskToSabotage = value; }
    }
}