using UnityEngine;

using System;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;

using static Vectorier.Core.Game;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Trigger {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement parentElement = null,

			int floatPrecision = -1
		) {
			string objRegex = Helpers.Get.Name(go);

			if (objRegex == "Camera") {
				return;
			}

			// Validation
			var spriteRenderer = go.GetComponent<SpriteRenderer>();

			if (spriteRenderer == null || !spriteRenderer.enabled || spriteRenderer.sprite == null) {
				return;
			}

			string objName = (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.sprite != null)
				? spriteRenderer.sprite.name
				: objRegex;

			var triggerSettings = go.GetComponent<TriggerSettings>();
			var dynamicTrigger = go.GetComponent<DynamicTrigger>();

			if (triggerSettings != null && dynamicTrigger != null) {
				Debug.LogError($"GameObject '{go.name}' cannot contain both TriggerSetting and DynamicTrigger, skipping.", go);
				return;
			} else if (triggerSettings == null && dynamicTrigger == null) {
				Debug.LogError($"GameObject '{go.name}' must contain at least TriggerSetting or DynamicTrigger, skipping.", go);
				return;
			}

			// Element
			XmlDocument xml = node.OwnerDocument;
			XmlElement mainElement = xml.CreateElement("Trigger");

			// Obsolete...
			if (!objRegex.StartsWith(">") && objRegex != objName && !string.IsNullOrEmpty(go.name)) {
				mainElement.SetAttribute("Name", objRegex);
			}

			// Position
			var (x, y) = Helpers.Get.Position(go);
			if (x != 0) { mainElement.SetAttribute("X", Helpers.Multiply(x, floatPrecision)); }
			if (y != 0) { mainElement.SetAttribute("Y", Helpers.Multiply(-y, floatPrecision)); }

			var (w, h) = Helpers.Get.Size(go);
			if (w != 0) { mainElement.SetAttribute("Width", Helpers.ToString(w)); }
			if (h != 0) { mainElement.SetAttribute("Height", Helpers.ToString(h)); }

			// TriggerSettings
			if (triggerSettings != null && triggerSettings.enabled) {
				XmlElement contentElement = TriggerHelpers.Build.Static(triggerSettings, xml);
				mainElement.AppendChild(contentElement);
			}

			// DynamicTrigger
			if (dynamicTrigger != null && dynamicTrigger.enabled) {
				XmlElement contentElement = TriggerHelpers.Build.Dynamic(dynamicTrigger, xml);
				mainElement.AppendChild(contentElement);
			}

			// Mode
			Helpers.ApplyWriteMode(go, mainElement);

			// Parent
			XmlNode targetParent = parentElement ?? node.FirstChild;

			var repeater = go.GetComponent<AppendRepeater>();
			if (repeater != null && repeater.enabled) {
				for (int i = 0; i < repeater.Multiplier; i++) {
					XmlNode clone = mainElement.CloneNode(true);
					targetParent.AppendChild(clone);
				}
			}

			targetParent.AppendChild(mainElement);
		}

		public static class TriggerHelpers {
			public static class Build {
				public static XmlElement Static(
					TriggerSettings triggerSettings,

					XmlDocument xml
				) {
					XmlElement contentElement = xml.CreateElement("Content");

					// try to parse triggerSettings.Content
					// fallback to fragment if it's not well-formed XML
					XmlDocument tempDoc = new XmlDocument();

					try {
						// wrapper allows multiple top-level nodes
						tempDoc.LoadXml($"<x>{triggerSettings.Content}</x>");

						foreach (XmlNode node in tempDoc.DocumentElement.ChildNodes) {
							// If the node is an outer <Content> element, append its children (unwrap it)
							if (
								node.NodeType == XmlNodeType.Element &&
								string.Equals(node.LocalName, "Content", StringComparison.OrdinalIgnoreCase)
							) {
								foreach (XmlNode inner in node.ChildNodes) {
									contentElement.AppendChild(xml.ImportNode(inner, true));
								}
							} else {
								contentElement.AppendChild(xml.ImportNode(node, true));
							}
						}
					} catch (XmlException) {
						// treat as fragment (best-effort)
						XmlDocumentFragment fragment = xml.CreateDocumentFragment();
						fragment.InnerXml = triggerSettings.Content ?? string.Empty;

						foreach (XmlNode child in fragment.ChildNodes) {
							// if a top-level fragment child is <Content>, unwrap it as above
							if (
								child.NodeType == XmlNodeType.Element &&
								string.Equals(child.LocalName, "Content", StringComparison.OrdinalIgnoreCase)
							) {
								foreach (XmlNode inner in child.ChildNodes) {
									contentElement.AppendChild(inner.CloneNode(true));
								}
							} else {
								contentElement.AppendChild(child.CloneNode(true));
							}
						}
					}

					return contentElement;
				}

				public static XmlElement Dynamic(
					DynamicTrigger dynamicTrigger,

					XmlDocument xml
				) {
					XmlElement contentElement = xml.CreateElement("Content");

					XmlElement initElement = xml.CreateElement("Init");

					XmlElement svActive = xml.CreateElement("SetVariable");
					svActive.SetAttribute("Name", "$Active");
					svActive.SetAttribute("Value", "1");
					initElement.AppendChild(svActive);

					XmlElement svAI = xml.CreateElement("SetVariable");
					svAI.SetAttribute("Name", "$AI");
					svAI.SetAttribute("Value", dynamicTrigger.AIAllowed.ToString());
					initElement.AppendChild(svAI);
	
					XmlElement svNode = xml.CreateElement("SetVariable");
					svNode.SetAttribute("Name", "$Node");
					svNode.SetAttribute("Value", string.IsNullOrEmpty(dynamicTrigger.modelNode) ? "COM" : dynamicTrigger.modelNode);
					initElement.AppendChild(svNode);

					// Animations
					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						string[] splittedAnimations = dynamicTrigger.Animations
							.Split(Vectorier.Core.Game.AttributeSeparator.ToCharArray())
							.Where(s => !string.IsNullOrWhiteSpace(s))
							.Select(s => s.Trim())
							.Distinct()
							.OrderBy(s => s)
							.ThenBy(s => s.Length)
							.ToArray();

						bool multiple = splittedAnimations.Length > 1;
						int counter = 1;

						foreach (string animName in splittedAnimations) {
							XmlElement animVar = xml.CreateElement("SetVariable");
							animVar.SetAttribute("Name", (multiple ? counter.ToString() + "_" : "") + "ReqAnim");
							animVar.SetAttribute("Value", animName);
							initElement.AppendChild(animVar);
							counter++;
						}
					}

					// Sound & Flags
					if (dynamicTrigger.PlaySound) {
						XmlElement svSound = xml.CreateElement("SetVariable");
						svSound.SetAttribute("Name", "Sound");
						svSound.SetAttribute("Value", dynamicTrigger.Sound);
						initElement.AppendChild(svSound);
					}

					XmlElement svFlag = xml.CreateElement("SetVariable");
					svFlag.SetAttribute("Name", "Flag1");
					svFlag.SetAttribute("Value", "0");
					initElement.AppendChild(svFlag);

					contentElement.AppendChild(initElement);

					// Loop
					XmlElement loopElement = xml.CreateElement("Loop");

					// Events
					XmlElement eventsElement = xml.CreateElement("Events");

					XmlElement eventBlockEntry = xml.CreateElement("EventBlock");
					eventBlockEntry.SetAttribute("Template", "FreqUsed." + dynamicTrigger.EventType.ToString());
					eventsElement.AppendChild(eventBlockEntry);

					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						XmlElement eventBlockReqAnim = xml.CreateElement("EventBlock");
						eventBlockReqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");
						eventsElement.AppendChild(eventBlockReqAnim);
					}

					loopElement.AppendChild(eventsElement);

					// Conditions
					if (!string.IsNullOrEmpty(dynamicTrigger.Animations)) {
						string[] splittedAnimations = dynamicTrigger.Animations
							.Split(Vectorier.Core.Game.AttributeSeparator.ToCharArray())
							.Where(s => !string.IsNullOrWhiteSpace(s))
							.Select(s => s.Trim())
							.Distinct()
							.OrderBy(s => s)
							.ThenBy(s => s.Length)
							.ToArray();

						XmlElement conditionsElement = xml.CreateElement("Conditions");

						if (splittedAnimations.Length == 1) {
							XmlElement conditionBlock = xml.CreateElement("ConditionBlock");
							conditionBlock.SetAttribute("Template", "CommonLib.RequiredAnimation");
							conditionsElement.AppendChild(conditionBlock);
						} else {
							XmlElement conditionsOperatorElement = xml.CreateElement("Operator");
							conditionsOperatorElement.SetAttribute("Type", "Or");

							int counter = 1;

							foreach (string name in splittedAnimations) {
								XmlElement conditionBlock = xml.CreateElement("ConditionBlock");
								conditionBlock.SetAttribute("Template", "CommonLib.RequiredAnimation");
								conditionBlock.SetAttribute("Prefix", counter.ToString() + "_");
								conditionsOperatorElement.AppendChild(conditionBlock);
								counter++;
							}

							if (conditionsElement.HasChildNodes) {
								conditionsElement.AppendChild(conditionsOperatorElement);
							}
						}

						if (conditionsElement.HasChildNodes) {
							loopElement.AppendChild(conditionsElement);
						}
					}

					// Actions
					XmlElement actionsElement = xml.CreateElement("Actions");

					string randInt = UnityEngine.Random.Range((int)1E8, (int)1E9 - 1).ToString();

					if (dynamicTrigger.MultipleTransformation) {
						string[] transforms = dynamicTrigger.TransformationNames // 262
							.Where(s => !string.IsNullOrWhiteSpace(s))
							.Select(s => s.Trim())
							.Distinct()
							.ToArray();

						string order = dynamicTrigger.Order.ToString();

						XmlElement chooseElement = xml.CreateElement("Choose");
						chooseElement.SetAttribute("Order", order);

						if (dynamicTrigger.Set < 0) {
							chooseElement.SetAttribute("Set", "0");
						} else if (dynamicTrigger.Set == 0) {
							// chooseElement.SetAttribute("Set", dynamicTrigger.TransformationNames.Count.ToString());
						} else {
							chooseElement.SetAttribute("Set", dynamicTrigger.Set.ToString());
						}

						if (order.ToLower() != "straight") {
							transforms = transforms // 281
								.OrderBy(s => s)
								.ThenBy(s => s.Length)
								.ToArray();
						}

						foreach (string transformationName in transforms) {
							if (!string.IsNullOrEmpty(transformationName) && !transformationName.StartsWith(">")) {
								XmlElement transformElement = xml.CreateElement("Transform");
								transformElement.SetAttribute("Name", transformationName);
								chooseElement.AppendChild(transformElement);
							}
						}

						actionsElement.AppendChild(chooseElement);
					} else {
						XmlElement transformElement = xml.CreateElement("Transform");
						string tr_name = string.IsNullOrEmpty(dynamicTrigger.TriggerTransformName) ? randInt : dynamicTrigger.TriggerTransformName;
						transformElement.SetAttribute("Name", tr_name);
						actionsElement.AppendChild(transformElement);
					}

					if (dynamicTrigger.PlaySound) {
						XmlElement actionBlockSound = xml.CreateElement("ActionBlock");
						actionBlockSound.SetAttribute("Template", "CommonLib.Sound");
						actionsElement.AppendChild(actionBlockSound);

						if (dynamicTrigger.Latency > 0f) {
							XmlElement waitElement = xml.CreateElement("Wait");
							waitElement.SetAttribute("Frames", Math.Round(dynamicTrigger.Latency * 60).ToString());
							actionsElement.AppendChild(waitElement);
						}
					}

					loopElement.AppendChild(actionsElement);

					contentElement.AppendChild(loopElement);

					return contentElement;
				}
			}
		}
	}
}