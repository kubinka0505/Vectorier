using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using System.Globalization;

using System.Collections.Generic;

// -=-=-=- //

#nullable enable

namespace Vectorier.Settings.Credits {
	class Parser {
		public class UserData {
			public string Name = "";
			public string? Url = null;
			public List<string> Aliases;

			public UserData() {
				Aliases = new List<string>();
			}
		}

		public class MaintainerData {
			public string Name = "";
			public string Period = "";
		}

		// UI fields
		public static string Name = "Vectorier";
		public static Texture2D Logo = AssetDatabase.LoadAssetAtPath<Texture2D>(
			Path.Combine("Assets", "Scripts", "Vectorier", "Logo.png") // progname doesn't work
		);
		public static int SpaceVertical = 18;

		// dynamic data
		public static string? UrlCode;
		public static string? UrlChat;
		public static string? projectName;

		public static Dictionary<string, UserData> userMap = new Dictionary<string, UserData>();

		public static Dictionary<string, List<string>> Creators = new Dictionary<string, List<string>>();
		public static Dictionary<string, List<MaintainerData>> MaintainersActive = new Dictionary<string, List<MaintainerData>>();
		public static Dictionary<string, List<MaintainerData>> MaintainersExecutive = new Dictionary<string, List<MaintainerData>>();

		public static Dictionary<string, List<string>> Textures = new Dictionary<string, List<string>>();
		public static Dictionary<string, List<string>> Packages = new Dictionary<string, List<string>>();

		public static Dictionary<string, List<string>> Acknowledgements = new Dictionary<string, List<string>>();

		public static void Load(string xmlPath) {
			try {
				if (!File.Exists(xmlPath)) {
					return;
				}

				string xmlText = File.ReadAllText(xmlPath);

				// Wrap the XML in a single root element
				xmlText = $"<Root>{xmlText}</Root>";

				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xmlText);

				// --- Users ---
				XmlNode usersNode = doc.SelectSingleNode("//Root/Info/Users");
				if (usersNode != null) {
					foreach (XmlNode node in usersNode.SelectNodes("User")) {
						UserData u = new UserData();
						string[] nameParts = node.Attributes["Name"]?.Value.Split('|') ?? new string[] { "Unknown" };
						u.Name = nameParts[0];
						u.Aliases.AddRange(nameParts);
						u.Url = node.Attributes["URL"]?.Value;
						userMap[u.Name] = u;
					}
				}

				// --- Project ---
				projectName = doc.SelectSingleNode("//Root/Project")?.Attributes["Name"]?.Value ?? "Unknown";

				// --- Social links ---
				XmlNode socialNode = doc.SelectSingleNode("//Root/Info/Social");
				UrlCode = socialNode?.SelectSingleNode("Code")?.Attributes["URL"]?.Value.Replace("{progName}", projectName) ?? "";
				UrlChat = socialNode?.SelectSingleNode("Chat")?.Attributes["URL"]?.Value ?? "";

				// --- Creators ---
				XmlNodeList creatorNodes = doc.SelectNodes("//Root/Project/Creators/User");
				foreach (XmlNode node in creatorNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;
					Creators[name] = new List<string> { name };
				}

				// --- Maintainers: Active ---
				XmlNodeList activeNodes = doc.SelectNodes("//Root/Project/Maintainers/Active/User");
				foreach (XmlNode node in activeNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;
					string period = FormatPeriod(node.Attributes["Start"]?.Value, node.Attributes["End"]?.Value);
					MaintainersActive[name] = new List<MaintainerData> { new MaintainerData { Name = name, Period = period } };
				}

				// --- Maintainers: Executive ---
				XmlNodeList execNodes = doc.SelectNodes("//Root/Project/Maintainers/Executive/User");
				foreach (XmlNode node in execNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;
					string period = FormatPeriod(node.Attributes["Start"]?.Value, node.Attributes["End"]?.Value);
					MaintainersExecutive[name] = new List<MaintainerData> { new MaintainerData { Name = name, Period = period } };
				}

				// --- Textures ---
				XmlNodeList textureNodes = doc.SelectNodes("//Root/Project/Assets/Textures/Asset");
				foreach (XmlNode node in textureNodes) {
					string name = node.Attributes["Name"]?.Value ?? "Unknown";
					string authors = node.Attributes["Authors"]?.Value ?? "";
					Textures[name] = ResolveAliases(authors);
				}

				// --- Packages ---
				XmlNodeList packageNodes = doc.SelectNodes("//Root/Project/Assets/Packages/Asset");
				foreach (XmlNode node in packageNodes) {
					string name = node.Attributes["Name"]?.Value ?? "Unknown";
					string authors = node.Attributes["Authors"]?.Value ?? "";
					Packages[name] = ResolveAliases(authors);
				}

				// --- Acknowledgements ---
				XmlNodeList ackNodes = doc.SelectNodes("//Root/Project/Acknowledgements/User");
				foreach (XmlNode node in ackNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					string contribs = node.Attributes["Contributions"]?.Value ?? "";

					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;

					Acknowledgements[name] = new List<string>(contribs.Split('|'));
				}
			} catch (Exception ex) {
				Debug.LogError($"Failed to load credits: {ex}");
			}
		}

		public static List<string> ResolveAliases(string authors) {
			List<string> result = new List<string>();

			foreach (string a in authors.Split('|')) {
				foreach (var kvp in userMap) {
					if (kvp.Value.Aliases.Contains(a)) {
						result.Add(kvp.Value.Name);
						break;
					}
				}
			}

			return result;
		}

		public static string FormatPeriod(string? startUnix, string? endUnix) {
			string startStr = string.Empty;
			string endStr = string.Empty;

			if (!string.IsNullOrEmpty(startUnix)) {
				if (long.TryParse(startUnix, out long startVal)) {
					startStr = FormatDate(startVal);
				}
			}

			if (!string.IsNullOrEmpty(endUnix)) {
				if (long.TryParse(endUnix, out long endVal)) {
					endStr = FormatDate(endVal);
				}
			}

			if (string.IsNullOrEmpty(startStr) && string.IsNullOrEmpty(endStr))
				return "date unknown";

			if (!string.IsNullOrEmpty(startStr) && !string.IsNullOrEmpty(endStr))
				return $"from {startStr} to {endStr}";

			if (!string.IsNullOrEmpty(startStr))
				return $"from {startStr}";

			return $"to {endStr}";
		}

		public static string FormatDate(
			long timestamp,
			bool useSystemLocale = false
		) {
			DateTime dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;

			// Day with leading zero + ordinal
			string dayWithSuffix = dt.ToString("dd") + GetDaySuffix(dt.Day);

			// Month and year
			string monthYear = useSystemLocale
				? dt.ToString("MMMM yyyy", CultureInfo.CurrentCulture)
				: dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

			return $"{dayWithSuffix} {monthYear}";
		}

		public static string GetDaySuffix(int day) {
			switch (day) {
				case 1:
				case 21:
				case 31:
					return "st";
				case 2:
				case 22:
					return "nd";
				case 3:
				case 23:
					return "rd";
				default:
					return "th";
			}
		}
	}
}