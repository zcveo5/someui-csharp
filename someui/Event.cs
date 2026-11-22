namespace someui;

public class Event
{
    private readonly List<Action> _subscribers = [];
    
    public void Add(Action callback)
    {
        if (!_subscribers.Contains(callback))
        {
            _subscribers.Add(callback);
        }
    }
    
    public void Remove(Action callback)
    {
        _subscribers.Remove(callback);
    }
    
    public void Invoke()
    {
        foreach (var callback in _subscribers)
        {
            callback?.Invoke();
        }
    }
}

public class Event1Arg<T1>
{
    private readonly List<Action<T1>> _subscribers = [];
    
    public void Add(Action<T1> callback)
    {
        if (!_subscribers.Contains(callback))
        {
            _subscribers.Add(callback);
        }
    }
    
    public void Remove(Action<T1> callback)
    {
        _subscribers.Remove(callback);
    }
    
    public void Invoke(T1 arg1)
    {
        foreach (var callback in _subscribers)
        {
            callback?.Invoke(arg1);
        }
    }
}

public class EventPairArg<T1, T2>
{
    private readonly List<Action<T1, T2>> _subscribers = [];
    
    public void Add(Action<T1, T2> callback)
    {
        if (!_subscribers.Contains(callback))
        {
            _subscribers.Add(callback);
        }
    }
    
    public void Remove(Action<T1, T2> callback)
    {
        _subscribers.Remove(callback);
    }
    
    public void Invoke(T1 arg1, T2 arg2)
    {
        foreach (var callback in _subscribers)
        {
            callback?.Invoke(arg1, arg2);
        }
    }
}