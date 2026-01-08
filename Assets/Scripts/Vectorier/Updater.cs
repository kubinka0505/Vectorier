using UnityEngine;
using UnityEditor;

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Debug = Logger.Debug;

// -=-=-=- //

[InitializeOnLoad]
public static class VectorierUpdater {
	private const string CommitsEndpoint = "https://api.github.com/repos/kubinka0505/Vectorier/commits";
	private const string PrefKey_LastSha = "Vectorier.Updater.CommitsShaLast";
	private const string PrefKey_ETag = "Vectorier.Updater.CommitsETag";
	private const string PrefKey_LastCheckUtc = "Vectorier.Updater.CheckUtcLast";

	private const double CheckIntervalSeconds = 3600;
	private static double nextCheckTime = 0;

	static VectorierUpdater() {
		EditorApplication.update += OnEditorUpdate;
	}

	/*
	[MenuItem("Vectorier/Check For Update")]
	public static void MenuCheckNow() {
		_ = CheckForUpdateAsync(force: true);
	}
	*/

	private static void OnEditorUpdate() {
		if (!Vectorier.Settings.Updater) {
			return;
		}

		if (EditorApplication.timeSinceStartup < nextCheckTime) {
			return;
		}

		// schedule next run
		nextCheckTime = EditorApplication.timeSinceStartup + CheckIntervalSeconds;

		_ = CheckForUpdateAsync(force: false);
	}

	private static async Task CheckForUpdateAsync(bool force) {
		try {
			// very important for older Unity/.NET runtimes
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

			// avoid checking too frequently even across editor restarts:
			string lastCheckStr = EditorPrefs.GetString(PrefKey_LastCheckUtc, "");

			if (!force && DateTime.TryParse(lastCheckStr, out DateTime lastCheck)) {
				var elapsed = DateTime.UtcNow - lastCheck;

				if (elapsed.TotalSeconds < CheckIntervalSeconds) {
					return;
				}
			}
			EditorPrefs.SetString(PrefKey_LastCheckUtc, DateTime.UtcNow.ToString("o"));

			using (var client = new HttpClient()) {
				// required header on GitHub API
				client.DefaultRequestHeaders.UserAgent.ParseAdd("Vectorier (Unity Editor)");
				client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");

				// string token = EditorPrefs.GetString("Vectorier_GithubToken", "");
				// if (!string.IsNullOrEmpty(token)) client.DefaultRequestHeaders.Authorization =
				//     new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);

				var request = new HttpRequestMessage(HttpMethod.Get, CommitsEndpoint);

				// if theres ETag, ask GitHub to return 304 Not Modified if unchanged
				string storedEtag = EditorPrefs.GetString(PrefKey_ETag, "");
				if (!string.IsNullOrEmpty(storedEtag) && !force) {
					request.Headers.IfNoneMatch.ParseAdd(storedEtag);
				}

				// provide If-Modified-Since using last known commit date (defensive)
				string lastSha = EditorPrefs.GetString(PrefKey_LastSha, "");
				if (!string.IsNullOrEmpty(lastSha)) {
					// ...
				}

				using (var resp = await client.SendAsync(request)) {
					string log = "";

					if (resp.Headers.Contains("X-RateLimit-Limit") && resp.Headers.Contains("X-RateLimit-Remaining")) {
						string limit = string.Join(",", resp.Headers.GetValues("X-RateLimit-Limit"));
						string remaining = string.Join(",", resp.Headers.GetValues("X-RateLimit-Remaining"));

						log += $"Remaining API checks ({remaining}/{limit})";
					}

					if (resp.Headers.Contains("X-RateLimit-Reset")) {
						string resetEpoch = string.Join(",", resp.Headers.GetValues("X-RateLimit-Reset"));

						if (!string.IsNullOrEmpty(log)) {
							log += " ";
						}

						if (long.TryParse(resetEpoch, out long epoch)) {
							DateTime resetTime = DateTimeOffset.FromUnixTimeSeconds(epoch).ToLocalTime().DateTime;
							log += $"(Resets at {resetTime:dd.MM.yyyy HH:mm:ss})";
						}
					}

					Debug.Log(log);

					string body = await resp.Content.ReadAsStringAsync();

					if (resp.StatusCode == HttpStatusCode.Forbidden) {
						Debug.LogWarning($"Response body: {body}");
						Debug.LogWarning("Check X-RateLimit-* headers above. If remaining is 0 the unauthenticated rate limit is hit (60/hr).");
						return;
					}

					if (!resp.IsSuccessStatusCode && (int)resp.StatusCode != 304) {
						Debug.LogWarning($"GitHub API returned {(int)resp.StatusCode}: {body}");
						return;
					}

					// save ETag for next conditional request
					if (resp.Headers.ETag != null) {
						EditorPrefs.SetString(PrefKey_ETag, resp.Headers.ETag.Tag);
					}

					// parse commit list safely
					JArray json = null;
					try {
						if (string.IsNullOrWhiteSpace(body)) {
							// Debug.LogWarning("Empty response body.");
							return;
						}

						// sometimes returns an object (e.g., {"message": "API rate limit exceeded"})
						char firstChar = body.TrimStart()[0];
						if (firstChar == '{') {
							var obj = JObject.Parse(body);
							string msg = (string)obj["message"];

							Debug.LogWarning($"[VectorierUpdater] Unexpected JSON object instead of array. Message: {msg}");
							return;
						}

						json = JArray.Parse(body);
					} catch (Exception jex) {
						Debug.LogWarning($"Failed to parse GitHub response as JArray: {jex.Message}\nBody:\n{body}");
						return;
					}

					if (json == null || json.Count == 0) {
						Debug.Log("[VectorierUpdater] No commits found in response.");
						return;
					}


					var top = json[0];
					string sha = (string)top["sha"];
					string dateStr = (string)top["commit"]["committer"]["date"];
					DateTime commitDate = DateTime.Parse(dateStr).ToUniversalTime();

					string prevSha = EditorPrefs.GetString(PrefKey_LastSha, "");
					if (sha != prevSha) {
						EditorPrefs.SetString(PrefKey_LastSha, sha);

						// compare prev date (if stored) and only notify if difference > 3600s
						if (EditorUtility.DisplayDialog("Updater",
							$"New commit at {commitDate:yyyy-MM-dd HH:mm:ss} UTC.\n\nOpen repository?",
							"Yes", "No")) {
							Application.OpenURL("https://github.com/kubinka0505/Vectorier");
						}
					}
				}
			}
		} catch {
			// Debug.LogWarning($"Exception while checking for updates: {ex}");
		}
	}
}