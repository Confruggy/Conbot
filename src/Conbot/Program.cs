using System;

using Conbot;

Environment.CurrentDirectory = AppContext.BaseDirectory;
await new Startup().StartAsync();
