# Dynamic Injection

## Description
A simple and lightweight dynamic dependency injection implementation.

## Installation

Add the package via Package Manager by adding it from git URL:  
`https://github.com/Kmiecis/Unity-Package-DynamicInjection.git`  
Package Manager can be found inside the Unity Editor in the Window tab

OR

Git add this repository as a submodule inside your Unity project Assets folder:  
`git submodule add https://github.com/Kmiecis/Unity-Package-DynamicInjection`

## Examples

We will use your typical manager classes to use as an injection.
To allow switching for different implementations, we will use some common interface.

```cs
public interface IManager
{
    bool Value { get; }
}
```

To install dependency we have to mark it by DI_Install attribute.

```cs
[DI_Install]
public class TrueManager : IManager
{
    public bool Value => true;
}
```

By default, installation uses instance type for binding.
We can override it by specyfing binding type in the attribute.

```cs
[DI_Install(typeof(IManager))]
public class FalseManager : IManager
{
    public bool Value => false;
}
```

Installation is done by calling DI_Binder.Bind(...) method.
Installation from the inside of class:

```cs
[DI_Install]
public class MonoManager : MonoBehaviour
{
    private void Awake()
    {
        DI_Binder.Bind(this);
    }

    private void OnDestroy()
    {
        DI_Binder.Unbind(this);
    }
}

[DI_Install]
public class CommonManager
{
    public CommonManager()
    {
        DI_Binder.Bind(this);
    }

    public void Unbind()
    {
        DI_Binder.Unbind(this);
    }
}
```

Installation from the outside of class:

```cs
[DI_Install]
public class MonoManager : MonoBehaviour
{
}

[DI_Install]
public class CommonManager
{
}

public class Managers : MonoBehaviour
{
    public MonoManager monoManagerPrefab;

    private MonoManager _monoManager;
    private CommonManager _commonManager;

    private void Awake()
    {
        _monoManager = Instantiate(monoManagerPrefab);
        DI_Binder.Bind(_monoManager);

        _commonManager = new CommonManager();
        DI_Binder.Bind(_commonManager);
    }

    private void OnDestroy()
    {
        DI_Binder.Unbind(_monoManager);
        DI_Binder.Unbind(_commonManager);
    }
}
```

Injection works similarly.
Injecting once:

```cs
public class User
{
    [DI_Inject(typeof(TrueManager))]
    private IManager _manager;

    public User()
    {
        DI_Binder.Bind(this);
    }

    private void OnIManagerInject(IManager manager)
    {
        // No need to assign the value. Field '_manager' will be set shortly. This is just convenient callback.
        if (manager.Value)
        {   // Outputs if somewhere else TrueManager has been installed
            UnityEngine.Debug.Log("Received TrueManager");
        }
    }
}
```

Injecting updated:

```cs
public class User
{
    [DI_Update(typeof(FalseManager))]
    private IManager _manager;

    public User()
    {
        DI_Binder.Bind(this);
    }

    private void OnIManagerInject(IManager manager)
    {
        // No need to assign the value. Field '_manager' will be set shortly. This is just convenient callback.
        if (manager.Value)
        {   // Never happens
            UnityEngine.Debug.Log("Received TrueManager");
        }
    }
}
```

Installation of external classes that can not be annotated is done by calling DI_Binder.Install(...) method.

```cs
public class Dependencies : MonoBehaviour
{
    public Canvas canvas;
    
    private void Start()
    {
        DI_Binder.Install(canvas);
    }
}
```
