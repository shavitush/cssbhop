using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace cssbhop
{
	class GameProcess : IDisposable
	{
		/// <summary>
		/// A value to know when memory reads fail.
		/// </summary>
		public const int MEMORY_READ_FAILED = int.MinValue;

		/// <summary>
		/// Window title for the game.
		/// </summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>
		/// Path to hl2.exe.
		/// </summary>
		public string Path
		{
			get;
			set;
		}

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
		} = null;

		/// <summary>
		/// Keep-alive thread. (Is the game closed?)
		/// </summary>
		private System.Timers.Timer keepAlive
		{
			get;
			set;
		} = null;

		/// <summary>
		/// Performance monitor.
		/// </summary>
		private Monitor monitor
		{
			get;
			set;
		} = null;

		/// <summary>
		/// List of the last performance measurements.
		/// </summary>
		private List<long> monitorList
		{
			get;
			set;
		} = null;

		/// <summary>
		/// Class constructor.
		/// </summary>
		public GameProcess()
		{
			if(General.Monitoring)
			{
				this.monitor = new Monitor(string.Empty);
				this.monitorList = new List<long>();
			}
		}

		/// <summary>
		/// Starts the needed threads.
		/// </summary>
		public void StartThread()
		{
			// Starts the cheat thread.
			this.Thread = new Thread(new ThreadStart(this.cheatTread));
			this.Thread.Start();

			// Starts the keep-alive timer, to make sure the game is open.
			this.keepAlive = new System.Timers.Timer();
			this.keepAlive.Elapsed += new ElapsedEventHandler(this.onTimedEvent);
			this.keepAlive.Interval = 2500;
			this.keepAlive.Enabled = true;
		}

		/// <summary>
		/// Kills the cheat related threads and timers.
		/// </summary>
		public void KillThreads()
		{
			try
			{
				this.Thread.Abort();
				this.keepAlive.Close();
			}

			catch(Exception)
			{
				Console.WriteLine("ERROR CLOSING THREADS!");
			}
		}

		/// <summary>
		/// The cheat thread itself.
		/// </summary>
		private void cheatTread()
		{
			ulong i = 0;

			while(true)
			{
				this.monitor?.Start();

				if((++i % 1000) == 0)
				{
					this.LocalPlayerAddress = this.ReadInt(this.ClientDLL + Offsets.LocalPlayer);

					if(this.monitorList?.Count > 0)
					{
						Console.WriteLine("Last 1000 actions averaged at {0}ms.", Convert.ToInt64(this.monitorList.Average()));
						this.monitorList.Clear();
					}
				}

#if DEBUG
                Thread.Sleep(250);
#else
				Thread.Sleep(1);
#endif

#if DEBUG
                Console.WriteLine("Team? {0} Alive? {1} can jump {2} in water {3}", this.Team, this.LifeState, this.CanJump.ToString(), this.InWater.ToString());
#endif

				bool bSpaceHeld = ((GetAsyncKeyState(Keys.Space) & (1 << 16)) > 0);

				if(bSpaceHeld)
				{
					this.WriteInt(this.ClientDLL + Offsets.JumpAddress, 4);
				}

				if((!this.CanJump && !this.InWater) || !this.Alive || this.Team < 2 || this.Paused)
				{
					this.monitor?.Stop();
					this.monitorList?.Add(this.monitor.ElapsedMilliseconds);
					this.monitor?.Reset();

					continue;
				}

				// 5 - jumping, 4 - not
				this.WriteInt(this.ClientDLL + Offsets.JumpAddress, 5);

				Thread.Sleep(25);
#if DEBUG
                Console.WriteLine("1: {0}", this.ReadInt(this.ClientDLL + Offsets.JumpAddress));
#endif
			}
		}

		/// <summary>
		/// Verifies if the game is open.
		/// </summary>
		private void onTimedEvent(object source, ElapsedEventArgs e)
		{
			if(!this.Running)
			{
				Console.WriteLine(Environment.NewLine + "\t- Detected game closing. Cheat shut down.");

				this.Thread.Abort();
				this.keepAlive.Close();
			}
		}

		/// <summary>
		/// Returns true if the game process is still open.
		/// </summary>
		public bool Running
		{
			get
			{
				if(this.Process == null)
				{
					throw new ArgumentNullException("process");
				}

				try
				{
					Process.GetProcessById(this.ProcessID);
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
		static extern short GetAsyncKeyState(Keys vKey);

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
		static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		/// <summary>
		/// http://www.pinvoke.net/default.aspx/kernel32.readprocessmemory
		/// </summary>
		/// <param name="hProcess">Handler to process.</param>
		/// <param name="lpBaseAddress">Addrress to write to.</param>
		/// <param name="lpBuffer">Buffer that includes the data we want to write.</param>
		/// <param name="dwSize">Size of the memory we write.</param>
		/// <param name="lpNumberOfBytesRead">Reference to data we successfully wrote.</param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

		/// <summary>
		/// Reads a memory value from a given address.
		/// </summary>
		/// <param name="address">Address to read from.</param>
		/// <returns>Value of the memory address we asked to read.</returns>
		public int ReadInt(int address)
		{
			byte[] bData = new byte[4];
			IntPtr ipBytesRead = IntPtr.Zero;

			if(!ReadProcessMemory(this.ProcessHandler, (IntPtr)address, bData, bData.Length, out ipBytesRead) || ipBytesRead.ToInt32() != 4)
			{
				return MEMORY_READ_FAILED;
			}

			return BitConverter.ToInt32(bData, 0);
		}

		/// <summary>
		/// Writes a value to a specified memory address.
		/// </summary>
		/// <param name="address">Address to write to.</param>
		/// <returns>True if written or false if not.</returns>
		public bool WriteInt(int address, int value)
		{
			byte[] bData = BitConverter.GetBytes(value);
			IntPtr ipBytesWritten = IntPtr.Zero;

			if(!WriteProcessMemory(this.ProcessHandler, (IntPtr)address, bData, bData.Length, out ipBytesWritten) || ipBytesWritten.ToInt32() != 4)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the player's health.
		/// </summary>
		public int Health
		{
			get
			{
				return this.ReadInt(this.LocalPlayerAddress + Offsets.m_iHealth);
			}
		}

		/// <summary>
		/// Gets the player's team.
		/// </summary>
		public int Team
		{
			get
			{
				return this.ReadInt(this.LocalPlayerAddress + Offsets.m_iTeam);
			}
		}

		/// <summary>
		/// Gets the player's movetype.
		/// </summary>
		public int MoveType
		{
			get
			{
				return this.ReadInt(this.LocalPlayerAddress + Offsets.m_MoveType);
			}
		}

		/// <summary>
		/// Gets the lifestate, 25600 is alive.
		/// </summary>
		public int LifeState
		{
			get
			{
				return this.ReadInt(this.LocalPlayerAddress + Offsets.m_lifestate);
			}
		}

		/// <summary>
		/// Checks if the player is alive.
		/// </summary>
		public bool Alive
		{
			get
			{
				return (this.LifeState == 25600);
			}
		}

		/// <summary>
		/// Gets the player's ground entity.
		/// </summary>
		public int GroundEntity
		{
			get
			{
				return this.ReadInt(this.LocalPlayerAddress + Offsets.m_hGroundEntity);
			}
		}

		/// <summary>
		/// Checks if the player is on a ladder.
		/// </summary>
		public bool OnLadder
		{
			get
			{
				return (this.MoveType == (int)Offsets.MoveType.MOVETYPE_LADDER);
			}
		}

		/// <summary>
		/// Gets the player's flags.
		/// </summary>
		public int Flags
		{
			get
			{
				return this.ReadInt(this.LocalPlayerAddress + Offsets.m_fFlags);
			}
		}

		/// <summary>
		/// Is the player inside water?
		/// </summary>
		public bool InWater
		{
			get
			{
				return ((this.Flags & (1 << 9)) > 0);
			}
		}

		/// <summary>
		/// Is the game paused?
		/// </summary>
		public bool Paused
		{
			get
			{
				return (this.ReadInt(Offsets.ChatOpen) == 1 || this.ReadInt(this.VGUIDLL + Offsets.PauseMenu) == 1);
			}
		}

		/// <summary>
		/// Checks if the player is able to jump which is: either on ground, on ladder or waterlevel being 2 or higher.
		/// </summary>
		public bool CanJump
		{
			get
			{
				return (this.GroundEntity != -1 || this.OnLadder);
			}
		}

		#region IDisposable support
		/// <summary>
		/// Dispose related variables.
		/// </summary>
		private bool disposed = false;
		private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

		/// <summary>
		/// Dispose the monitor.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposal implementation.
		/// </summary>
		/// <param name="disposing">Are we currently disposing?</param>
		protected virtual void Dispose(bool disposing)
		{
			if(this.disposed)
			{
				return;
			}

			if(disposing)
			{
				this.handle.Dispose();
				this.keepAlive?.Close();
				this.keepAlive.Dispose();
				this.Process.Dispose();
				this.monitor.Dispose();

				try
				{
					this.Thread?.Abort();
				}

				catch(Exception ex)
				{
					Console.WriteLine($"Faced an exception: ({ex.ToString()}) while aborting threads: {ex.Message}");
				}
			}

			disposed = true;
		}

		~GameProcess()
		{
			this.Dispose(false);
		}
		#endregion
	}
}
