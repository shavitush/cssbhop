using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace cssbhop
{
    class GameProcess
    {
        /// <summary>
        /// Statics.
        /// </summary>
        public static int MEMORY_READ_FAILED = int.MinValue;

        /// <summary>
        /// Private variables.
        /// </summary>
        private string sName;
        private string sPath;
        private string sCmdline;
        private bool bInsecure;
        private Process pProcess;
        private int iProcessID;
        private Thread tCheatThread;
        private System.Timers.Timer tKeepAlive;
        private IntPtr ipHandler;
        private int iClientDLL;
        private int iVGUIDLL;
        private int iLocalPlayerAddress;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public GameProcess()
        {
            this.sName = string.Empty;
            this.sCmdline = string.Empty;
            this.bInsecure = false;
            this.pProcess = null;
            this.iProcessID = -1;
            this.tCheatThread = null;
            this.tKeepAlive = null;
            this.ipHandler = IntPtr.Zero;
            this.iClientDLL = -1;
            this.iVGUIDLL = -1;
            this.iLocalPlayerAddress = -1;
        }

        /// <summary>
        /// Window title for the game.
        /// </summary>
        public string Name
        {
            get
            {
                return this.sName;
            }

            set
            {
                this.sName = value;
            }
        }

        /// <summary>
        /// Path to hl2.exe.
        /// </summary>
        public string Path
        {
            get
            {
                return this.sPath;
            }

            set
            {
                this.sPath = value;
            }
        }

        /// <summary>
        /// Command line parameters.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.sCmdline;
            }

            set
            {
                this.sCmdline = value;
            }
        }

        /// <summary>
        /// Running VAC secured?
        /// </summary>
        public bool Insecure
        {
            get
            {
                return this.bInsecure;
            }

            set
            {
                this.bInsecure = value;
            }
        }

        /// <summary>
        /// Process object.
        /// </summary>
        public Process Process
        {
            get
            {
                return this.pProcess;
            }

            set
            {
                this.pProcess = value;
            }
        }

        /// <summary>
        /// Process ID.
        /// </summary>
        public int ProcessID
        {
            get
            {
                return this.iProcessID;
            }

            set
            {
                this.iProcessID = value;
            }
        }

        public IntPtr ProcessHandler
        {
            get
            {
                return this.ipHandler;
            }

            set
            {
                this.ipHandler = value;
            }
        }

        public int ClientDLL
        {
            get
            {
                return this.iClientDLL;
            }

            set
            {
                this.iClientDLL = value;
            }
        }

        public int VGUIDLL
        {
            get
            {
                return this.iVGUIDLL;
            }

            set
            {
                this.iVGUIDLL = value;
            }
        }

        public int LocalPlayerAddress
        {
            get
            {
                return this.iLocalPlayerAddress;
            }

            set
            {
                this.iLocalPlayerAddress = value;
            }
        }

        /// <summary>
        /// Cheat thread.
        /// </summary>
        public Thread Thread
        {
            get
            {
                return this.tCheatThread;
            }

            set
            {
                this.tCheatThread = value;
            }
        }

        /// <summary>
        /// Keep-alive thread. (Is the game closed?)
        /// </summary>
        public System.Timers.Timer KeepAlive
        {
            get
            {
                return this.tKeepAlive;
            }

            set
            {
                this.tKeepAlive = value;
            }
        }

        /// <summary>
        /// Starts the needed threads.
        /// </summary>
        public void StartThread()
        {
            // Starts the cheat thread.
            this.Thread = new Thread(new ThreadStart(this.CheatTread));
            this.Thread.Start();

            // Starts the keep-alive timer, to make sure the game is open.
            this.KeepAlive = new System.Timers.Timer();
            this.KeepAlive.Elapsed += new ElapsedEventHandler(this.OnTimedEvent);
            this.KeepAlive.Interval = 2500;
            this.KeepAlive.Enabled = true;
        }

        /// <summary>
        /// Kills the cheat related threads and timers.
        /// </summary>
        public void KillThreads()
        {
            try
            {
                this.Thread.Abort();
                this.KeepAlive.Close();
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
                if((++i % 1000) == 0)
                {
                    this.LocalPlayerAddress = this.ReadInt(this.ClientDLL + Offsets.LocalPlayer);
                }

#if DEBUG
                Thread.Sleep(250);
#else
                Thread.Sleep(1);
#endif

#if DEBUG
                Console.WriteLine("Team? {0} Alive? {1} can jump {2} in water {3}", iTeam, this.LifeState, this.CanJump.ToString(), this.InWater.ToString());
#endif

                bool bSpaceHeld = ((GetAsyncKeyState(Keys.Space) & (1 << 16)) > 0);

                if(bSpaceHeld)
                {
                    this.WriteInt(this.ClientDLL + Offsets.JumpAddress, 4);
                }

                if(this.Paused || this.Team < 2 || !this.Alive || (!this.CanJump && !this.InWater) || !bSpaceHeld)
                {
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
        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if(!this.Running)
            {
                Console.WriteLine("\n\t- Detected game closing. Cheat shut down.");

                this.Thread.Abort();
                this.KeepAlive.Close();
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

            if(!ReadProcessMemory(this.ipHandler, (IntPtr)address, bData, bData.Length, out ipBytesRead) || ipBytesRead.ToInt32() != 4)
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

            if(!WriteProcessMemory(this.ipHandler, (IntPtr)address, bData, bData.Length, out ipBytesWritten) || ipBytesWritten.ToInt32() != 4)
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
    }
}
