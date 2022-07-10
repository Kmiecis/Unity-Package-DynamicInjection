# Common Dynamic Injection

## Description
A simple and lightweight dynamic dependency injection implementation.

## Examples

We begin by defining a class that we want to use as an injection.
To allow switching for different implementations, we will use some common interface.
```cs
public interface IManager
{
    bool Value { get; }
}

public class TrueManager : IManager
{
    public bool Value => true;
}

public class FalseManager : IManager
{
    public bool Value => false;
}
```

In following cases, DI_ADependantBehaviour will handle binding in Awake method. This means both installation and injection.

To install it we have few options, depending on the use case.
In case that we have a concrete instance:
```cs
public class Managers : DI_ADependantBehaviour
{
    [DI_Install]
    public IManager manager = new TrueManager();
}
```

In case that we want the instance to be created on the fly OR TrueManager would also be a MonoBehaviour:
```cs
public class Managers : DI_ADependantBehaviour
{
    [DI_Install(type: typeof(IManager))]
    public TrueManager manager;
}
```

Injecting works similarly.
```cs
public class User : DI_ADependantBehaviour
{
    [DI_Inject]
    private IManager _manager;

    private void OnIManagerInject(IManager manager)
    {
        // No need to assign the value. '_manager' will be set shortly. This is just convenient callback.
        UnityEngine.Debug.Log("Received manager with value: " + manager.Value);
    }
}
```
