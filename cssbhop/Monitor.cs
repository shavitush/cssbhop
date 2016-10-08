using System.Diagnostics;

namespace cssbhop
{
	/// <summary>
	/// Extends the Stopwatch class for our purposes.
	/// </summary>
	class Monitor : Stopwatch
	{
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
	}
}
