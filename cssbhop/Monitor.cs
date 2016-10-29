﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cssbhop
{
	/// <summary>
	/// Extends the Stopwatch class for our purposes.
	/// </summary>
	class Monitor : Stopwatch, IDisposable
	{
		/// <summary>
		/// Dispose logic.
		/// </summary>
		private bool disposed = false;
		private SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

		/// <summary>
		/// Private variables.
		/// </summary>
		private Stopwatch swStopwatch;
		private string sFormatting;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Monitor(string formatting, bool start = false)
		{
			this.sFormatting = formatting;
			this.swStopwatch = new Stopwatch();

			if(start)
			{
				this.swStopwatch.Start();
			}
		}

		/// <summary>
		/// Changes the {ms} in the formatting rules to the amount of miliseconds passed and resets it.
		/// 
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string sTimeElapsed = this.swStopwatch.ElapsedMilliseconds.ToString();
			this.swStopwatch.Reset();

			return sFormatting.Replace("{ms}", sTimeElapsed);
		}

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
			}
			
			disposed = true;
		}

		~Monitor()
		{
			this.Dispose(false);
		}
	}
}
