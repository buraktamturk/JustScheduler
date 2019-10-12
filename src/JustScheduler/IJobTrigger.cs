namespace JustScheduler {
    public interface IJobTrigger<T> {
        void Trigger();
    }
    
    public interface IJobTrigger<T, X> {
        void Trigger(X data);
    }
}