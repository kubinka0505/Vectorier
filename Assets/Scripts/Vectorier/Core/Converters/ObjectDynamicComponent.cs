using UnityEngine;
using System.Xml;
using System.Collections.Generic;

#nullable enable
#pragma warning disable CS8602
#pragma warning disable CS8603

// -=-=-=- //

namespace Vectorier.Core.Components {
	public class ObjectDynamicComponent {
		internal readonly struct DynamicData {
			public readonly string Name;
			public readonly Transform Transform;
			public readonly MovementData[] Moves;

			public DynamicData(string name, Transform transform, MovementData[] moves) {
				Name = name;
				Transform = transform;
				Moves = moves;
			}
		}

		internal readonly struct MovementData {
			public readonly int Index;
			public readonly float Duration;
			public readonly float Delay;
			public readonly float SupportX;
			public readonly float SupportY;
			public readonly float MoveX;
			public readonly float MoveY;

			public MovementData(
				int index,
				float duration,
				float delay,
				float supportX,
				float supportY,
				float moveX,
				float moveY
			) {
				Index = index;
				Duration = duration;
				Delay = delay;
				SupportX = supportX;
				SupportY = supportY;
				MoveX = moveX;
				MoveY = moveY;
			}
		}

		private static void DispatchRecursive(
			GameObject go,

			XmlElement currentObjectElement,
			XmlElement currentContentElement,

			bool legacy
		) {
			if (!Vectorier.Utils.GameObjectParser.Parse(go).IsVisible()) {
				return;
			}

			int P(string key) => Vectorier.Settings.Core.GetPrecision(key);

			// Tag-first dispatch order
			switch (go.tag) {
				case "EditorOnly":
				case "Unused":
					return;

				case "Dynamic":
					Convert(go, currentContentElement, legacy);
					return;

				case "Platform":
					Components.PlatformComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Trapezoid":
					Components.TrapezoidComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Area":
					Components.AreaComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Trigger":
					Components.TriggerComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Model":
					Components.ModelComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Animation":
					Components.AnimationComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Object":
					Components.ObjectStaticComponent.Convert(go, currentObjectElement, currentContentElement, P(go.tag), legacy);
					return;

				case "Image":
					Components.Image.Convert(
						go: go,
						node: currentObjectElement,
						parentElement: currentContentElement,
						floatPrecision: Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.ImagePrecisionKey),
						legacy: legacy
					);
					return;

				case "Untagged":
					foreach (Transform childTransform in go.transform) {
						DispatchRecursive(childTransform.gameObject, currentObjectElement, currentContentElement, legacy);
					}
					return;
			}

			// Component fallback for untagged / unhandled objects with image components
			if (go.GetComponent<UnityEngine.UI.Image>() != null || go.GetComponent<SpriteRenderer>() != null) {
				Components.Image.Convert(
					go: go,
					node: currentObjectElement,
					parentElement: currentContentElement,
					floatPrecision: Vectorier.Settings.Core.GetPrecision(Vectorier.Settings.Core.Elements.ImagePrecisionKey),
					legacy: legacy
				);
			}
		}

		// =========================================================
		// HELPERS
		// =========================================================

		public static class Helpers {
			internal static class DynamicExtractor {
				public static DynamicData Extract(GameObject go, Dynamic dyn) {
					var moves = new List<MovementData>(16);

					for (int i = 1; i <= 16; i++) {
						if (!dyn.MovementUsage.UseMovement(i)) continue;

						var m = GetMove(dyn, i);
						if (m == null) continue;

						moves.Add(new MovementData(
							i,
							m.MoveDuration,
							m.Delay,
							m.SupportXAxis,
							m.SupportYAxis,
							m.MoveXAxis,
							m.MoveYAxis
						));
					}

					return new DynamicData(
						dyn.TransformationName,
						go.transform,
						moves.ToArray()
					);
				}

				private static Dynamic.Movement? GetMove(Dynamic d, int i) {
					return i switch {
						1 => d.MoveInterval1,
						2 => d.MoveInterval2,
						3 => d.MoveInterval3,
						4 => d.MoveInterval4,
						5 => d.MoveInterval5,
						6 => d.MoveInterval6,
						7 => d.MoveInterval7,
						8 => d.MoveInterval8,
						9 => d.MoveInterval9,
						10 => d.MoveInterval10,
						11 => d.MoveInterval11,
						12 => d.MoveInterval12,
						13 => d.MoveInterval13,
						14 => d.MoveInterval14,
						15 => d.MoveInterval15,
						16 => d.MoveInterval16,
						_ => null
					};
				}
			}

