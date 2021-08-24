SCharpZxScript
===
You can easily write CSharpScript using the Zx function of [ProcessX](https://github.com/Cysharp/ProcessX).

![image](https://user-images.githubusercontent.com/24310162/130572603-f13cf336-43c4-4e29-93ed-75b132e5718a.png)
![image](https://user-images.githubusercontent.com/24310162/130572747-50e37590-ac34-4ea6-a389-d78af796fb5a.png)

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

  - [Getting Started](#getting-started)
  - [Edit Script](#edit-script)
- [Settings](#settings)
  - [Use NuGet Package](#use-nuget-package)
  - [Use Reference Project](#use-reference-project)
  - [Use Reference CS](#use-reference-cs)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

Getting Started
---

First of all, CSharpZxScript requires .NET5 SDK. 
It is recommended that you register Visual Studio 2019, which includes .NET5 as the default program to open the Solution.
The easiest way to acquire and run CSharpZxScript is as a dotnet tool.

> dotnet tool install --global [CSharpZxScript](https://www.nuget.org/packages/CSharpZxScript/1.0.0?preview=1)

When the installation is complete, you can see help with the following command

> cszx -help

```
Usage: CSharpZxScript <Command>
 
Commands:
  Run, r
  Edit, e
  SettingsList, sl
  SettingsAddPackage, sapa
  SettingsRemovePackage, srpa
  SettingsAddProject, sapr
  SettingsRemoveProject, srpr
  SettingsAddCs, sac
  SettingsRemoveCs, src
  AddRightClickMenu, arc         Add Run ZxScript and Edit ZxScript to the right-click menu of .cs
  RemoveRightClickMenu, rrc      Remove right-click menu
  help                           Display help.
  version                        Display version.
```

Since the command operation is difficult, it is recommended to enter the following command and add it to the right-click Menu.

> cszx arc

You can create the .cs file you want to run and run it from the right-click menu

```test.cs
using System;
using Zx;
using static Zx.Env;

await run($"echo {"echo test"}");
await run($"echo {Environment.CurrentDirectory}");

Environment.Exit(1);
```

![image](https://user-images.githubusercontent.com/24310162/130572603-f13cf336-43c4-4e29-93ed-75b132e5718a.png)

```
"echo test"
test.cs Directory

ExitCode 1
Please any key...
```

How to delete right-click Menu.
> cszx rrc


Edit Script
---

To edit the script, select "Edit Zx Script" from the right-click menu.
The default IDE set in .sln will be launched.
And the code completion for C# works perfectly.

Settings
===

The settings apply to all scripts under the folder.
In addition it is possible to set across multiple folders.
Note that if a package with a different version is found at that time, the version of the child's folder will take precedence.
The settings are saved as [ZxScriptSettings.json] in each folder

Use NuGet Package
---

Can use the NuGet package.

Let's put System.Text.Json as an example
> C:\Scripts>cszx sapa System.Text.Json 5.0.2

edit script.
> C:\Scripts>cszx e test.cs

```test.cs
using static Zx.Env;

var strArray = new []{"A", "B", "C", "D", "E"};

await run($"echo {System.Text.Json.JsonSerializer.Serialize(strArray)}");
```
You can debug on the IDE.

> C:\Scripts>cszx r test.cs

```
["A","B","C","D","E"]
```

How to delete
> C:\Scripts>cszx srpa System.Text.Json

Use Reference Project
---

Can reference to csproj.
This allows you to share your C # library with other projects.
This means that you can use a Utility created in C # in a script.

You can register with an absolute path, but it is recommended to register with a relative path.
> C:\Scripts>cszx sapr ../../Project.Core.csproj

How to delete
> C:\Scripts>cszx srpr ../../Project.Core.csproj

Use Reference CS
---

Can reference to .cs
You can easily add a class for your script.
Similar to reference csproj, but simpler.

You can register with an absolute path, but it is recommended to register with a relative path.
> C:\Scripts>cszx sapc ScriptCore.cs

How to delete
> C:\Scripts>cszx srpr ScriptCore.cs
