using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace cssbhop
{
	/// <summary>
	/// Extends the Stopwatch class for our purposes.
	/// </summary>
	internal class Monitor : Stopwatch, IDisposable
	{
		#region Private variables
		/// <summary>
		/// Private variables.
		/// </summary>
		private readonly Stopwatch _swStopwatch;
		private readonly string _sFormatting;
		#endregion

		#region Object constructor
		/// <summary>
		/// Constructor.
		/// </summary>
		public Monitor(string formatting, bool start = false)
		{
			_sFormatting = formatting;
			_swStopwatch = new Stopwatch();

			if(start)
			{
				_swStopwatch.Start();
			}
		}
		#endregion

		#region Overrides
		/// <summary>
		/// Changes the {ms} in the formatting rules to the amount of miliseconds passed and resets it.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string sTimeElapsed = _swStopwatch.ElapsedMilliseconds.ToString();
			_swStopwatch.Reset();

			return _sFormatting.Replace("{ms}", sTimeElapsed);
		}
		#endregion

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
			}
			
			_disposed = true;
		}

		~Monitor()
		{
			Dispose(false);
		}
		#endregion
	}
}
