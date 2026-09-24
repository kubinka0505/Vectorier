using UnityEditor;
using UnityEngine;

using System;
using System.IO;
using System.Xml;
using System.Linq;

using Vectorier.Core.Helpers;

using Debug = Logger.Debug;

#nullable enable
#pragma warning disable CS8602
#pragma warning disable CS8603

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Image {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement? parentElement = null,

			int floatPrecision = -1,
			bool legacy = false
		) {
			if (!ImageRules.CanExport(go, legacy)) {
				return;
			}

			var data = ImageExtractor.Extract(go);

			var xml = node.OwnerDocument!;
			var element = ImageXmlBuilder.Build(
				xml,
				go,
				data,
				floatPrecision
			);

			var parent = parentElement ?? node.FirstChild;
			if (parent == null) {
				return;
			}

			ImageXmlBuilder.Append(parent, element, go);
		}
	}

	internal static class ImageRules {
		public static bool CanExport(
			GameObject go,
			bool legacy
		) {
			if (!Helpers.WriteMode.CanWrite(go, legacy)) {
				return false;
			}

			var sr = go.GetComponent<SpriteRenderer>();

			return
				sr != null &&
				sr.enabled &&
				sr.sprite != null;
		}
	}

	internal sealed class ImageData {
		public string? ClassName;

		public SpriteRenderer? SpriteRenderer;

		public bool HasDynamicSize;

		public float ScaleX;
		public float ScaleY;
	}

	internal static class ImageExtractor {
		public static ImageData Extract(GameObject go) {
			var sr = go.GetComponent<SpriteRenderer>();

			string path = AssetDatabase.GetAssetPath(sr.sprite);

			path = Vectorier.Utils.Paths.From(
				Path.Combine(Application.dataPath, "Resources", "Textures")
			).RelativeTo(path);

			path = Path.ChangeExtension(path, null);

			path = path.Replace(
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			);

			var dynamicSize = go.GetComponent<DynamicSize>();

			bool hasDynamicSize =
				dynamicSize != null &&
				dynamicSize.enabled &&
				dynamicSize.Size.MoveDuration > 0 &&
				(
					dynamicSize.Size.FinalWidth > 0 ||
					dynamicSize.Size.FinalHeight > 0
				);

			var scale = go.transform.lossyScale;

			return new ImageData {
				ClassName = path,

				SpriteRenderer = sr,

				HasDynamicSize = hasDynamicSize,

				ScaleX = scale.x,
				ScaleY = scale.y
			};
		}
	}

	internal static class ImageXmlBuilder {
		public static XmlElement Build(
			XmlDocument xml,

			GameObject go,
			ImageData data,

			int precision
		) {
			var image = xml.CreateElement("Image");

			image.SetAttribute(
				"ClassName",
				data.ClassName
			);

			// Animated sprite
			switch (data.SpriteRenderer.maskInteraction) {
				case SpriteMaskInteraction.VisibleInsideMask:
					image.SetAttribute("Type", "2");
					break;

				case SpriteMaskInteraction.VisibleOutsideMask:
					image.SetAttribute("Type", "3");
					break;
			}

			var properties = xml.CreateElement("Properties");

			ImageHelpers.ApplyStaticRotation(go, image, properties, xml);
			ImageHelpers.ApplyStaticColor(go, image, properties, xml);

			ImageHelpers.ApplyDynamicColor(go, properties, xml);
			ImageHelpers.ApplyDynamicRotate(go, properties, xml);

			if (
				!data.HasDynamicSize &&
				(data.ScaleX == 0 || data.ScaleY == 0)
			) {
				Debug.LogError($@"""{go.name}"" has invalid image sprite scale, skipping", go);
				return null;
			}

			if (data.HasDynamicSize) {
				ImageHelpers.ApplyDynamicSize(
					go,
					properties,
					xml
				);
			}

			if (properties.HasChildNodes) {
				image.AppendChild(properties);
			}

			Helpers.WriteMode.Apply(go, image);

			return image;
		}

		public static void Append(
			XmlNode? parent,
			XmlElement image,
			GameObject go
		) {
			if (image == null) {
				return;
			}

			var repeater = go.GetComponent<AppendRepeater>();

			if (repeater != null && repeater.enabled) {
				for (int i = 0; i < repeater.Multiplier; i++) {
					parent.AppendChild(
						image.CloneNode(true)
					);
				}
			}

			parent.AppendChild(image);
		}
	}

	public static class ImageHelpers {
		public static class AffineTransformation {
			public struct Matrix {
				public float A, B, C, D, Tx, Ty;
				public float TopLeftX, TopLeftY;
				public float BoundingWidth, BoundingHeight;
				public int NativeWidth, NativeHeight;
			}

			public static Matrix? Compute(GameObject go) {
				if (go == null) {
					return null;
				}

				var spriteRenderer = go.GetComponent<SpriteRenderer>();
				if (spriteRenderer == null || spriteRenderer.sprite == null) {
					return null;
				}

				var sprite = spriteRenderer.sprite;
				if (sprite.texture == null) {
					return null;
				}

				Transform transform = go.transform;
				Vector3 worldEuler = transform.rotation.eulerAngles;

				bool flipX = spriteRenderer.flipX;
				bool flipY = spriteRenderer.flipY;

				// skip matrix
				bool identity = IsIdentityEquivalent(
					Mathf.Repeat(worldEuler.x, 360f),
					Mathf.Repeat(worldEuler.y, 360f),
					Mathf.Repeat(worldEuler.z, 360f),
					flipX,
					flipY
				);

				if (identity) {
					return null;
				}

				// correct sprite dimensions (NOT texture)
				float nativeWidth = sprite.rect.width;
				float nativeHeight = sprite.rect.height;

				float PPU = Vectorier.Core.Game.Properties.UnitScale; // pixels per unit
				float PTW = 1f / PPU;

				// pivot in world units
				Vector2 pivot = sprite.pivot * PTW;

				// dimensions in world units
				float width = nativeWidth * PTW;
				float height = nativeHeight * PTW;

				float signX = flipX ? -1f : 1f;
				float signY = flipY ? -1f : 1f;

				// pivot-correct local corners (Top-Left origin)
				Vector3 localTopLeft = new Vector3(-pivot.x, height - pivot.y, 0f);
				Vector3 localTopRight = localTopLeft + new Vector3(signX * width, 0f, 0f);
				Vector3 localBottomLeft = localTopLeft + new Vector3(0f, -signY * height, 0f);
				Vector3 localBottomRight = localTopLeft + new Vector3(signX * width, -signY * height, 0f);

				// transform to world
				Vector3 worldTopLeft = transform.TransformPoint(localTopLeft);
				Vector3 worldTopRight = transform.TransformPoint(localTopRight);
				Vector3 worldBottomLeft = transform.TransformPoint(localBottomLeft);
				Vector3 worldBottomRight = transform.TransformPoint(localBottomRight);

				// convert to pixel-space
				Vector2 pointTopLeft = new Vector2(worldTopLeft.x * PPU, -worldTopLeft.y * PPU);
				Vector2 pointTopRight = new Vector2(worldTopRight.x * PPU, -worldTopRight.y * PPU);
				Vector2 pointBottomLeft = new Vector2(worldBottomLeft.x * PPU, -worldBottomLeft.y * PPU);
				Vector2 pointBottomRight = new Vector2(worldBottomRight.x * PPU, -worldBottomRight.y * PPU);

				Vector2 vectorWidth = pointTopRight - pointTopLeft;
				Vector2 vectorHeight = pointBottomLeft - pointTopLeft;

				float A = vectorWidth.x;
				float B = vectorWidth.y;
				float C = vectorHeight.x;
				float D = vectorHeight.y;

				float imagePosX = transform.position.x * PPU;
				float imagePosY = -transform.position.y * PPU;

				float topLeftX = imagePosX + Mathf.Min(0f, A) + Mathf.Min(0f, C);
				float topLeftY = imagePosY + Mathf.Min(0f, B) + Mathf.Min(0f, D);

				float Tx = imagePosX - topLeftX;
				float Ty = imagePosY - topLeftY;

				float minX = Mathf.Min(pointTopLeft.x, Mathf.Min(pointTopRight.x, Mathf.Min(pointBottomLeft.x, pointBottomRight.x)));
				float minY = Mathf.Min(pointTopLeft.y, Mathf.Min(pointTopRight.y, Mathf.Min(pointBottomLeft.y, pointBottomRight.y)));
				float maxX = Mathf.Max(pointTopLeft.x, Mathf.Max(pointTopRight.x, Mathf.Max(pointBottomLeft.x, pointBottomRight.x)));
				float maxY = Mathf.Max(pointTopLeft.y, Mathf.Max(pointTopRight.y, Mathf.Max(pointBottomLeft.y, pointBottomRight.y)));

				return new Matrix {
					A = A,
					B = B,
					C = C,
					D = D,

					Tx = Tx,
					Ty = Ty,

					TopLeftX = topLeftX,
					TopLeftY = topLeftY,
					BoundingWidth = maxX - minX,
					BoundingHeight = maxY - minY,

					NativeWidth = (int)nativeWidth,
					NativeHeight = (int)nativeHeight
				};
			}

			private static bool IsIdentityEquivalent(
				float nx,
				float ny,
				float nz,

				bool flipX,
				bool flipY
			) {
				nx = Mathf.Repeat(nx, 360f);
				ny = Mathf.Repeat(ny, 360f);
				nz = Mathf.Repeat(nz, 360f);

				return 
					(nx == 0f && ny == 0f && nz == 0f && !flipX && !flipY) ||
					(nx == 180f && ny == 0f && nz == 0f && !flipX && flipY) ||
					(nx == 0f && ny == 180f && nz == 0f && flipX && !flipY) ||
					(nx == 0f && ny == 0f && nz == 180f && flipX && flipY) ||
					(nx == 180f && ny == 180f && nz == 0f && flipX && flipY) ||
					(nx == 180f && ny == 0f && nz == 180f && flipX && flipY) ||
					(nx == 0f && ny == 180f && nz == 180f && !flipX && flipY) ||
					(nx == 180f && ny == 180f && nz == 180f && !flipX && !flipY);
			}
		}

		public static void ApplySpriteSize(
			GameObject go,

			XmlElement element
		) {
			var spriteRenderer = go.GetComponent<SpriteRenderer>();

			if (spriteRenderer == null || /*!spriteRenderer.enabled ||*/ spriteRenderer.sprite == null) {
				return;
			}

			Bounds bounds = spriteRenderer.sprite.bounds;
			Vector3 scale = go.transform.lossyScale;

			float width = bounds.size.x * Vectorier.Core.Game.Properties.UnitScale;
			float height = bounds.size.y * Vectorier.Core.Game.Properties.UnitScale;

			float finalWidth = width * scale.x;
			float finalHeight = height * scale.y;

			if (finalWidth != 0) {
				element.SetAttribute("Width", Helpers.Format.ToString(finalWidth));
			}

			if (finalHeight != 0) {
				element.SetAttribute("Height", Helpers.Format.ToString(finalHeight));
			}
		}

		public static void ApplyStaticRotation(
			GameObject go,
			XmlElement mainElement,
			XmlElement propertiesElement,
			XmlDocument xml,
			int floatPrecision = -1
		) {
			var sr = go.GetComponent<SpriteRenderer>();
			if (sr == null || sr.sprite == null) return;

			bool isCenter = Helpers.Get.IsCenterPivot(go);
			var norm = Helpers.BoundNormalizer.Normalize(
				go,
				go.transform.position,
				sr.sprite.bounds.size,
				go.transform.lossyScale,
				go.transform.eulerAngles,
				sr.flipX,
				sr.flipY,
				isCenter,
				BoundNormalizer.ObjectType.Sprite,
				floatPrecision
			);

			if (!norm.IsValid) return;

			if (norm.X != 0) mainElement.SetAttribute("X", Helpers.Format.ToString(norm.X, floatPrecision));
			if (norm.Y != 0) mainElement.SetAttribute("Y", Helpers.Format.ToString(norm.Y, floatPrecision));

			mainElement.SetAttribute("Width", Helpers.Format.ToString(norm.Width, floatPrecision));
			mainElement.SetAttribute("Height", Helpers.Format.ToString(norm.Height, floatPrecision));

			// Write <Static><Matrix> if non-identity transformation exists
			if (norm.HasMatrix) {
				XmlElement staticElement = XML.Utils.General.GetOrCreateElement("Static", propertiesElement, xml);
				XmlElement mat = xml.CreateElement("Matrix");

				if (norm.A != 0) mat.SetAttribute("A", Helpers.Format.ToString(norm.A));
				if (norm.B != 0) mat.SetAttribute("B", Helpers.Format.ToString(norm.B));
				if (norm.C != 0) mat.SetAttribute("C", Helpers.Format.ToString(norm.C));
				if (norm.D != 0) mat.SetAttribute("D", Helpers.Format.ToString(norm.D));
				if (norm.Tx != 0) mat.SetAttribute("Tx", Helpers.Format.ToString(norm.Tx));
				if (norm.Ty != 0) mat.SetAttribute("Ty", Helpers.Format.ToString(norm.Ty));

				staticElement.AppendChild(mat);
			}
		}

		private static void ApplyRegularPlacement(
			GameObject go,

			XmlElement mainElement,

			int floatPrecision = -1
		) {
			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || /*!spriteRenderer.enabled ||*/ spriteRenderer.sprite == null) {
				return;
			}

			Bounds bounds = spriteRenderer.sprite.bounds;
			Vector3 scale = go.transform.lossyScale;

			float imagePosX = go.transform.position.x * Vectorier.Core.Game.Properties.UnitScale;
			float imagePosY = -go.transform.position.y * Vectorier.Core.Game.Properties.UnitScale;

			if (Mathf.Round(imagePosX) != 0) {
				mainElement.SetAttribute("X", Helpers.Format.ToString(Mathf.Round(imagePosX), floatPrecision));
			}

			if (Mathf.Round(imagePosY) != 0) {
				mainElement.SetAttribute("Y", Helpers.Format.ToString(Mathf.Round(imagePosY), floatPrecision));
			}

			float width = bounds.size.x * Vectorier.Core.Game.Properties.UnitScale;
			float height = bounds.size.y * Vectorier.Core.Game.Properties.UnitScale;

			float finalWidth = width * scale.x;
			float finalHeight = height * scale.y;

			mainElement.SetAttribute("Width", finalWidth == 0 ? "" : Helpers.Format.ToString(finalWidth));
			mainElement.SetAttribute("Height", finalHeight == 0 ? "" : Helpers.Format.ToString(finalHeight));

			/*
			if (width * scale.x != width) {
				mainElement.SetAttribute("NativeX", Helpers.Format.ToString(width, floatPrecision));
			}

			if (height * scale.y != height) {
				mainElement.SetAttribute("NativeY", Helpers.Format.ToString(height, floatPrecision));
			}
			*/
		}

		public static void ApplyStaticColor(
			GameObject go,

			XmlElement mainElement,
			XmlElement propertiesElement,

			XmlDocument xml
		) {
			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || /*!spriteRenderer.enabled ||*/ spriteRenderer.sprite == null) {
				return;
			}

			Color color = spriteRenderer.color;
			if (color == Color.white) {
				return;
			}

			string objName = spriteRenderer.sprite != null ? spriteRenderer.sprite.name : Helpers.Get.Name(go);

			int unsupportedTransparencyOmmit = 5;

			if (objName.ToLowerInvariant().EndsWith("_black") && Mathf.RoundToInt(color.a * 100) % unsupportedTransparencyOmmit == 0) {
				// Debug.LogWarning($"\"{objName}\" sprite is hardcoded to have no color support, ignoring. [click to toggle]\nHint: To disable this warning, set transparency to value divisible by {unsupportedTransparencyOmmit}", go);
				return;
			}

			XmlElement staticElement = XML.Utils.General.GetOrCreateElement("Static", propertiesElement, xml);

			string alphaHex = Mathf.RoundToInt(color.a * 255).ToString("X2");
			string rgbaColor = ColorUtility.ToHtmlStringRGB(color) + alphaHex;

			XmlElement colorElement = xml.CreateElement("StartColor");
			colorElement.SetAttribute("Color", $"#{rgbaColor}");
			staticElement.AppendChild(colorElement);
		}

		// Dynamic
		public static void ApplyDynamicColor(
			GameObject go,
			XmlElement propertiesElement,
			XmlDocument xml
		) {
			var dynamicColors = go.GetComponents<DynamicColor>();
			if (dynamicColors == null || dynamicColors.Length == 0) {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || spriteRenderer.sprite == null) {
				return;
			}

			XmlElement? dynamicElement = null;
			Color currentStartColor = spriteRenderer.color;

			foreach (var dynamicColor in dynamicColors) {
				if (dynamicColor == null || !dynamicColor.enabled) {
					continue;
				}

				Color finishColor = dynamicColor.ChangeToColor;

				// Skip identical start/finish colors
				if (currentStartColor.Equals(finishColor)) {
					continue;
				}

				// Lazy-fetch/create <Dynamic> element only when valid transformation exists
				dynamicElement ??= XML.Utils.General.GetOrCreateElement("Dynamic", propertiesElement, xml);

				XmlElement transformationElement = xml.CreateElement("Transformation");
				transformationElement.SetAttribute("Name", dynamicColor.TransformationName);

				float totalDuration = dynamicColor.Duration;
				int totalFrames = Mathf.CeilToInt(totalDuration * 60);

				int[] stepWeights = dynamicColor.Easing switch {
					DynamicColor.EasingTypes.Linear     => new[] { 1 },
					DynamicColor.EasingTypes.EaseIn     => new[] { 1, 2, 3, 4, 5 },
					DynamicColor.EasingTypes.EaseOut    => new[] { 5, 4, 3, 2, 1 },
					DynamicColor.EasingTypes.EaseInOut  => new[] { 1, 2, 3, 2, 1 },
					DynamicColor.EasingTypes.EaseOutIn  => new[] { 3, 2, 1, 2, 3 },
					_                                  => new[] { 1 }
				};

				int totalWeight = stepWeights.Sum();
				int steps = stepWeights.Length;

				int[] framesPerStepArr = new int[steps];
				int framesAllocated = 0;

				for (int i = 0; i < steps; i++) {
					float exactFrames = (float)totalFrames * stepWeights[i] / totalWeight;
					framesPerStepArr[i] = Mathf.FloorToInt(exactFrames);
					framesAllocated += framesPerStepArr[i];
				}

				int remainder = totalFrames - framesAllocated;
				while (remainder-- > 0) {
					int maxIndex = Array.IndexOf(stepWeights, stepWeights.Max());
					framesPerStepArr[maxIndex]++;
				}

				Color startLinear = currentStartColor.linear;
				Color finishLinear = finishColor.linear;

				float colorStepUnit = 1f / steps;
				float accumulatedProgress = 0f;

				for (int i = 0; i < steps; i++) {
					accumulatedProgress = Mathf.Clamp01(accumulatedProgress + colorStepUnit);
					Color stepColor = Color.Lerp(startLinear, finishLinear, accumulatedProgress);

					string stepColorHex =
						ColorUtility.ToHtmlStringRGB(stepColor.gamma) +
						Mathf.RoundToInt(stepColor.a * 255).ToString("X2");

					XmlElement colorElement = xml.CreateElement("Color");
					colorElement.SetAttribute("ColorFinish", $"#{stepColorHex}");
					colorElement.SetAttribute("Frames", framesPerStepArr[i].ToString());

					transformationElement.AppendChild(colorElement);
				}

				dynamicElement.AppendChild(transformationElement);

				// Update color for sequential DynamicColor components chaining
				currentStartColor = finishColor;
			}
		}

		public static void ApplyDynamicRotate(
			GameObject go,
			XmlElement propertiesElement,
			XmlDocument xml
		) {
			var dynamicRotates = go.GetComponents<DynamicRotate>();
			if (dynamicRotates == null || dynamicRotates.Length == 0) {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || spriteRenderer.sprite == null) {
				return;
			}

			XmlElement? dynamicElement = null;

			foreach (var dynamicRotate in dynamicRotates) {
				if (
					dynamicRotate == null ||
					!dynamicRotate.enabled ||
					dynamicRotate.Rotation.Duration <= 0 ||
					Mathf.Approximately(dynamicRotate.Rotation.Angle, 0f)
				) {
					continue;
				}

				// Lazy-fetch/create <Dynamic> element only when valid transformation exists
				dynamicElement ??= XML.Utils.General.GetOrCreateElement("Dynamic", propertiesElement, xml);

				float imageWidth = spriteRenderer.sprite.texture.width * go.transform.lossyScale.x;
				float imageHeight = spriteRenderer.sprite.texture.height * go.transform.lossyScale.y;

				// Compute anchor
				string sep = Vectorier.Core.Game.Properties.AttributeSeparator;
				string anchorValue = $"0{sep}0";

				switch (dynamicRotate.Rotation.Anchor) {
					case DynamicRotate.AnchorPoints.TopRight:
						anchorValue = $"{imageWidth}{sep}0";
						break;
					case DynamicRotate.AnchorPoints.BottomLeft:
						anchorValue = $"0{sep}{imageHeight}";
						break;
					case DynamicRotate.AnchorPoints.BottomRight:
						anchorValue = $"{imageWidth}{sep}{imageHeight}";
						break;
					case DynamicRotate.AnchorPoints.Center:
						anchorValue = $"{imageWidth / 2}{sep}{imageHeight / 2}";
						break;
				}

				// Create <Transformation> element
				XmlElement transformationElement = xml.CreateElement("Transformation");
				transformationElement.SetAttribute("Name", dynamicRotate.TransformationName);

				float totalAngle = -dynamicRotate.Rotation.Angle; // degrees
				float totalDuration = dynamicRotate.Rotation.Duration; // seconds
				int totalFrames = Mathf.CeilToInt(totalDuration * 60);

				int[] stepWeights = dynamicRotate.Rotation.Easing switch {
					DynamicRotate.EasingTypes.Linear   => new[] { 1 },
					DynamicRotate.EasingTypes.EaseIn   => new[] { 1, 2, 3, 4, 5 },
					DynamicRotate.EasingTypes.EaseOut  => new[] { 5, 4, 3, 2, 1 },
					DynamicRotate.EasingTypes.EaseInOut => new[] { 1, 2, 3, 2, 1 },
					DynamicRotate.EasingTypes.EaseOutIn => new[] { 3, 2, 1, 2, 3 },
					_                                  => new[] { 1 }
				};

				int totalWeight = stepWeights.Sum();
				int steps = stepWeights.Length;

				// Angle split: even per step
				float anglePerStep = totalAngle / steps;

				// Compute frames per step proportional to weight
				int[] framesPerStepArr = new int[steps];
				int framesAllocated = 0;
				for (int i = 0; i < steps; i++) {
					float exactFrames = (float)totalFrames * stepWeights[i] / totalWeight;
					framesPerStepArr[i] = Mathf.FloorToInt(exactFrames);
					framesAllocated += framesPerStepArr[i];
				}

				// Distribute remaining frames to largest-weight steps
				int remainder = totalFrames - framesAllocated;
				while (remainder > 0) {
					int maxIndex = 0;
					int maxWeight = stepWeights[0];

					for (int j = 1; j < steps; j++) {
						if (stepWeights[j] > maxWeight) {
							maxWeight = stepWeights[j];
							maxIndex = j;
						}
					}

					framesPerStepArr[maxIndex]++;
					remainder--;
				}

				// Create <Rotation> elements
				for (int i = 0; i < steps; i++) {
					if (framesPerStepArr[i] <= 0) {
						continue;
					}

					XmlElement rotationElement = xml.CreateElement("Rotation");

					rotationElement.SetAttribute("Angle", Vectorier.Core.Helpers.Format.ToString(anglePerStep));
					rotationElement.SetAttribute("Anchor", anchorValue);
					rotationElement.SetAttribute("Frames", framesPerStepArr[i].ToString());

					transformationElement.AppendChild(rotationElement);
				}

				dynamicElement.AppendChild(transformationElement);
			}
		}

		public static void ApplyDynamicSize(
			GameObject go,
			XmlElement propertiesElement,
			XmlDocument xml
		) {
			var dynamicSizes = go.GetComponents<DynamicSize>();
			if (dynamicSizes == null || dynamicSizes.Length == 0) {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || spriteRenderer.sprite == null) {
				return;
			}

			XmlElement? dynamicElement = null;

			// Base sprite dimensions ("native" size)
			Bounds bounds = spriteRenderer.sprite.bounds;
			Vector3 initialScale = go.transform.lossyScale;

			float nativeWidth = bounds.size.x * Vectorier.Core.Game.Properties.UnitScale;
			float nativeHeight = bounds.size.y * Vectorier.Core.Game.Properties.UnitScale;

			float currentScaleX = initialScale.x;
			float currentScaleY = initialScale.y;

			foreach (var dynamicSize in dynamicSizes) {
				if (
					dynamicSize == null ||
					!dynamicSize.enabled ||
					dynamicSize.Size.MoveDuration <= 0 ||
					(dynamicSize.Size.FinalWidth <= 0 && dynamicSize.Size.FinalHeight <= 0)
				) {
					continue;
				}

				// Lazy-fetch/create <Dynamic> element only when a valid transformation exists
				dynamicElement ??= XML.Utils.General.GetOrCreateElement("Dynamic", propertiesElement, xml);

				XmlElement transformationElement = xml.CreateElement("Transformation");
				transformationElement.SetAttribute("Name", dynamicSize.TransformationName);

				float totalDuration = dynamicSize.Size.MoveDuration;
				int totalFrames = Mathf.CeilToInt(totalDuration * 60f);

				// Easing pattern selection
				int[] stepWeights = dynamicSize.Size.Easing switch {
					DynamicSize.EasingTypes.Linear    => new[] { 1 },
					DynamicSize.EasingTypes.EaseIn    => new[] { 1, 2, 3, 4, 5 },
					DynamicSize.EasingTypes.EaseOut   => new[] { 5, 4, 3, 2, 1 },
					DynamicSize.EasingTypes.EaseInOut => new[] { 1, 2, 3, 2, 1 },
					DynamicSize.EasingTypes.EaseOutIn => new[] { 3, 2, 1, 2, 3 },
					_                                => new[] { 1 }
				};

				int totalWeight = stepWeights.Sum();
				int stepCount = stepWeights.Length;

				// Frame distribution proportional to weight
				int[] framesPerStep = new int[stepCount];
				int framesAllocated = 0;

				for (int i = 0; i < stepCount; i++) {
					float exactFrames = totalFrames * (float)stepWeights[i] / totalWeight;
					framesPerStep[i] = Mathf.FloorToInt(exactFrames);
					framesAllocated += framesPerStep[i];
				}

				// Distribute leftover frames to largest-weight steps
				int remainder = totalFrames - framesAllocated;
				while (remainder-- > 0) {
					int maxIndex = Array.IndexOf(stepWeights, stepWeights.Max());
					framesPerStep[maxIndex]++;
				}

				// Build <Size> elements
				for (int i = 0; i < stepCount; i++) {
					if (framesPerStep[i] <= 0) {
						continue;
					}

					XmlElement sizeElement = xml.CreateElement("Size");
					sizeElement.SetAttribute("Frames", framesPerStep[i].ToString());

					float stepWidth = nativeWidth * currentScaleX * dynamicSize.Size.FinalWidth * stepWeights[i] / totalWeight;
					float stepHeight = nativeHeight * currentScaleY * dynamicSize.Size.FinalHeight * stepWeights[i] / totalWeight;

					if (stepWidth > 0f) {
						sizeElement.SetAttribute("FinalWidth", Helpers.Format.ToString(stepWidth));
					}

					if (stepHeight > 0f) {
						sizeElement.SetAttribute("FinalHeight", Helpers.Format.ToString(stepHeight));
					}

					transformationElement.AppendChild(sizeElement);
				}

				dynamicElement.AppendChild(transformationElement);

				// Update active scale for sequential chaining of multiple size animations
				if (dynamicSize.Size.FinalWidth > 0f) {
					currentScaleX *= dynamicSize.Size.FinalWidth;
				}
				if (dynamicSize.Size.FinalHeight > 0f) {
					currentScaleY *= dynamicSize.Size.FinalHeight;
				}
			}
		}
	}
}