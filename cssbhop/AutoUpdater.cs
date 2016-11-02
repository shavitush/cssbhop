using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;

namespace cssbhop
{
	public class AutoUpdater
	{
		#region Object constructor
		/// <summary>
		/// Starts the update thread.
		/// </summary>
		public static void StartUpdate()
		{
			var tUpdateThread = new Thread(new ThreadStart(updateThread));
			tUpdateThread.Start();
		}
		#endregion

		#region Object methods
		/// <summary>
		/// Checks if there's a pending update.
		/// </summary>
		private static void updateThread()
		{
			try
			{
				var Request = WebRequest.Create($"https://api.github.com/repos/{General.Repository}/releases/latest") as HttpWebRequest;
				Request.UserAgent = "cssbhop";
				Request.Method = "GET";

				using(var ResponseReader = new StreamReader(Request.GetResponse().GetResponseStream()))
				{
					dynamic dReleaseInfo = new JavaScriptSerializer().Deserialize<dynamic>(ResponseReader.ReadToEnd());
					float fLatestVersion = float.Parse(dReleaseInfo["tag_name"]);

					if(fLatestVersion > General.Version)
					{
						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine(Environment.NewLine + "--- There's a pending update ({0} -> {1})!{2}Write \"update\" to get started.", General.Version, fLatestVersion, Environment.NewLine);
						Console.ForegroundColor = ConsoleColor.White;

						Program.UpdateNeeded = true;
					}
				}
			}

			catch(Exception ex)
			{
				Console.WriteLine($"ERROR: {ex.Message}");

				Program.UpdateNeeded = false;
			}
		}
		#endregion
	}
}
