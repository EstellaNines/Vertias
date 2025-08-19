using UnityEngine;
using UnityEditor;
#if UNITY_2018_1_OR_NEWER
using UnityEngine.Networking;
#endif
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding
{
	/// <summary>Handles update checking for the A* Pathfinding Project</summary>
	[InitializeOnLoad]
	public static class AstarUpdateChecker
	{
#if UNITY_2018_1_OR_NEWER
		/// <summary>Used for downloading new version information</summary>
		static UnityWebRequest updateCheckDownload;
#else
		/// <summary>Used for downloading new version information</summary>
		static WWW updateCheckDownload;
#endif

		static System.DateTime _lastUpdateCheck;
		static bool _lastUpdateCheckRead;

		static System.Version _latestVersion;

		static System.Version _latestBetaVersion;

		/// <summary>Description of the latest update of the A* Pathfinding Project</summary>
		static string _latestVersionDescription;

		static bool hasParsedServerMessage;

		/// <summary>Number of days between update checks</summary>
		const double updateCheckRate = 1F;

		/// <summary>URL to the version file containing the latest version number.</summary>
		const string updateURL = "http://www.arongranberg.com/astar/version.php";

		/// <summary>Last time an update check was made</summary>
		public static System.DateTime lastUpdateCheck
		{
			get
			{
				try
				{
					// Reading from EditorPrefs is relatively slow, avoid it
					if (_lastUpdateCheckRead) return _lastUpdateCheck;

					_lastUpdateCheck = System.DateTime.Parse(EditorPrefs.GetString("AstarLastUpdateCheck", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
					_lastUpdateCheckRead = true;
				}
				catch (System.FormatException)
				{
					lastUpdateCheck = System.DateTime.UtcNow;
					Debug.LogWarning("Invalid DateTime string encountered when loading from preferences");
				}
				return _lastUpdateCheck;
			}
			private set
			{
				_lastUpdateCheck = value;
				EditorPrefs.SetString("AstarLastUpdateCheck", _lastUpdateCheck.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}

		/// <summary>Latest version of the A* Pathfinding Project</summary>
		public static System.Version latestVersion
		{
			get
			{
				RefreshServerMessage();
				return _latestVersion ?? AstarPath.Version;
			}
			private set
			{
				_latestVersion = value;
			}
		}

		/// <summary>Latest beta version of the A* Pathfinding Project</summary>
		public static System.Version latestBetaVersion
		{
			get
			{
				RefreshServerMessage();
				return _latestBetaVersion ?? AstarPath.Version;
			}
			private set
			{
				_latestBetaVersion = value;
			}
		}

		/// <summary>Summary of the latest update</summary>
		public static string latestVersionDescription
		{
			get
			{
				RefreshServerMessage();
				return _latestVersionDescription ?? "";
			}
			private set
			{
				_latestVersionDescription = value;
			}
		}

		/// <summary>
		/// Holds various URLs and text for the editor.
		/// This info can be updated when a check for new versions is done to ensure that there are no invalid links.
		/// </summary>
		static Dictionary<string, string> astarServerData = new Dictionary<string, string> {
			{ "URL:modifiers", "http://www.arongranberg.com/astar/docs/modifiers.php" },
			{ "URL:astarpro", "http://arongranberg.com/unity/a-pathfinding/astarpro/" },
			{ "URL:documentation", "http://arongranberg.com/astar/docs/" },
			{ "URL:findoutmore", "http://arongranberg.com/astar" },
			{ "URL:download", "http://arongranberg.com/unity/a-pathfinding/download" },
			{ "URL:changelog", "http://arongranberg.com/astar/docs/changelog.php" },
			{ "URL:tags", "http://arongranberg.com/astar/docs/tags.php" },
			{ "URL:homepage", "http://arongranberg.com/astar/" }
		};

		static AstarUpdateChecker()
		{
			// 禁用自动更新检查以避免网络连接错误
			// EditorApplication.update += UpdateCheckLoop;
			EditorBase.getDocumentationURL = () => GetURL("documentation");
		}


		static void RefreshServerMessage()
		{
			if (!hasParsedServerMessage)
			{
				var serverMessage = EditorPrefs.GetString("AstarServerMessage");

				if (!string.IsNullOrEmpty(serverMessage))
				{
					ParseServerMessage(serverMessage);
					ShowUpdateWindowIfRelevant();
				}
			}
		}

		public static string GetURL(string tag)
		{
			RefreshServerMessage();
			string url;
			astarServerData.TryGetValue("URL:" + tag, out url);
			return url ?? "";
		}

		/// <summary>Initiate a check for updates now, regardless of when the last check was done</summary>
		public static void CheckForUpdatesNow()
		{
			// 禁用更新检查功能以避免网络连接错误
			Debug.Log("A* Pathfinding Project: 更新检查已被禁用以避免网络连接问题");
			return;
		}

		/// <summary>
		/// Checking for updates...
		/// Should be called from EditorApplication.update
		/// </summary>
		static void UpdateCheckLoop()
		{
			// Go on until the update check has been completed
			if (!CheckForUpdates())
			{
				EditorApplication.update -= UpdateCheckLoop;
			}
		}

		/// <summary>
		/// Checks for updates if there was some time since last check.
		/// It must be called repeatedly to ensure that the result is processed.
		/// Returns: True if an update check is progressing (WWW request)
		/// </summary>
		static bool CheckForUpdates()
		{
			// 禁用更新检查功能以避免网络连接错误
			// 直接返回false表示没有正在进行的更新检查
			return false;
		}

		static void DownloadVersionInfo()
		{
			// 禁用版本信息下载以避免网络连接错误
			// 不再发起任何网络请求
			return;
		}

		/// <summary>Handles the data from the update page</summary>
		static void UpdateCheckCompleted(string result)
		{
			EditorPrefs.SetString("AstarServerMessage", result);
			ParseServerMessage(result);
			ShowUpdateWindowIfRelevant();
		}

		static void ParseServerMessage(string result)
		{
			if (string.IsNullOrEmpty(result))
			{
				return;
			}

			hasParsedServerMessage = true;

#if ASTARDEBUG
			Debug.Log("Result from update check:\n"+result);
#endif

			string[] splits = result.Split('|');
			latestVersionDescription = splits.Length > 1 ? splits[1] : "";

			if (splits.Length > 4)
			{
				// First 4 are just compatibility fields
				var fields = splits.Skip(4).ToArray();

				// Take all pairs of fields
				for (int i = 0; i < (fields.Length / 2) * 2; i += 2)
				{
					string key = fields[i];
					string val = fields[i + 1];
					astarServerData[key] = val;
				}
			}

			try
			{
				latestVersion = new System.Version(astarServerData["VERSION:branch"]);
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning("Could not parse version\n" + ex);
			}

			try
			{
				latestBetaVersion = new System.Version(astarServerData["VERSION:beta"]);
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning("Could not parse version\n" + ex);
			}
		}

		static void ShowUpdateWindowIfRelevant()
		{
#if !ASTAR_ATAVISM
			try
			{
				System.DateTime remindDate;
				var remindVersion = new System.Version(EditorPrefs.GetString("AstarRemindUpdateVersion", "0.0.0.0"));
				if (latestVersion == remindVersion && System.DateTime.TryParse(EditorPrefs.GetString("AstarRemindUpdateDate", "1/1/1971 00:00:01"), out remindDate))
				{
					if (System.DateTime.UtcNow < remindDate)
					{
						// Don't remind yet
						return;
					}
				}
				else
				{
					EditorPrefs.DeleteKey("AstarRemindUpdateDate");
					EditorPrefs.DeleteKey("AstarRemindUpdateVersion");
				}
			}
			catch
			{
				Debug.LogError("Invalid AstarRemindUpdateVersion or AstarRemindUpdateDate");
			}

			var skipVersion = new System.Version(EditorPrefs.GetString("AstarSkipUpToVersion", AstarPath.Version.ToString()));

			if (AstarPathEditor.FullyDefinedVersion(latestVersion) != AstarPathEditor.FullyDefinedVersion(skipVersion) && AstarPathEditor.FullyDefinedVersion(latestVersion) > AstarPathEditor.FullyDefinedVersion(AstarPath.Version))
			{
				EditorPrefs.DeleteKey("AstarSkipUpToVersion");
				EditorPrefs.DeleteKey("AstarRemindUpdateDate");
				EditorPrefs.DeleteKey("AstarRemindUpdateVersion");

				AstarUpdateWindow.Init(latestVersion, latestVersionDescription);
			}
#endif
		}
	}
}
