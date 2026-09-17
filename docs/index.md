---
layout: home

hero:
  name: PML .NET for E3D
  text: Write C# that AVEVA Everything3D can actually load
  tagline: A practical, example-driven guide to .NET customisation for AVEVA E3D 3.1 — from your first PMLNetCallable class to addins, pseudo UDAs and standalone tools.
  actions:
    - theme: brand
      text: Start the guide
      link: /guide/introduction
    - theme: alt
      text: API cheatsheet
      link: /reference/api-cheatsheet
    - theme: alt
      text: Claude skill
      link: /skill

features:
  - title: Verified against a real install
    details: Every namespace, class and signature in this guide was read out of the assemblies shipped with AVEVA E3D 3.1, not copied from an older PDMS manual.
  - title: Eleven chapters, one workflow
    details: Each chapter follows the same shape — what it is, why it matters, a minimal working example, the rules that bite, and a checklist before you ship.
  - title: Templates you can compile
    details: Five starter projects for the five things people actually build — callable class, user control, addin, pseudo UDA and standalone executable.
  - title: Teaches Claude too
    details: The same content is packaged as a Claude Code skill, so an AI assistant can scaffold and review PML .NET projects against the same rules you follow.
---

## The short version

AVEVA E3D ships a .NET API. You can use it in four ways:

| Approach | PML needed | You get |
| --- | --- | --- |
| **PMLNetCallable class** | Yes | A C# class that behaves like a native PML object |
| **PMLNetCallable user control** | Yes | A WinForms control hosted on a PML form |
| **.NET addin** | No | Code loaded by E3D at startup, with its own commands and docked windows |
| **Standalone application** | No | An executable outside E3D with read/write access to the project databases |

Everything else in this guide is detail on those four.

## Non-negotiables

If you get only four things right, get these:

1. **Target .NET Framework 4.7.2 and platform x86.** E3D 3.1 is a 32-bit CLR host. AnyCPU or x64 assemblies fail to load, usually silently.
2. **Reference the DLLs in place** from your E3D installation folder, and never copy them into your repository.
3. **Mark everything you expose**: the class, every constructor, the `Assign` method, and each method or property PML calls need `[PMLNetCallable]`.
4. **Restart the module after rebuilding.** Once E3D imports an assembly it locks the file until the process exits.

Chapter 3 explains each of these in full.

## Who this is for

Engineers and designers who already know E3D and PML, and want compiled code where PML runs out of road — file and network access, external systems, richer UI, or heavy loops over the database.

You need working C# — classes, properties, events, generics at a reading level. You do not need prior AVEVA API experience.

::: info Not an AVEVA publication
This is an independent community guide. It is not produced, reviewed or endorsed by AVEVA. Always check your own installation's documentation and API help files, which are the authority for your version.
:::
