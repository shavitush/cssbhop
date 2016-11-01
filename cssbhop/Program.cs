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
		private const string processName = "hl2.exe";
		#endregion

		#region General variables
		private static GameProcess game = null;
		public static bool UpdateNeeded = false;
		#endregion

		#region Startup method
		public static void Main(string[] args)
		{
			var monitor = new Monitor("Time it took to execute threads: {ms}ms", true);
			monitor?.Start();

			var options = new OptionSet()
				.Add("monitor|monitoring", "Show how long it takes to execute stuff.",
				(string dummy) =>
				{
					General.Monitoring = true;
					Console.WriteLine("Performance monitoring enabled.");
				});

			options.Parse(args);

			game = new GameProcess();

			Console.Title = $"Autobhop tool ~ {General.Version}";
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"https://github.com/{General.Repository}" + Environment.NewLine);
			AutoUpdater.StartUpdate();

			bool bFound = false;

			foreach(var process in Process.GetProcesses())
			{
				try
				{
					if(process.MainModule.ModuleName.Equals(processName))
					{
						using(var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
						{
							foreach(var manager in searcher.Get())
							{
								game.CommandLine += manager["CommandLine"] + " ";
							}
						}

						game.Name = process.MainWindowTitle;
						game.Path = process.MainModule.FileName;
						game.ProcessName = process.MainModule.ModuleName;
						game.Process = process;
						game.Insecure = game.CommandLine.Contains("-insecure");
						game.ProcessID = process.Id;
						game.ProcessHandler = process.Handle;

						foreach(ProcessModule module in game.Process.Modules)
						{
							if(module.ModuleName.Equals("client.dll"))
							{
								game.ClientDLL = (int)module.BaseAddress;
								Console.WriteLine("client.dll ~ 0x" + game.ClientDLL.ToString("X"));
							}

							else if(module.ModuleName.Equals("vguimatsurface.dll"))
							{
								game.VGUIDLL = (int)module.BaseAddress;
								Console.WriteLine("vguimatsurface.dll ~ 0x" + game.VGUIDLL.ToString("X"));
							}
						}

						game.LocalPlayerAddress = game.ReadInt(game.ClientDLL + Offsets.LocalPlayer);

						Console.WriteLine(Environment.NewLine + $"Found {game.ProcessName} ({game.Name} ~ PID {game.ProcessID})");

						if(!game.Insecure)
						{
							Console.WriteLine("\t- Running without -insecure, terminating game to prevent VAC bans.");

							game.Process.Kill();
							game.Process.WaitForExit();
						}

						else
						{
							game.StartThread();
						}

						bFound = true;
					}
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
				Console.WriteLine($"ERROR: Could not find {processName}");
			}

			else if(!game.Insecure)
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

			game.KillThreads();
		}
		#endregion
	}
}