			internal static class DynamicXmlBuilder {
				public static void Build(
					XmlDocument xml,
					XmlElement moveElement,
					DynamicData data,
					GameObject go,
					bool legacy
				) {
					foreach (var m in data.Moves) {
						int rawFramesToMove = Mathf.RoundToInt(
							m.Duration * Vectorier.Core.Game.Properties.FrameRate
						);

						float moveX = m.MoveX * Vectorier.Core.Game.Properties.UnitScale;
						float moveY = -m.MoveY * Vectorier.Core.Game.Properties.UnitScale;

						float supportX = m.SupportX * Vectorier.Core.Game.Properties.UnitScale;
						float supportY = -m.SupportY * Vectorier.Core.Game.Properties.UnitScale;

						if (rawFramesToMove <= 0 || (moveX == 0 && moveY == 0)) {
							Debug.LogWarning($@"Transformation interval {m.Index} in GameObject named ""{go.name}"" is empty", go);
							continue;
						}

						int framesToMove = Mathf.Max(1, rawFramesToMove);

						var interval = xml.CreateElement("MoveInterval");
						interval.SetAttribute("Number", m.Index.ToString());
						interval.SetAttribute("FramesToMove", framesToMove.ToString());

						int delayFrames = Mathf.RoundToInt(
							m.Delay * Vectorier.Core.Game.Properties.FrameRate
						);

						if (delayFrames != 0) {
							interval.SetAttribute("Delay", delayFrames.ToString());
						}

						var start = xml.CreateElement("Point");
						start.SetAttribute("Name", "Start");
						interval.AppendChild(start);

						var support = xml.CreateElement("Point");
						support.SetAttribute("Name", "Support");
						support.SetAttribute("Number", m.Index.ToString());

						if (legacy) {
							if (supportX != 0) {
								support.SetAttribute("X", Vectorier.Core.Helpers.Format.ToString(supportX));
							}

							if (supportY != 0) {
								support.SetAttribute("Y", Vectorier.Core.Helpers.Format.ToString(supportY));
							}
						} else {
							support.SetAttribute("X", Vectorier.Core.Helpers.Format.ToString(supportX));
							support.SetAttribute("Y", Vectorier.Core.Helpers.Format.ToString(supportY));
						}

						var finish = xml.CreateElement("Point");
						finish.SetAttribute("Name", "Finish");

						if (legacy) {
							if (moveX != 0) {
								finish.SetAttribute("X", Vectorier.Core.Helpers.Format.ToString(moveX));
							}

							if (moveY != 0) {
								finish.SetAttribute("Y", Vectorier.Core.Helpers.Format.ToString(moveY));
							}
						} else {
							finish.SetAttribute("X", Vectorier.Core.Helpers.Format.ToString(moveX));
							finish.SetAttribute("Y", Vectorier.Core.Helpers.Format.ToString(moveY));
						}

						interval.AppendChild(support);
						interval.AppendChild(finish);

						moveElement.AppendChild(interval);
					}
				}
			}

			public static bool HasDynamicAncestor(Transform transform) {
				Transform? current = transform.parent;

				while (current != null) {

					if (current.CompareTag("Dynamic")) {
						return true;
					}

					current = current.parent;
				}

				return false;
			}

			public static void ProcessTransformations(
				GameObject go,

				XmlElement objectElement,

				bool legacy
			) {
				XmlDocument xml = objectElement.OwnerDocument;
				var dynamicComponents = go.GetComponents<Dynamic>();

				bool hasDynamic = dynamicComponents != null && dynamicComponents.Length > 0;

				if (!hasDynamic) {
					objectElement.SetAttribute(
						"Labels",
						string.Join(
							Vectorier.Core.Game.Properties.AttributeSeparator,
							Vectorier.Core.Helpers.Get.Name(go).Split()
						)
					);
					return;
				}

				XmlElement propertiesElement = xml.CreateElement("Properties");
				XmlElement dynamicRootElement = xml.CreateElement("Dynamic");

				foreach (var dyn in dynamicComponents) {
					if (dyn == null || !dyn.enabled) {
						continue;
					}

					if (string.IsNullOrWhiteSpace(dyn.TransformationName)) {
						Debug.LogError($"{go.name} dynamic component has no transformation name", go);
						continue;
					}

					var data = DynamicExtractor.Extract(go, dyn);

					XmlElement transformationElement = xml.CreateElement("Transformation");
					transformationElement.SetAttribute("Name", data.Name);

					XmlElement moveElement = xml.CreateElement("Move");

					DynamicXmlBuilder.Build(xml, moveElement, data, go, legacy);

					if (moveElement.HasChildNodes) {
						transformationElement.AppendChild(moveElement);
					}

					if (transformationElement.HasChildNodes) {
						dynamicRootElement.AppendChild(transformationElement);
					}
				}

				if (dynamicRootElement.HasChildNodes) {
					propertiesElement.AppendChild(dynamicRootElement);
				}

				if (propertiesElement.HasChildNodes) {
					objectElement.AppendChild(propertiesElement);
				}

				Vectorier.Core.Helpers.WriteMode.Apply(go, objectElement);
			}
		}

		public static void Convert(
			GameObject go,
			XmlNode node,
			bool legacy
		) {
			if (!Vectorier.Utils.GameObjectParser.Parse(go).IsVisible()) {
				return;
			}
			XmlDocument doc = node.OwnerDocument ?? (XmlDocument)node;

			XmlElement objectElement = doc.CreateElement("Object");

			Vectorier.Core.Helpers.WriteMode.Apply(go, objectElement);

			objectElement.SetAttribute("Labels", go.name);

			Helpers.ProcessTransformations(go, objectElement, legacy);

			XmlElement contentElement = doc.CreateElement("Content");

			node.AppendChild(objectElement);
			objectElement.AppendChild(contentElement);

			// RESTORED: Directly recursive dispatch using original world transform
			foreach (Transform childTransform in go.transform) {
				DispatchRecursive(childTransform.gameObject, objectElement, contentElement, legacy);
			}

			if (!contentElement.HasChildNodes) {
				objectElement.RemoveChild(contentElement);
			}
		}
	}
}