using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;

namespace cssbhop
{
	public class Program
	{
		private static GameProcess gGP_Game = null;

		public static void Main()
		{
			gGP_Game = new GameProcess();

			Console.Title = "Autobhop tool ~ 1.1";
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine("https://github.com/shavitush/cssbhop\n");

			bool bFound = false;

			foreach(Process process in Process.GetProcesses())
			{
				try
				{
					if(process.MainModule.FileName.Contains("hl2.exe"))
					{
						using(ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
						{
							foreach(ManagementObject a in searcher.Get())
							{
								gGP_Game.CommandLine += a["CommandLine"] + " ";
							}
						}

						gGP_Game.Name = process.MainWindowTitle;
						gGP_Game.Path = process.MainModule.FileName;
						gGP_Game.Process = process;
						gGP_Game.Insecure = gGP_Game.CommandLine.Contains("-insecure");
						gGP_Game.ProcessID = process.Id;
						gGP_Game.ProcessHandler = process.Handle;

						foreach(ProcessModule module in gGP_Game.Process.Modules)
						{
							if(module.ModuleName.Equals("client.dll"))
							{
								gGP_Game.ClientDLL = (int)module.BaseAddress;
								Console.WriteLine("client.dll ~ 0x{0}", gGP_Game.ClientDLL.ToString("X"));
							}

							else if(module.ModuleName.Equals("vguimatsurface.dll"))
							{
								gGP_Game.VGUIDLL = (int)module.BaseAddress;
								Console.WriteLine("vguimatsurface.dll ~ 0x{0}", gGP_Game.VGUIDLL.ToString("X"));
							}
						}

						gGP_Game.LocalPlayerAddress = gGP_Game.ReadInt(gGP_Game.ClientDLL + Offsets.LocalPlayer);

						Console.WriteLine("\nFound hl2.exe ({0} ~ PID {1})", gGP_Game.Name, gGP_Game.ProcessID);

						if(!gGP_Game.Insecure)
						{
							Console.WriteLine("\t- Running without -insecure, terminating game to prevent VAC bans.");

							gGP_Game.Process.Kill();
							gGP_Game.Process.WaitForExit();
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

			Console.WriteLine("Write \"bye\" and press <ENTER> to exit.");

			if(!bFound)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Could not find hl2.exe");
			}

			else if(!gGP_Game.Insecure)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("\t- ERROR: Running without -insecure.");
			}

			Console.ForegroundColor = ConsoleColor.White;

			while(!Console.ReadLine().Equals("bye"))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("ERROR: Invalid input.\n");
				Console.ForegroundColor = ConsoleColor.White;
			}

			gGP_Game.KillThreads();
		}
	}
}
