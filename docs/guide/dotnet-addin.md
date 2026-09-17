# 9. How to Create a .NET Addin for E3D

## What it is

An addin is an assembly E3D loads at startup because its name appears in the module's addins XML file. It implements one interface, and from that moment it is part of the application: it can register commands, create docked windows, subscribe to model events, and install pseudo attribute handlers.

No PML anywhere.

```
E3D starts
   │
   ├─ reads DesignAddins.xml
   │     └─ loads MyCompany.E3D.Tools.dll
   │           └─ finds the IAddinInjected class
   │                 └─ calls Start(resolver)
   │                       ├─ creates a DockedWindow
   │                       ├─ registers Commands
   │                       └─ subscribes to CurrentElementChanged
   │
   └─ reads DesignCustomization.xml
         └─ loads MyTools.uic
               └─ a button whose Command key is "MyCompany.ShowTools"
```

## Why it matters

This is how AVEVA's own functionality is built. If you want your tool to behave like a native part of E3D — on a toolbar, in a menu, docked beside the model tree, present from startup — this is the way.

## The three pieces

| Piece | Interface / base class | Job |
| --- | --- | --- |
| Addin | `IAddinInjected` | Runs at startup, wires everything together |
| Command | `Command` | The behaviour a toolbar button triggers |
| User control | `UserControl` | What the docked window contains |

The command does not have to show UI, and the addin does not have to register a command. The minimum useful addin is a `Start` method that registers a pseudo attribute handler and nothing else.

## `IAddinInjected` versus `IAddin`

E3D has two addin interfaces:

```csharp
// The original
public interface IAddin
{
    void Start(ServiceManager serviceManager);
    void Stop();
    string Name { get; }
    string Description { get; }
}

// The dependency-injected version
public interface IAddinInjected
{
    void Start(IDependencyResolver resolver);
}
```

`IAddinInjected` does not redeclare `Stop`, `Name` and `Description` — it is used alongside them. In practice an E3D addin implements `IAddinInjected` and supplies all five members, so it works whichever way the host calls it:

```csharp
public class MyAddin : IAddinInjected
{
    public string Name { get { return "MyAddin"; } }
    public string Description { get { return "..."; } }

    public void Start(IDependencyResolver resolver) { /* real work */ }
    public void Start(ServiceManager serviceManager) { /* left empty */ }
    public void Stop() { }
}
```

**Use `IAddinInjected` for new code.** The `ServiceManager` overload stays empty. That is exactly what AVEVA's own E3D samples do.

Services are resolved by interface, not by concrete type:

```csharp
IWindowManager windows = DependencyResolver.GetImplementationOf<IWindowManager>();
ICommandManager commands = DependencyResolver.GetImplementationOf<ICommandManager>();
```

Either the static `DependencyResolver` or the `resolver` passed to `Start` works. The resolver argument is preferable — it is easier to substitute in a test.

::: warning PDMS-era code uses the other shape
Older material shows `serviceManager.GetService(typeof(CommandManager))` and casts to the concrete `CommandManager` / `WindowManager` classes. Those types still exist, but in E3D the interface-based resolution is the supported route.
:::

## A complete addin

```csharp
using System;
using Aveva.ApplicationFramework;
using Aveva.ApplicationFramework.Presentation;
using Aveva.Core.Database;

namespace MyCompany.E3D.Tools
{
    public class AttributeBrowserAddin : IAddinInjected
    {
        private const string WindowKey = "MyCompany.E3D.Tools.AttributeBrowser";

        private DockedWindow mWindow;
        private AttributeListControl mControl;

        public string Name
        {
            get { return "AttributeBrowserAddin"; }
        }

        public string Description
        {
            get { return "Shows the attributes of the current element"; }
        }

        public void Start(IDependencyResolver resolver)
        {
            IWindowManager windowManager =
                resolver.GetImplementationOf<IWindowManager>();

            mControl = new AttributeListControl();

            mWindow = windowManager.CreateDockedWindow(
                WindowKey,
                "Attributes",
                mControl,
                DockedPosition.Right);

            mWindow.Width = 260;

            // Windows created at startup should remember where the user put them.
            mWindow.SaveLayout = true;

            ICommandManager commandManager =
                resolver.GetImplementationOf<ICommandManager>();

            commandManager.Commands.Add(new ShowAttributeBrowserCommand(mWindow));

            CurrentElement.CurrentElementChanged += OnCurrentElementChanged;
        }

        // Required by the host, intentionally empty for an injected addin.
        public void Start(ServiceManager serviceManager)
        {
        }

        public void Stop()
        {
            CurrentElement.CurrentElementChanged -= OnCurrentElementChanged;
        }

        private void OnCurrentElementChanged(object sender,
                                             CurrentElementChangedEventArgs e)
        {
            DbElement element = CurrentElement.Element;
            if (!element.IsValid) return;

            mWindow.Title = "Attributes of "
                + element.GetAsString(DbAttributeInstance.FLNM);

            mControl.Clear();

            foreach (DbAttribute attribute in element.GetAttributes())
            {
                try
                {
                    mControl.AddAttribute(attribute.Name,
                                          element.GetAsString(attribute));
                }
                catch (Exception)
                {
                    // Some attributes are invalid for some element states.
                    // Skipping is correct here; a browser is best-effort.
                }
            }
        }
    }
}
```

