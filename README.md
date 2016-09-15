### Build status
[![Build status](https://travis-ci.org/shavitush/cssbhop.svg?branch=master)](https://travis-ci.org/shavitush/cssbhop)

# CS:S Auto Bunnyhop (C#)
A client sided bhop cheat that doesn't let you go on VAC secured seervers.
___

* This tool is basically client sided autobunnyhop for Counter-Strike: Source, works as an external cheat and uses the Win32API functions `ReadProcessMemory` and `WriteProcessMemory`.
* It's probably VAC detected, so if you run it without using `-insecure` in your CS:S startup parameters, CS:S and the cheat will just terminate to avoid a VAC ban. (You could edit the source code to allow you; but don't risk a VAC ban, it's not worth it!)
* It could be used for purposes like creating a fully featured external C# cheat for Source Engine games or even simply port to CS:GO.

Structure
--
* `Program.cs` - initializes the thread, `GameProcess` class and grabs modules.
* `Offsets.cs` - has memory addresses and offsets, they'll probably break through updates.
* `GameProcess.cs` - a simple to use and fully commented class for cheat usage.

License
--
GNU GPL v3, see **[LICENSE](https://github.com/shavitush/cssbhop/blob/master/LICENSE)**.
