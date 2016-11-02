using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using Mono.Options;

namespace cssbhop
{
	public class Program
	{
		#region Game related settings
		private const string ProcessName = "hl2.exe";
		#endregion

		#region General variables
		private static GameProcess _game;
		public static bool UpdateNeeded = false;
		#endregion

		#region Startup method
		public static void Main(string[] args)
		{
			var monitor = new Monitor("Time it took to execute threads: {ms}ms", true);
			monitor.Start();

			var options = new OptionSet()
				.Add("monitor|monitoring", "Show how long it takes to execute stuff.",
				dummy =>
				{
					General.Monitoring = true;
					Console.WriteLine("Performance monitoring enabled.");
				});

			options.Parse(args);

			_game = new GameProcess();

			Console.Title = $"Autobhop tool ~ {General.Version}";
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"https://github.com/{General.Repository}" + Environment.NewLine);
			AutoUpdater.StartUpdate();

			bool bFound = false;

			foreach(var process in Process.GetProcesses())
			{
				try
				{
					if(!process.MainModule.ModuleName.Equals(ProcessName))
					{
						continue;
					}

					using(var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessID = {process.Id}"))
					{
						foreach(var manager in searcher.Get())
						{
							_game.CommandLine += manager["CommandLine"] + " ";
						}
					}

					_game.Name = process.MainWindowTitle;
					_game.Path = process.MainModule.FileName;
					_game.ProcessName = process.MainModule.ModuleName;
					_game.Process = process;
					_game.Insecure = _game.CommandLine.Contains("-insecure");
					_game.ProcessID = process.Id;
					_game.ProcessHandler = process.Handle;

					foreach(ProcessModule module in _game.Process.Modules)
					{
						if(module.ModuleName.Equals("client.dll"))
						{
							_game.ClientDLL = (int)module.BaseAddress;
							Console.WriteLine("client.dll ~ 0x" + _game.ClientDLL.ToString("X"));
						}

						else if(module.ModuleName.Equals("vguimatsurface.dll"))
						{
							_game.VGUIDLL = (int)module.BaseAddress;
							Console.WriteLine("vguimatsurface.dll ~ 0x" + _game.VGUIDLL.ToString("X"));
						}
					}

					_game.LocalPlayerAddress = _game.ReadInt(_game.ClientDLL + Offsets.LocalPlayer);

					Console.WriteLine(Environment.NewLine + $"Found {_game.ProcessName} ({_game.Name} ~ PID {_game.ProcessID})");

					if(!_game.Insecure)
					{
						Console.WriteLine("\t- Running without -insecure, terminating game to prevent VAC bans.");

						_game.Process.Kill();
						_game.Process.WaitForExit();
					}

					else
					{
						_game.StartThread();
					}

					bFound = true;
				}

				catch(Win32Exception exception)
				{
					if((uint)exception.ErrorCode != 0x80004005)
					{
						throw;
					}
				}
			}

			Console.WriteLine("Write \"bye\" and press <ENTER> to exit." + Environment.NewLine);
			Console.ForegroundColor = ConsoleColor.Red;

			if(!bFound)
			{
				Console.WriteLine($"ERROR: Could not find {ProcessName}");
			}

			else if(!_game.Insecure)
			{
				Console.WriteLine("\t- ERROR: Running without -insecure.");
			}

			Console.ForegroundColor = ConsoleColor.White;

			if(General.Monitoring)
			{
				Console.WriteLine(monitor?.ToString());
			}

			string sInput;

			while(!(sInput = Console.ReadLine()).Equals("bye"))
			{
				if(sInput.Equals("update"))
				{
					if(UpdateNeeded)
					{
						Process.Start($"https://github.com/{General.Repository}/releases/latest");
					}

					else
					{
						Console.WriteLine("There's no pending update.");
					}
				}

				else
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("ERROR: Invalid input." + Environment.NewLine);
					Console.ForegroundColor = ConsoleColor.White;
				}
			}

			_game.KillThreads();
		}
		#endregion
	}
}
