using System;
using ConsoleAppFramework;
using CSharpZxScript;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);