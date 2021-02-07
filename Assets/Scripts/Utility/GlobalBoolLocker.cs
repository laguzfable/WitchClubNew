using System.Collections.Generic;
using System.Threading;

namespace Kenaz
{
    public sealed class GlobalBoolLocker
    {        
        Dictionary<string, bool> keys = new Dictionary<string, bool>();
        static GlobalBoolLocker instance;

        Timer timer;
        string curKey;
        static public GlobalBoolLocker Instance
        {
            get
            {
                if(null == instance)
                {
                    instance = new GlobalBoolLocker();
                }
                
                return instance;
            }
        }
        
        GlobalBoolLocker()
        {
            
        }

        public void UnlockAll()
        {
            keys.Clear();
        }

        /*send lock info to action.
        usualy implement this to button event handler*/
        public void Lock(string key, float duration, System.Action<bool> action)
        {
            if(!IsLock(key))
            {
                LockWithTime(key, duration);
                action(true);
            }
            else
            {
                action(false);
            }
        }
        
        public bool IsLock(string key)
        {
            bool value;
            if(keys.TryGetValue(key, out value))
            {
                return value; 
            }
            return false;
        }

        public void LockWithTime(string key, float duration)
        {
            LockWithTime(key, duration*1000);
        }
        public void LockWithTime(string key, int duration)
        {
            curKey = key;
            Lock(curKey);
            timer = new Timer(new TimerCallback(TimeOut), null, duration, -1);
        }

        void TimeOut(object state)
        {
            timer.Dispose();
            Unlock(curKey);
            curKey = null;
            timer = null;
        }
        
        public void Lock(string key)
        {
            if(keys.ContainsKey(key))
            {
                keys[key] = true;
            }
            else
            {
                keys.Add(key, true);
            }
        }
        
        public void Unlock(string key)
        {
            if(keys.ContainsKey(key))
            {
                keys[key] = false;
            }
            else
            {
                keys.Add(key, false);
            }
        }
    }
}

