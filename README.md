# Dynamic Injection

## Description
A simple and lightweight Dynamic Dependency Injection implementation.

## Installation

Add the package via Package Manager by adding it from git URL:  
`https://github.com/Kmiecis/Unity-Package-DynamicInjection.git`  
Package Manager can be found inside the Unity Editor in the Window tab

OR

Git add this repository as a submodule inside your Unity project Assets folder:  
`git submodule add https://github.com/Kmiecis/Unity-Package-DynamicInjection`

## Examples

We will use plain manager classes to use as an injection.
To allow switching for different implementations, we will add common interface.

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

By default, installation process uses type directly under the attribute.
We can override it by specyfing binding type in the attribute.

```cs
[DI_Install(typeof(IManager))]
public class FalseManager : IManager
{
    public bool Value => false;
}
```

OR by alternating the base class / interface.

```cs
[DI_Install]
public interface IManager
...
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

Injection works similarly and an instance can be assigned to a Field, a Method, or both.
Injecting once:

```cs
public class User
{
    // Injects only if somewhere TrueManager has been installed
    [DI_Inject(typeof(TrueManager))]
    private IManager _manager;

    public User()
    {
        DI_Binder.Bind(this);
    }

    [DI_Inject]
    private void OnInject(IManager manager)
    {
        if (manager.Value)
        {   // Outputs if somewhere TrueManager has been installed
            UnityEngine.Debug.Log("Received TrueManager");
        }
    }
}
```

Injecting updated:

```cs
public class User
{
    // Injects only if somewhere IManager has been installed
    [DI_Update]
    private IManager _manager;

    public User()
    {
        DI_Binder.Bind(this);
    }

    [DI_Update]
    private void OnInject(TrueManager manager)
    {
        if (!manager.Value)
        {   // Never happens
            UnityEngine.Debug.Log("Received FalseManager");
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
