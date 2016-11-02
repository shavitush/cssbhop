using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using Timer = System.Timers.Timer;

namespace cssbhop
{
	internal class GameProcess : IDisposable
	{
		/// <summary>
		/// A value to know when memory reads fail.
		/// </summary>
		public const int MemoryReadFailed = int.MinValue;

		/// <summary>
		/// Window title for the game.
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// Path to the game's process.
		/// </summary>
		public string Path
		{
			get;
			set;
		}

		/// <summary>
		/// File name for the game.
		/// For example, hl2.exe for most Source Engine games.
		/// </summary>
		public string ProcessName
		{
			get;
			set;
		} = "<PROCESS>";

		/// <summary>
		/// Command line parameters.
		/// </summary>
		public string CommandLine
		{
			get;
			set;
		}

		/// <summary>
		/// Running with -insecure?
		/// </summary>
		public bool Insecure
		{
			get;
			set;
		}

		/// <summary>
		/// Process object.
		/// </summary>
		public Process Process
		{
			get;
			set;
		} = null;

		/// <summary>
		/// Process ID.
		/// </summary>
		public int ProcessID
		{
			get;
			set;
		}

		/// <summary>
		/// Handler to the game process.
		/// </summary>
		public IntPtr ProcessHandler
		{
			get;
			set;
		} = IntPtr.Zero;

		/// <summary>
		/// Address for the client.dll module.
		/// </summary>
		public int ClientDLL
		{
			get;
			set;
		}

		/// <summary>
		/// Address for the vguimatsurface.dll module.
		/// </summary>
		public int VGUIDLL
		{
			get;
			set;
		}

		/// <summary>
		/// Address to localplayer.
		/// Combined with client.dll.
		/// </summary>
		public int LocalPlayerAddress
		{
			get;
			set;
		}

		/// <summary>
		/// Cheat thread.
		/// </summary>
		public Thread Thread
		{
			get;
			set;
		}

		/// <summary>
		/// Keep-alive thread. (Is the game closed?)
		/// </summary>
		private Timer KeepAlive
		{
			get;
			set;
		}

		/// <summary>
		/// Performance monitor.
		/// </summary>
		private Monitor Monitor
		{
			get;
		}

		/// <summary>
		/// List of the last performance measurements.
		/// </summary>
		private List<long> MonitorList
		{
			get;
		}

		/// <summary>
		/// Class constructor.
		/// </summary>
		public GameProcess()
		{
			if(!General.Monitoring)
			{
				return;
			}

			Monitor = new Monitor(string.Empty);
			MonitorList = new List<long>();
		}

		/// <summary>
		/// Starts the needed threads.
		/// </summary>
		public void StartThread()
		{
			// Starts the cheat thread.
			Thread = new Thread(CheatTread);
			Thread.Start();

			// Starts the keep-alive timer, to make sure the game is open.
			KeepAlive = new Timer();
			KeepAlive.Elapsed += OnTimedEvent;
			KeepAlive.Interval = 2500;
			KeepAlive.Enabled = true;
		}

		/// <summary>
		/// Kills the cheat related threads and timers.
		/// </summary>
		public void KillThreads()
		{
			try
			{
				Thread.Abort();
				KeepAlive.Close();
			}

			catch(Exception)
			{
				Console.WriteLine("ERROR CLOSING THREADS!");
			}
		}

		/// <summary>
		/// The cheat thread itself.
		/// </summary>
		private void CheatTread()
		{
			ulong i = 0;

			while(true)
			{
				Monitor?.Start();

				if((++i % 1000) == 0)
				{
					LocalPlayerAddress = ReadInt(ClientDLL + Offsets.LocalPlayer);

					if(MonitorList?.Count > 0)
					{
						Console.WriteLine("Last 1000 actions averaged at {0}ms.", Convert.ToInt64(MonitorList.Average()));
						MonitorList.Clear();
					}
				}

#if DEBUG
                Thread.Sleep(250);
#else
				Thread.Sleep(1);
#endif

#if DEBUG
                Console.WriteLine($"Team? {Team} Alive? {LifeState} can jump {CanJump} in water {InWater}");
#endif

				bool bSpaceHeld = ((GetAsyncKeyState(Keys.Space) & (1 << 16)) > 0);

				if(bSpaceHeld)
				{
					WriteInt(ClientDLL + Offsets.JumpAddress, 4);
				}

				if((!CanJump && !InWater) || !Alive || Team < 2 || Paused)
				{
					if(Monitor != null)
					{
						Monitor.Stop();
						MonitorList?.Add(Monitor.ElapsedMilliseconds);
						Monitor.Reset();
					}

					continue;
				}

				// 5 - jumping, 4 - not
				WriteInt(ClientDLL + Offsets.JumpAddress, 5);

				Thread.Sleep(25);
#if DEBUG
                Console.WriteLine($"1: {ReadInt(ClientDLL + Offsets.JumpAddress)}");
#endif
			}
		}

		/// <summary>
		/// Verifies if the game is open.
		/// </summary>
		private void OnTimedEvent(object source, ElapsedEventArgs e)
		{
			if(!Running)
			{
				Console.WriteLine(Environment.NewLine + "\t- Detected game closing. Cheat shut down.");

				Thread.Abort();
				KeepAlive.Close();
			}
		}

