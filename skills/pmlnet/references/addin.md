# .NET Addin (CAF)

An assembly E3D loads at startup because it is listed in the module's addins XML. No PML involved.

Template: `assets/templates/Addin/`

## Interface choice

Implement **`IAddinInjected`** for new code, and supply all five members so the host can call either way:

```csharp
public class MyAddin : IAddinInjected
{
    public string Name        { get { return "MyAddin"; } }
    public string Description { get { return "..."; } }

    public void Start(IDependencyResolver resolver) { /* all setup here */ }
    public void Start(ServiceManager serviceManager) { /* intentionally empty */ }
    public void Stop() { /* unsubscribe everything Start subscribed */ }
}
```

Putting setup in both `Start` overloads runs it twice.

Resolve services by **interface**:

```csharp
IWindowManager  wm = resolver.GetImplementationOf<IWindowManager>();
ICommandManager cm = resolver.GetImplementationOf<ICommandManager>();
```

PDMS-era code uses `serviceManager.GetService(typeof(CommandManager))` and the concrete classes. Do not copy that shape into E3D code.

## Full shape

```csharp
public void Start(IDependencyResolver resolver)
{
    try
    {
        IWindowManager wm = resolver.GetImplementationOf<IWindowManager>();

        mControl = new MyControl();
        mWindow = wm.CreateDockedWindow(
            "MyCompany.E3D.Tools.MyWindow", "My Tool",
            mControl, DockedPosition.Right);
        mWindow.Width = 260;
        mWindow.SaveLayout = true;          // remember the user's layout

        ICommandManager cm = resolver.GetImplementationOf<ICommandManager>();
        cm.Commands.Add(new ShowMyToolCommand(mWindow));

        CurrentElement.CurrentElementChanged += OnCeChanged;
        mSubscribed = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine("MyAddin failed to start: " + ex);
    }
}

public void Stop()
{
    if (mSubscribed)
    {
        CurrentElement.CurrentElementChanged -= OnCeChanged;
        mSubscribed = false;
    }
}
```

`Start` must never throw — an exception there can take E3D startup down.

## Command

```csharp
public class ShowMyToolCommand : Command
{
    private readonly DockedWindow mWindow;

    public ShowMyToolCommand(DockedWindow window)
    {
        this.Key = "MyCompany.E3D.Tools.ShowMyTool";   // globally unique
        mWindow = window;
        mWindow.Closed += (s, e) => this.Checked = false;
        DependencyResolver.GetImplementationOf<IWindowManager>()
            .WindowLayoutLoaded += (s, e) => this.Checked = mWindow.Visible;
    }

    public override void Execute()
    {
        if (this.Checked) mWindow.Show(); else mWindow.Hide();
    }
}
```

Command state members: `Checked` (toggle buttons), `Enabled`, `Visible`, `Value` (combo/label text), `List` (combo items).

## Windows

```csharp
wm.CreateDockedWindow(key, title, control, DockedPosition.Right);
wm.CreateTabbedWindow(key, title, control, DockedPosition.Left, image);
wm.CreateMdiWindow(key, title, control);
```

`DockedPosition`: `Left`, `Right`, `Top`, `Bottom`, `Floating`.

Set `SaveLayout = true` on startup-created windows. Do not force them visible — let the saved layout and the command decide. Changing a window `Key` later discards the user's saved layout.

## Registration (manual — tell the user)

**1. Addins XML.** Copy `<Module>Addins.xml` from `%AVEVA_DESIGN_EXE%` into your deployment folder, keep every AVEVA entry, append yours **without** the `.dll` extension:

```xml
<string>C:\AVEVA\Custom\MyCompany.E3D.Addin</string>
```

**2. `.uic` file.** Produced inside E3D: right-click a toolbar → Customize… → create a command bar and a button → set the button's command key to match `Command.Key` exactly.

**3. Customization XML.** Register the `.uic` in your copy of `<Module>Customization.xml`:

```xml
<CustomizationFile Name="MyTools" Path="C:\AVEVA\Custom\MyTools.uic" />
```

Never edit AVEVA's copies of either XML file in place.

## Running PML from an addin

```csharp
using Aveva.Core.Utilities.CommandLine;
Command.CreateCommand("SHOW !!MyForm").Run();
```

Note the name collision with `Aveva.ApplicationFramework.Presentation.Command` — qualify when both namespaces are in scope.

## Rules

- One addin class per assembly.
- Command keys globally unique, namespaced.
- `Stop()` undoes every subscription `Start` made.
- Addins start in XML order — do not depend on another addin having started.
- All API calls on the UI thread.
