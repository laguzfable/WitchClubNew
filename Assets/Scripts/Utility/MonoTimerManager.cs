using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kenaz
{
    public class MonoTimerManager : MonoBehaviour
    {
        public LinkedList<MonoTimer> timerList { private set; get; } = new LinkedList<MonoTimer>();

        private void Awake()
        {
            Toolbox.RegisterComponent<MonoTimerManager>();
        }

        // Update is called once per frame
        void Update()
        {
            float dt = Time.deltaTime;
            foreach(var timer in timerList)
            {
                if (timer.isRunning)
                {
                    timer.Update(dt);
                }
            }
        }
    }

    public class MonoTimer
    {
        protected MonoTimerManager mgr;

        public float curTime { private set; get; } = 0f;
        public float endTime;
        public bool isRunning { private set; get; } = false;

        public event System.Action<float> OnTick = null;
        public event System.Action OnComplete = null;

        public int curRepeatCount { private set; get; } = 0;
        public int repeatCount { private set; get; } = 0;

        public event System.Action OnRepeatComplete = null;

        class SequenceData
        {
            public float triggerTime;
            public System.Action OnTrigger;
            public bool isComplete = false;

            public SequenceData(float time, System.Action e)
            {
                triggerTime = time;
                OnTrigger = e;
            }
        }
        List<SequenceData> seqEventArr = new List<SequenceData>();

        // set time to 0 will running until stop manually.
        public MonoTimer(float time, bool immediate = false)
        {
            mgr = Toolbox.Instance.GetOrAddComponent<MonoTimerManager>();
            mgr.timerList.AddLast(this);
            endTime = time;
            isRunning = immediate;
        }

        ~MonoTimer()
        {
            seqEventArr.Clear();
            seqEventArr = null;
            if (mgr != null)
            {
                mgr.timerList.Remove(this);
                mgr = null;
            }
        }

        public MonoTimer SetRepeat(float time, int count)
        {
            endTime = time;
            repeatCount = count;

            return this;
        }

        public MonoTimer AddSequence(float time, System.Action e)
        {
            seqEventArr.Add(new SequenceData(time, e));
            return this;
        }

        public void Start()
        {
            isRunning = true;
        }

        public void Reset(bool keepRunning = false)
        {
            curTime = 0f;
            isRunning = keepRunning;
            foreach(var seq in seqEventArr)
            {
                seq.isComplete = false;
            }
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void ClearEvent()
        {
            OnTick = null;
            OnComplete = null;
        }

        public void Update(float dt)
        {
            curTime += dt;

            foreach (var seq in seqEventArr)
            {
                if (!seq.isComplete)
                {
                    if (curTime >= seq.triggerTime)
                    {
                        seq.OnTrigger();
                        seq.isComplete = true;
                    }
                }
            }

            if (OnComplete != null && IsComplete())
            {
                isRunning = false;
                if(repeatCount != 0) // repeat mode
                {
                    if(repeatCount != -1 && curRepeatCount < repeatCount)
                    {
                        curRepeatCount++;
                        isRunning = true;
                    }
                    else
                    {
                        OnRepeatComplete?.Invoke();
                    }
                }
                OnComplete();
            }
            else
            {
                OnTick?.Invoke(dt);
            }
        }

        public bool IsComplete()
        {
            if(endTime  <= 0f)
            {
                return false;
            }
            return curTime >= endTime;
        }
    }
}