**Unsubscribe in `Stop()`.** An addin that leaves handlers attached keeps its objects alive and keeps running against a model it no longer owns.

## The command

A command is a named unit of behaviour. Toolbar buttons, menu items and PML all trigger it by `Key`.

```csharp
using System;
using Aveva.ApplicationFramework;
using Aveva.ApplicationFramework.Presentation;

namespace MyCompany.E3D.Tools
{
    public class ShowAttributeBrowserCommand : Command
    {
        private readonly DockedWindow mWindow;

        public ShowAttributeBrowserCommand(DockedWindow window)
        {
            // Must be unique across every loaded addin.
            this.Key = "MyCompany.E3D.Tools.ShowAttributeBrowser";

            mWindow = window;
            mWindow.Closed += OnWindowClosed;

            DependencyResolver.GetImplementationOf<IWindowManager>()
                .WindowLayoutLoaded += OnWindowLayoutLoaded;
        }

        private void OnWindowLayoutLoaded(object sender, EventArgs e)
        {
            // Match the toggle state to the restored layout.
            this.Checked = mWindow.Visible;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            this.Checked = false;
        }

        public override void Execute()
        {
            if (this.Checked)
                mWindow.Show();
            else
                mWindow.Hide();
        }
    }
}
```

### Command keys

**Rule — the key must be globally unique.** It is matched by string against every loaded addin's commands. Namespace it with your company and assembly name. A collision means one command silently shadows the other.

### Command state

| Member | Used by |
| --- | --- |
| `Checked` | Toggle buttons — `StateButtonTool` |
| `Enabled` | Greying a tool out |
| `Visible` | Hiding a tool entirely |
| `Value` | Text or selection shown on the tool (combo boxes, labels) |
| `List` | The items in a `ComboBoxTool` or `ListTool` |

A command driving a combo box populates `List` in its constructor and reads `Value` in `Execute()`:

```csharp
public ExportCommand()
{
    this.Key = "MyCompany.E3D.Tools.Export";
    this.List.Add("Excel");
    this.List.Add("CSV");
    this.List.Add("XML");
    this.Value = "Excel";
}

public override void Execute()
{
    string format = (string)this.Value;
    // ...
}
```

## Windows

`IWindowManager` creates three kinds:

```csharp
// Docked at an edge, like the model explorer
DockedWindow docked = windowManager.CreateDockedWindow(
    key, "Title", control, DockedPosition.Right);

// Tabbed with other windows in the same position
TabbedWindow tabbed = windowManager.CreateTabbedWindow(
    key, "Title", control, DockedPosition.Left, image);

// A document window in the MDI area
MdiWindow mdi = windowManager.CreateMdiWindow(key, "Title", control);
```

`DockedPosition` is `Left`, `Right`, `Top`, `Bottom` or `Floating`.

Set `SaveLayout = true` on anything created at startup, so the user's arrangement survives a restart.

::: tip Create the window, do not show it
`CreateDockedWindow` makes the window; it does not force it visible. Let the command and the saved layout decide. An addin that pops its window open on every startup gets uninstalled.
:::

## Registration

### 1. The addins XML

