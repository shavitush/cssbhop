namespace cssbhop
{
	public class General
	{
		/// <summary>
		/// Private variables.
		/// </summary>
		private static float fVersion = 1.3f;
		private static bool bMonitoring = false;

		/// <summary>
		/// Retrieves the current version of cssbhop.
		/// </summary>
		public static float Version
		{
			get
			{
				return fVersion;
			}
		}

		/// <summary>
		/// Are we monitoring performance?
		/// </summary>
		public static bool Monitoring
		{
			get
			{
				return bMonitoring;
			}

			set
			{
				bMonitoring = value;
			}
		}
	}
}