		/// <summary>
		/// Returns true if the game process is still open.
		/// </summary>
		public bool Running
		{
			get
			{
				if(Process == null)
				{
					throw new NullReferenceException($"Unable to find the process {ProcessName} (PID: {ProcessID}).");
				}

				try
				{
					Process.GetProcessById(ProcessID);
				}

				catch(ArgumentException)
				{
					return false;
				}

				return true;
			}
		}

		/// <summary>
		/// Returns the key state of a given virtual key.
		/// </summary>
		/// <param name="vKey">Virtual key to check for.</param>
		/// <returns>Key state.</returns>
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(Keys vKey);

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/kernel32.readprocessmemory
		/// </summary>
		/// <param name="hProcess">Handler to process.</param>
		/// <param name="lpBaseAddress">Addrress to read from.</param>
		/// <param name="lpBuffer">Buffer to save read memory on.</param>
		/// <param name="dwSize">Size of the memory we read.</param>
		/// <param name="lpNumberOfBytesRead">Reference to data we successfully read.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/kernel32.readprocessmemory
		/// </summary>
		/// <param name="hProcess">Handler to process.</param>
		/// <param name="lpBaseAddress">Addrress to write to.</param>
		/// <param name="lpBuffer">Buffer that includes the data we want to write.</param>
		/// <param name="nSize">Size of the memory we write.</param>
		/// <param name="lpNumberOfBytesWritten">Reference to data we successfully wrote.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

		/// <summary>
		/// Reads a memory value from a given address.
		/// </summary>
		/// <param name="address">Address to read from.</param>
		/// <returns>Value of the memory address we asked to read.</returns>
		public int ReadInt(int address)
		{
			var bData = new byte[4];
			IntPtr ipBytesRead;

			if(!ReadProcessMemory(ProcessHandler, (IntPtr)address, bData, bData.Length, out ipBytesRead) || ipBytesRead.ToInt32() != 4)
			{
				return MemoryReadFailed;
			}

			return BitConverter.ToInt32(bData, 0);
		}

		/// <summary>
		/// Writes a value to a specified memory address.
		/// </summary>
		/// <param name="address">Address to write to.</param>
		/// <param name="value">Integer value to write.</param>
		/// <returns>True if written or false if not.</returns>
		public bool WriteInt(int address, int value)
		{
			var bData = BitConverter.GetBytes(value);
			IntPtr ipBytesWritten;

			if(!WriteProcessMemory(ProcessHandler, (IntPtr)address, bData, bData.Length, out ipBytesWritten) || ipBytesWritten.ToInt32() != 4)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the player's health.
		/// </summary>
		public int Health => ReadInt(LocalPlayerAddress + Offsets.Health);

		/// <summary>
		/// Gets the player's team.
		/// </summary>
		public int Team => ReadInt(LocalPlayerAddress + Offsets.Team);

		/// <summary>
		/// Gets the player's movetype.
		/// </summary>
		public int MoveType => ReadInt(LocalPlayerAddress + Offsets.MoveType);

		/// <summary>
		/// Gets the lifestate, 25600 is alive.
		/// </summary>
		public int LifeState => ReadInt(LocalPlayerAddress + Offsets.Lifestate);

		/// <summary>
		/// Checks if the player is alive.
		/// </summary>
		public bool Alive => (LifeState == 25600);

		/// <summary>
		/// Gets the player's ground entity.
		/// </summary>
		public int GroundEntity => ReadInt(LocalPlayerAddress + Offsets.GroundEntity);

		/// <summary>
		/// Checks if the player is on a ladder.
		/// </summary>
		public bool OnLadder => (MoveType == 9); // 9 is ladder!

		/// <summary>
		/// Gets the player's flags.
		/// </summary>
		public int Flags => ReadInt(LocalPlayerAddress + Offsets.Flags);

		/// <summary>
		/// Is the player inside water?
		/// </summary>
		public bool InWater => ((Flags & (1 << 9)) > 0);

		/// <summary>
		/// Is the game paused?
		/// </summary>
		public bool Paused => (ReadInt(Offsets.ChatOpen) == 1 || ReadInt(VGUIDLL + Offsets.PauseMenu) == 1);

		/// <summary>
		/// Checks if the player is able to jump which is: either on ground, on ladder or waterlevel being 2 or higher.
		/// </summary>
		public bool CanJump => (GroundEntity != -1 || OnLadder);

		#region IDisposable support
		/// <summary>
		/// Dispose related variables.
		/// </summary>
		private bool _disposed;
		private readonly SafeHandle _handle = new SafeFileHandle(IntPtr.Zero, true);

		/// <summary>
		/// Dispose the monitor.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposal implementation.
		/// </summary>
		/// <param name="disposing">Are we currently disposing?</param>
		protected virtual void Dispose(bool disposing)
		{
			if(_disposed)
			{
				return;
			}

			if(disposing)
			{
				_handle.Dispose();
				KeepAlive?.Close();
				KeepAlive?.Dispose();
				Process.Dispose();
				Monitor.Dispose();

				try
				{
					Thread?.Abort();
				}

				catch(Exception ex)
				{
					Console.WriteLine($"Faced an exception: ({ex}) while aborting threads: {ex.Message}");
				}
			}

			_disposed = true;
		}

		~GameProcess()
		{
			Dispose(false);
		}
		#endregion
	}
}
