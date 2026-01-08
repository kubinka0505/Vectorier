using System.Xml;
using UnityEngine;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Spawn_ {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement parentElement = null,

			int floatPrecision = -1
		) {
			XmlDocument xml = node.OwnerDocument;

			var respawn = go.GetComponent<Respawn>();
			var spawn = go.GetComponent<Spawn>();

			if (respawn != null) {
				SpawnHelpers.Build.Respawn(go, node, xml, floatPrecision);
			} else if (respawn == null && spawn != null && !spawn.RefersToRespawn) {
				SpawnHelpers.Build.Spawn(go, node, xml, floatPrecision);
			}

			SpawnHelpers.ApplyWriteModeSpawn(go, respawn != null ? null : spawn, xml, node);
		}

		// Helpers
		public static class SpawnHelpers {
			public static class Build {
				public static void Spawn(
					GameObject go,

					XmlNode node,
					XmlDocument xml,

					int floatPrecision = -1
				) {
					var spawn = go.GetComponent<Spawn>();
					if (spawn == null || !spawn.enabled) {
						return;
					}

					XmlElement mainElement = xml.CreateElement("Spawn");

					mainElement.SetAttribute("Name", spawn.SpawnName);

					var (x, y) = Helpers.Get.Position(go);
					if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
					if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

					mainElement.SetAttribute("Animation", spawn.SpawnAnimation);

					node.FirstChild.AppendChild(mainElement);
				}

				public static void Respawn(
					GameObject go,

					XmlNode node,
					XmlDocument xml,

					int floatPrecision = -1
				) {
					var respawn = go.GetComponent<Respawn>();
					if (respawn == null) {
						return;
					}

					XmlElement objectElement = xml.CreateElement("Object"); // CREATES OBJECT
					XmlElement contentElement = xml.CreateElement("Content");

					// Add Spawns
					Spawn[] spawns = GameObject.FindObjectsOfType<Spawn>();

					foreach (var sp in spawns) {
						if (sp.RefersToRespawn && sp.SpawnName == respawn.RespawnName) {
							XmlElement spElement = xml.CreateElement("Spawn");
			
							var (sp_x, sp_y) = Helpers.Get.Position(sp.gameObject);
							if (sp_x != 0) { spElement.SetAttribute("X", Helpers.Multiply(sp_x, floatPrecision)); }
							if (sp_y != 0) { spElement.SetAttribute("Y", Helpers.Multiply(-sp_y, floatPrecision)); }

							spElement.SetAttribute("Name", sp.SpawnName);
							spElement.SetAttribute("Animation", sp.SpawnAnimation);
							contentElement.AppendChild(spElement);
						}
					}

					// Add Trigger
					XmlElement mainElement = xml.CreateElement("Trigger");

					// todo: remove?
					mainElement.SetAttribute("Name", respawn.TriggerName);

					var (x, y) = Helpers.Get.Position(go);
					if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
					if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

					ImageHelpers.ApplySpriteSize(go, mainElement);

					// Properties
					XmlElement propertiesElement = xml.CreateElement("Properties");
					XmlElement staticElement = xml.CreateElement("Static");
					XmlElement selectionElement = xml.CreateElement("Selection");
					selectionElement.SetAttribute("Choice", "AITriggers");
					selectionElement.SetAttribute("Variant", respawn.HunterModeRespawn ? "HunterMode" : "CommonMode");
					staticElement.AppendChild(selectionElement);
					propertiesElement.AppendChild(staticElement);
					mainElement.AppendChild(propertiesElement);

					// Trigger Content
					XmlElement triggerContent = xml.CreateElement("Content");
					XmlElement initElement = CreateInitElement(go, xml);
					triggerContent.AppendChild(initElement);

					CreateTemplateElements(go, triggerContent, xml);

					mainElement.AppendChild(triggerContent);
					contentElement.AppendChild(mainElement);
					objectElement.AppendChild(contentElement);

					node.FirstChild.AppendChild(objectElement);
				}

				public static XmlElement CreateInitElement(
					GameObject go,

					XmlDocument xml
				) {
					var respawn = go.GetComponent<Respawn>();
					if (respawn == null || !respawn.enabled) {
						return null;
					}

					XmlElement init = xml.CreateElement("Init");
					float frames = respawn.RespawnSecond * 60;

					string[][] variables = {
						new[] { "Name", "$Active", "Value", "1" },
						new[] { "Name", "$Node", "Value", "COM" },
						new[] { "Name", "Spawn", "Value", respawn.RespawnName },
						new[] { "Name", "Frames", "Value", Mathf.Round(frames).ToString() },
						new[] { "Name", "SpawnModel", "Value", respawn.Spawnmodel },
						new[] { "Name", "$AI", "Value", "0" },
						new[] { "Name", "Flag1", "Value", "0" },
					};

					foreach (var v in variables) {
						XmlElement setVar = xml.CreateElement("SetVariable");
						setVar.SetAttribute(v[0], v[1]);
						setVar.SetAttribute(v[2], v[3]);
						init.AppendChild(setVar);
					}

					return init;
				}

				public static void CreateTemplateElements(
					GameObject go,

					XmlElement parent,
					XmlDocument xml
				) {
					var respawn = go.GetComponent<Respawn>();
					if (respawn == null) {
						return;
					}

					if (respawn.RespawnOnScreen) {
						XmlElement loop1 = xml.CreateElement("Loop");
						loop1.SetAttribute("Template", "Respawn_OnScreen.Player");

						XmlElement loop2 = xml.CreateElement("Loop");
						loop2.SetAttribute("Template", "Respawn_OnScreen.Timeout");

						parent.AppendChild(loop1);
						parent.AppendChild(loop2);
					} else {
						XmlElement template = xml.CreateElement("Template");
						template.SetAttribute("Name", "Respawn");
						parent.AppendChild(template);
					}
				}
			}

			public static void ApplyWriteModeSpawn(GameObject go, object component, XmlDocument xml, XmlNode node) {
				string writeModeValue = "any";
				var writeMode = go.GetComponent<VectorierWriteMode>();

				if (writeMode != null && writeMode.enabled) {
					writeModeValue = writeMode.GetWriteModeValue();
					XmlElement targetElement = node.FirstChild.LastChild as XmlElement;
					writeMode.AddWriteModeProperties(xml, targetElement, writeModeValue);
				}
			}
		}
	}
}
