using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;

namespace cssbhop
{
	public class AutoUpdater
	{
		/// <summary>
		/// Starts the update thread.
		/// </summary>
		public static void StartUpdate()
		{
			Thread tUpdateThread = new Thread(new ThreadStart(UpdateThread));
			tUpdateThread.Start();
		}

		/// <summary>
		/// Checks if there's a pending update.
		/// </summary>
		public static void UpdateThread()
		{
			try
			{
				HttpWebRequest Request = WebRequest.Create($"https://api.github.com/repos/{General.Repository}/releases/latest") as HttpWebRequest;
				Request.UserAgent = "cssbhop";
				Request.Method = "GET";

				using(StreamReader ResponseReader = new StreamReader(Request.GetResponse().GetResponseStream()))
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
				Console.WriteLine("ERROR: {0}", ex.Message);

				Program.UpdateNeeded = false;
			}
		}
	}
}
