# 1. Introduction

## What this guide is

A working manual for writing C# against the AVEVA Everything3D .NET API, aimed at people who already use E3D and PML and now need compiled code.

It covers the whole path: setting up a project that E3D will actually load, exposing C# to PML, reading and writing the database, collecting elements with filters, building UI, and running outside E3D entirely.

## Why bother, when PML exists

PML is fine for most customisation. Reach for .NET when you hit one of these walls:

| Problem | Why PML struggles | What .NET gives you |
| --- | --- | --- |
| Talking to another system | No HTTP client, no message queues, no database drivers | The whole .NET base class library |
| Reading or writing real file formats | Text only | Excel, XML, JSON, PDF, ZIP, anything with a library |
| Heavy iteration over the model | Interpreted, one element at a time | Compiled loops, typed collections, filters evaluated in the core |
| Richer UI | Fixed gadget set | Any WinForms control, hosted on a PML form or docked in E3D |
| Logic you want tested and versioned | Hard to unit test | Ordinary C# projects, ordinary tooling |

The reverse is also true. PML is quicker to change, needs no build step, and does not lock a DLL for the life of the session. Plenty of good customisation is PML calling one small .NET object.

## What you need

- **AVEVA E3D 3.1** installed, with a project you can log into. This guide assumes the default install path `C:\AVEVA\Plant\Everything3D3.1`.
- **Visual Studio 2019 or later**, or the .NET Framework build tools, with the **.NET Framework 4.7.2 targeting pack**.
- **Working C#**: classes, properties, events, interfaces, delegates, generics at a reading level.
- **Working PML**: forms, methods, `!variables`, and how `pml rehash` works.

You do not need previous AVEVA API experience. You do need the ability to restart E3D repeatedly, because you will be doing it a lot.

## Versions, and why they matter here

Most .NET customisation material you will find online was written for **PDMS 12.x**, where the assemblies were named `Aveva.Pdms.Database`, `Aveva.Pdms.Standalone` and so on.

**E3D renamed them.** The equivalents are `Aveva.Core.Database`, `Aveva.E3D.Standalone`, and friends. The classes and method signatures are largely unchanged, but the `using` lines and assembly references are not. Code copied from a PDMS-era guide will not compile against E3D without that translation.

Every namespace, type and signature in this guide was read directly out of the assemblies in an E3D 3.1 installation. [Chapter 4](./api-and-dlls) gives the full old-to-new mapping.

## How the guide is structured

| Chapters | What you get |
| --- | --- |
| 2–4 | The four integration models, a project that loads, and the map of which DLL holds what |
| 5–7 | The core skills: exposing classes to PML, the database interface, collections and filters |
| 8–11 | The four things people build: user controls, addins, pseudo UDAs, standalone applications |

Every chapter follows the same shape:

1. **What it is** — the concept in a paragraph.
2. **Why it matters** — when you would choose this over the alternatives.
3. **A minimal working example** — complete enough to compile and run.
4. **Rules and gotchas** — the things that cost you an afternoon.
5. **Checklist** — what to verify before you call it done.

## Conventions

- `%AVEVA_DESIGN_EXE%` refers to your E3D executable folder — `C:\AVEVA\Plant\Everything3D3.1` unless your site moved it.
- Code shown is C# 7.3 (the highest version .NET Framework 4.7.2 projects support by default) unless stated otherwise.
- PML snippets are shown as typed into the E3D command window.
- Anything marked **rule** is a hard constraint of the platform, not a style preference.

::: warning This is not official documentation
This guide is written by users, for users. AVEVA's own .NET Customisation User Guide and the API help files shipped with your installation are the authority. Where this guide and your installation's documentation disagree, believe your installation.
:::

## Next

[Chapter 2](./dotnet-customisation-overview) lays out the four ways .NET code can attach itself to E3D, so you can pick the right one before writing any code.
