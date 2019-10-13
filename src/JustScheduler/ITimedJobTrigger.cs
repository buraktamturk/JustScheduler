using System;

namespace JustScheduler {
    public interface ITimedJobTrigger<T> {
        void Trigger(DateTime scheduleAt);
    }
    
    public interface ITimedJobTrigger<T, X> {
        void Trigger(X data, DateTime scheduleAt);
    }
}