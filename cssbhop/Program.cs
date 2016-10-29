using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using Mono.Options;

namespace cssbhop
{
	public class Program
	{
		#region General variables
		private static GameProcess Game = null;
		public static bool UpdateNeeded = false;
		#endregion

		#region Startup method
		public static void Main(string[] args)
		{
			Monitor monitor = new Monitor("Time it took to execute threads: {ms}ms", true);
			monitor?.Start();

			OptionSet options = new OptionSet()
				.Add("monitor|monitoring", "Show how long it takes to execute stuff.",
				(string dummy) =>
				{
					General.Monitoring = true;
					Console.WriteLine("Performance monitoring enabled.");
				});

			options.Parse(args);

			Game = new GameProcess();

			Console.Title = string.Format("Autobhop tool ~ {0}", General.Version);
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine($"https://github.com/{General.Repository}" + Environment.NewLine);
			AutoUpdater.StartUpdate();

			bool bFound = false;

			foreach(Process process in Process.GetProcesses())
			{
				try
				{
					if(process.MainModule.FileName.Contains("hl2.exe"))
					{
						using(ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
						{
							foreach(ManagementObject manager in searcher.Get())
							{
								Game.CommandLine += manager["CommandLine"] + " ";
							}
						}

						Game.Name = process.MainWindowTitle;
						Game.Path = process.MainModule.FileName;
						Game.Process = process;
						Game.Insecure = Game.CommandLine.Contains("-insecure");
						Game.ProcessID = process.Id;
						Game.ProcessHandler = process.Handle;

						foreach(ProcessModule module in Game.Process.Modules)
						{
							if(module.ModuleName.Equals("client.dll"))
							{
								Game.ClientDLL = (int)module.BaseAddress;
								Console.WriteLine("client.dll ~ 0x{0}", Game.ClientDLL.ToString("X"));
							}

							else if(module.ModuleName.Equals("vguimatsurface.dll"))
							{
								Game.VGUIDLL = (int)module.BaseAddress;
								Console.WriteLine("vguimatsurface.dll ~ 0x{0}", Game.VGUIDLL.ToString("X"));
							}
						}

						Game.LocalPlayerAddress = Game.ReadInt(Game.ClientDLL + Offsets.LocalPlayer);

						Console.WriteLine("\nFound hl2.exe ({0} ~ PID {1})", Game.Name, Game.ProcessID);

						if(!Game.Insecure)
						{
							Console.WriteLine("\t- Running without -insecure, terminating game to prevent VAC bans.");

							Game.Process.Kill();
							Game.Process.WaitForExit();
						}

						else
						{
							Game.StartThread();
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
				Console.WriteLine("ERROR: Could not find hl2.exe");
			}

			else if(!Game.Insecure)
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

			Game.KillThreads();
		}
		#endregion
	}
}