Each module has one: `DesignAddins.xml`, `DraftAddins.xml`, and so on. Copy the module's file into your deployment folder, add your entry, and point E3D at your copy.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ArrayOfString xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <string>ExplorerAddin</string>
  <string>DrawListAddin</string>
  <!-- ... the rest of the AVEVA entries ... -->
  <string>C:\AVEVA\Custom\MyCompany.E3D.Tools</string>
</ArrayOfString>
```

No `.dll` extension. A bare name resolves against the executable folder; a full path is used as-is.

### 2. The `.uic` file

A `.uic` holds tool definitions — buttons, menu items, their icons, and the command key each one triggers. You produce it from inside E3D:

1. Right-click an empty part of a toolbar → **Customize…**
2. Pick or create the **Active Customization File**.
3. Create a command bar (right-click in the left pane).
4. Create a button on it (right-click in the middle pane).
5. In the properties pane, set the caption, the icon, and the **command key** — the same string as your command's `Key`.

### 3. The customization XML

Register the `.uic` by adding a line to your copy of `<Module>Customization.xml`:

```xml
<UICustomizationSet>
  <UICustomizationFiles>
    <CustomizationFile Name="Module" Path="design.uic" />
    <!-- ... -->
    <CustomizationFile Name="MyTools" Path="C:\AVEVA\Custom\MyTools.uic" />
  </UICustomizationFiles>
</UICustomizationSet>
```

## Startup order and failure

Addins start in the order they appear in the XML. Two consequences:

- **Do not depend on another addin having started**, unless it is above you in the file and you are prepared to defend that.
- **An exception in `Start` is bad.** Depending on the host build it either kills E3D startup or disables your addin with a message the user will not read.

Defend the whole method:

```csharp
public void Start(IDependencyResolver resolver)
{
    try
    {
        // ... setup ...
    }
    catch (Exception ex)
    {
        Console.WriteLine("MyAddin failed to start: " + ex);
        // Degrade quietly - do not take the application down with you.
    }
}
```

## Debugging startup

Breakpoints in `Start` need E3D launched from Visual Studio, because attaching after the fact is already too late. See [Chapter 3](./project-setup).

When the addin does not appear at all, check in this order:

1. Is the DLL where the XML says it is?
2. Does the XML entry omit the `.dll` extension?
3. Is E3D reading **your** copy of the addins XML, not AVEVA's?
4. Is the build x86 and .NET Framework 4.7.2?
5. Does the class implement `IAddinInjected` and is it `public`?
6. Did `Start` throw? Watch the console window.

## Running PML from an addin

Sometimes the shortest path to an existing capability is the command line:

```csharp
using Aveva.Core.Utilities.CommandLine;

Command.CreateCommand("SHOW !!MyForm").Run();
```

Useful for reaching functionality that only exists in PML. Do not build a whole application this way — string-built command lines fail at runtime, not at compile time.

## Rules and gotchas

**Rule — command keys are unique, globally.** Namespace them.

**Rule — unsubscribe in `Stop()`.**

**Rule — `Start` must not throw.**

**Gotcha — `Start(ServiceManager)` stays empty** when you implement `IAddinInjected`. Putting your setup in both means it runs twice.

**Gotcha — edits to AVEVA's XML are lost on upgrade.** Always work from your own copy.

**Gotcha — `SaveLayout` and a changed window key.** Change the key and the user's saved layout no longer matches; the window comes back in its default position. Pick the key once.

**Gotcha — one addin class per assembly.** The host looks for the addin type in the assembly. Two candidates is ambiguous. Split them, or make one addin register everything.

## Checklist

- [ ] The class is `public` and implements `IAddinInjected`.
- [ ] `Name`, `Description`, `Stop()` and both `Start` overloads are present.
- [ ] Services are resolved with `GetImplementationOf<IWindowManager>()` and friends.
- [ ] Every command `Key` is namespaced and unique.
- [ ] Windows created at startup set `SaveLayout = true`.
- [ ] Event handlers attached in `Start` are detached in `Stop`.
- [ ] `Start` is wrapped so it cannot take E3D down.
- [ ] Your own copies of `<Module>Addins.xml` and `<Module>Customization.xml` are in the deployment folder.
- [ ] The `.uic` command key matches the command's `Key` exactly.

## Next

[Chapter 10](./pseudo-uda) uses an addin for something PML cannot do at all: computing an attribute's value in C# whenever anything asks for it.
