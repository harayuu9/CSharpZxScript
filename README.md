SCharpZxScript
===
You can easily write CSharpScript using the Zx function of [ProcessX](https://github.com/Cysharp/ProcessX).

![image](https://user-images.githubusercontent.com/24310162/130572603-f13cf336-43c4-4e29-93ed-75b132e5718a.png)
![image](https://user-images.githubusercontent.com/24310162/130572747-50e37590-ac34-4ea6-a389-d78af796fb5a.png)

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [Getting Started](#getting-started)

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
![image](https://user-images.githubusercontent.com/24310162/130572747-50e37590-ac34-4ea6-a389-d78af796fb5a.png)
![image](https://user-images.githubusercontent.com/24310162/130572603-f13cf336-43c4-4e29-93ed-75b132e5718a.png)

