using UnityEngine;

using System;
using System.Xml;
using System.Linq;

using Debug = Logger.Debug;

// -=-=-=- //

namespace Vectorier.Core.Components {
	public static class Image {
		public static void Convert(
			GameObject go,

			XmlNode node,
			XmlElement parentElement = null,

			int floatPrecision = -1
		) {
			string objRegex = Helpers.Get.Name(go);

			if (objRegex == "Camera")
				return;

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || spriteRenderer.sprite == null) {
				return;
			}

			string objName = spriteRenderer.sprite.name;
			XmlDocument xml = node.OwnerDocument;

			XmlElement mainElement = xml.CreateElement("Image");
			mainElement.SetAttribute("ClassName", objName);

			XmlElement propertiesElement = xml.CreateElement("Properties");

			// --- dynamic info ---
			var dynamicSize = go.GetComponent<DynamicSize>();
			bool hasDynamicSize =
				dynamicSize != null &&
				!dynamicSize.enabled &&
				dynamicSize.Size.MoveDuration > 0 &&
				(dynamicSize.Size.FinalWidth > 0 || dynamicSize.Size.FinalHeight > 0);

			// --- static info ---
			Vector3 scale = go.transform.lossyScale;
			float sx = scale.x;
			float sy = scale.y;

			// even if sx/sy == 0, we still need rotation info
			ImageHelpers.ApplyStaticRotation(go, mainElement, propertiesElement, xml);
			ImageHelpers.ApplyStaticColor(go, mainElement, propertiesElement, xml);
			ImageHelpers.ApplyDynamicColor(go, propertiesElement, xml);
			ImageHelpers.ApplyDynamicRotate(go, propertiesElement, xml);

			// only skip removal if both scale are zero AND no dynamic size
			if (!hasDynamicSize && (sx == 0 || sy == 0)) {
				string reason = null;

				if (sx == 0 && sy == 0) {
					reason = hasDynamicSize
						? $"GameObject {objRegex} local scale X and Y is 0 and it has no valid dynamic size component values"
						: $"GameObject {objRegex} local scale X and Y is 0 and it has no dynamic size component";
				} else if (sx == 0) {
					reason = hasDynamicSize
						? $"GameObject {objRegex} local scale X is 0 and it has no valid dynamic size component values"
						: $"GameObject {objRegex} local scale X is 0 and it has no dynamic size component";
				} else if (sy == 0) {
					reason = hasDynamicSize
						? $"GameObject {objRegex} local scale Y is 0 and it has no valid dynamic size component values"
						: $"GameObject {objRegex} local scale Y is 0 and it has no dynamic size component";
				}

				Debug.LogWarning(reason);
				return;
			}

			// append dynamic size (always, if it exists)
			if (hasDynamicSize) {
				ImageHelpers.ApplyDynamicSize(go, propertiesElement, xml);
			}

			// attach properties if anything was written
			if (propertiesElement.HasChildNodes) {
				mainElement.AppendChild(propertiesElement);
			}

			Helpers.ApplyWriteMode(go, mainElement);

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
	}

	public static class ImageHelpers {
		public static class AffineTransformation {
			public struct Matrix {
				public float A, B, C, D, Tx, Ty;
				public float TopLeftX, TopLeftY;
				public float BoundingWidth, BoundingHeight;
				public int NativeWidth, NativeHeight;
			}

			public static Matrix? Compute(GameObject obj) {
				if (obj == null) {
					return null;
				}

				var spriteRenderer = default(SpriteRenderer);

				var renderers = obj.GetComponents<SpriteRenderer>();
				if (renderers == null || renderers.Length == 0) {
					return null;
				}

				spriteRenderer = renderers[0];
				if (spriteRenderer == null || /*!spriteRenderer.enabled ||*/ spriteRenderer.sprite == null) {
					return null;
				}

				Texture2D texture = spriteRenderer.sprite.texture;
				if (texture == null) {
					return null;
				}

				Transform transform = obj.transform;
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

				// otherwise, continue with normal matrix computation
				int nativeWidth = texture.width;
				int nativeHeight = texture.height;

				float PPU = Vectorier.Core.Game.UnitScale; // pixels per unit			
				float PTW = 1f / PPU; // pixels to world

				float signX = flipX ? -1f : 1f;
				float signY = flipY ? -1f : 1f;

				// Local corners (in world units)
				Vector3 localTopLeft = new Vector3(0f, 0f, 0f);
				Vector3 localTopRight = new Vector3(signX * nativeWidth * PTW, 0f, 0f);
				Vector3 localBottomLeft = new Vector3(0f, -signY * nativeHeight * PTW, 0f);
				Vector3 localBottomRight = new Vector3(signX * nativeWidth * PTW, -signY * nativeHeight * PTW, 0f);

				// Transform to world
				Vector3 worldTopLeft = transform.TransformPoint(localTopLeft);
				Vector3 worldTopRight = transform.TransformPoint(localTopRight);
				Vector3 worldBottomLeft = transform.TransformPoint(localBottomLeft);
				Vector3 worldBottomRight = transform.TransformPoint(localBottomRight);

				// Convert to pixel-space
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

					NativeWidth = nativeWidth,
					NativeHeight = nativeHeight
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

			float width = bounds.size.x * Vectorier.Core.Game.UnitScale;
			float height = bounds.size.y * Vectorier.Core.Game.UnitScale;

			float finalWidth = width * scale.x;
			float finalHeight = height * scale.y;

			if (finalWidth != 0) {
				element.SetAttribute("Width", Helpers.ToString(finalWidth));
			}

			if (finalHeight != 0) {
				element.SetAttribute("Height", Helpers.ToString(finalHeight));
			}
		}

		public static void ApplyStaticRotation(
			GameObject go,

			XmlElement mainElement,
			XmlElement propertiesElement,
			XmlDocument xml,
	
			int floatPrecision = -1
		) {
			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || /*!spriteRenderer.enabled ||*/ spriteRenderer.sprite == null) {
				return;
			}

			// Get affine matrix (rotation / flip / shear)
			var matrix = AffineTransformation.Compute(go);

			// If no rotation or flipping → fallback
			if (matrix == null) {
				ApplyRegularPlacement(go, mainElement, floatPrecision);
				return;
			}

			// Safe non-null struct
			var m = matrix.Value;

			// Create Static element
			XmlElement staticElement = XML.Utils.GetOrCreateElement("Static", propertiesElement, xml);

			float roundedX = Mathf.Round(m.TopLeftX);
			float roundedY = Mathf.Round(m.TopLeftY);

			// Position (top-left)
			if (roundedX != 0) {
				mainElement.SetAttribute("X", Helpers.ToString(roundedX, floatPrecision));
			}

			if (roundedY != 0) {
				mainElement.SetAttribute("Y", Helpers.ToString(roundedY, floatPrecision));
			}

			// Bounding box
			mainElement.SetAttribute("Width", Helpers.ToString(m.BoundingWidth));
			mainElement.SetAttribute("Height", Helpers.ToString(m.BoundingHeight));

			// Native dims
			if (m.NativeWidth != Mathf.Round(m.BoundingWidth)) {
				mainElement.SetAttribute("NativeX", Helpers.ToString(m.NativeWidth));
			}

			if (m.NativeHeight != Mathf.Round(m.BoundingHeight)) {
				mainElement.SetAttribute("NativeY", Helpers.ToString(m.NativeHeight));
			}

			// Write <Matrix>
			XmlElement mat = xml.CreateElement("Matrix");

			if (m.A != 0) { mat.SetAttribute("A", Helpers.ToString(m.A)); }
			if (m.B != 0) { mat.SetAttribute("B", Helpers.ToString(m.B)); }
			if (m.C != 0) { mat.SetAttribute("C", Helpers.ToString(m.C)); }
			if (m.D != 0) { mat.SetAttribute("D", Helpers.ToString(m.D)); }
			if (m.Tx != 0) { mat.SetAttribute("Tx", Helpers.ToString(m.Tx)); }
			if (m.Ty != 0) { mat.SetAttribute("Ty", Helpers.ToString(m.Ty)); }

			staticElement.AppendChild(mat);
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

			float imagePosX = go.transform.position.x * Vectorier.Core.Game.UnitScale;
			float imagePosY = -go.transform.position.y * Vectorier.Core.Game.UnitScale;

			if (Mathf.Round(imagePosX) != 0) {
				mainElement.SetAttribute("X", Helpers.ToString(Mathf.Round(imagePosX), floatPrecision));
			}

			if (Mathf.Round(imagePosY) != 0) {
				mainElement.SetAttribute("Y", Helpers.ToString(Mathf.Round(imagePosY), floatPrecision));
			}

			float width = bounds.size.x * Vectorier.Core.Game.UnitScale;
			float height = bounds.size.y * Vectorier.Core.Game.UnitScale;

			float finalWidth = width * scale.x;
			float finalHeight = height * scale.y;

			mainElement.SetAttribute("Width", finalWidth == 0 ? "" : Helpers.ToString(finalWidth));
			mainElement.SetAttribute("Height", finalHeight == 0 ? "" : Helpers.ToString(finalHeight));

			if (width * scale.x != width) {
				mainElement.SetAttribute("NativeX", Helpers.ToString(width, floatPrecision));
			}

			if (height * scale.y != height) {
				mainElement.SetAttribute("NativeY", Helpers.ToString(height, floatPrecision));
			}
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

			XmlElement staticElement = XML.Utils.GetOrCreateElement("Static", propertiesElement, xml);

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

			// create or get <Dynamic> once
			XmlElement dynamicElement = XML.Utils.GetOrCreateElement("Dynamic", propertiesElement, xml);

			foreach (var dynamicColor in dynamicColors) {
				if (!dynamicColor.enabled) {
					continue;
				}

				Color startColor = spriteRenderer.color;
				Color finishColor = dynamicColor.ChangeToColor;

				// skip identical colors
				if (startColor.Equals(finishColor)) {
					continue;
				}

				// create <Transformation>
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
					_                               => new[] { 1 }
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

				Color startLinear = startColor.linear;
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
			}

			propertiesElement.AppendChild(dynamicElement);
		}

		public static void ApplyDynamicRotate(
			GameObject go,

			XmlElement propertiesElement,

			XmlDocument xml
		) {
			var dynamicRotate = go.GetComponent<DynamicRotate>();
			if (
				dynamicRotate == null ||
				!dynamicRotate.enabled ||
				dynamicRotate.Rotation.Duration <= 0 ||
				dynamicRotate.Rotation.Angle < float.Epsilon
			) {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			float imageWidth = spriteRenderer.sprite.texture.width * go.transform.lossyScale.x;
			float imageHeight = spriteRenderer.sprite.texture.height * go.transform.lossyScale.y;

			// compute anchor
			string sep = Vectorier.Core.Game.AttributeSeparator;
			string anchorValue = $"0{sep}0";

			switch (dynamicRotate.Rotation.Anchor) {
				case DynamicRotate.AnchorPoints.TopRight:
					anchorValue = $"{imageWidth}{sep}0";
					break;
				case DynamicRotate.AnchorPoints.BottomLeft:
					anchorValue = $"0|{imageHeight}";
					break;
				case DynamicRotate.AnchorPoints.BottomRight:
					anchorValue = $"{imageWidth}{sep}{imageHeight}";
					break;
				case DynamicRotate.AnchorPoints.Center:
					anchorValue = $"{imageWidth / 2}{sep}{imageHeight / 2}";
					break;
			}

			// create <Dynamic> element
			XmlElement dynamicElement = XML.Utils.GetOrCreateElement("Dynamic", propertiesElement, xml);

			// create <Transformation> element
			XmlElement transformationElement = xml.CreateElement("Transformation");
			transformationElement.SetAttribute("Name", dynamicRotate.TransformationName);

			float totalAngle = -dynamicRotate.Rotation.Angle; // degrees
			float totalDuration = dynamicRotate.Rotation.Duration; // seconds
			int totalFrames = Mathf.CeilToInt(totalDuration * 60);

			int[] stepWeights;
			switch (dynamicRotate.Rotation.Easing) {
				case DynamicRotate.EasingTypes.Linear:
					stepWeights = new int[] { 1 };
					break;
				case DynamicRotate.EasingTypes.EaseIn:
					stepWeights = new int[] { 1, 2, 3, 4, 5 };
					break;
				case DynamicRotate.EasingTypes.EaseOut:
					stepWeights = new int[] { 5, 4, 3, 2, 1 };
					break;
				case DynamicRotate.EasingTypes.EaseInOut:
					stepWeights = new int[] { 1, 2, 3, 2, 1 };
					break;
				case DynamicRotate.EasingTypes.EaseOutIn:
					stepWeights = new int[] { 3, 2, 1, 2, 3 };
					break;
				default:
					stepWeights = new int[] { 1 };
					break;
			}

			int totalWeight = stepWeights.Sum();
			int steps = stepWeights.Length;

			// angle split: even per step
			float anglePerStep = totalAngle / steps;

			// compute frames per step proportional to weight and ensure sum == totalFrames
			int[] framesPerStepArr = new int[steps];
			int framesAllocated = 0;
			for (int i = 0; i < steps; i++) {
				// compute proportional frames (float), floor to int
				float exactFrames = (float)totalFrames * stepWeights[i] / totalWeight;
				framesPerStepArr[i] = Mathf.FloorToInt(exactFrames);
				framesAllocated += framesPerStepArr[i];
			}

			// distribute any remaining frames
			// starting from middle-out for better symmetry and due to rounding
			int remainder = totalFrames - framesAllocated;

			while (remainder > 0) {
				// prefer center indices to keep symmetry (middle-first)
				int middle = steps / 2;
	
				// compute a better distribution order: middle, middle+1, middle-1, middle+2, ...
				int offset = (int)((steps - remainder) % steps); // just to vary, if mandatory
				int idx = middle;

				if (remainder % 2 < float.Epsilon) {
					// small attempt, not critical
					idx = middle + (remainder / 2);
				}

				// simpler: give 1 frame to the largest-weight step each loop for stability
				int maxIndex = 0;
				int maxWeight = stepWeights[0];

				for (int j = 1; j < steps; j++) {
					if (stepWeights[j] > maxWeight) { maxWeight = stepWeights[j]; maxIndex = j; }
				}

				framesPerStepArr[maxIndex] += 1;
				remainder--;
			}

			// create <Rotation> elements using even angle per step but different frames
			for (int i = 0; i < steps; i++) {
				if (framesPerStepArr[i] <= 0) {
					// skip degenerate zero-frame steps
					continue;
				}

				XmlElement rotationElement = xml.CreateElement("Rotation");

				rotationElement.SetAttribute("Angle", Vectorier.Core.Helpers.ToString(anglePerStep));
				rotationElement.SetAttribute("Anchor", anchorValue);
				rotationElement.SetAttribute("Frames", framesPerStepArr[i].ToString());

				transformationElement.AppendChild(rotationElement);
			}

			// build hierarchy
			dynamicElement.AppendChild(transformationElement);
			propertiesElement.AppendChild(dynamicElement);
		}

		public static void ApplyDynamicSize(
			GameObject go,

			XmlElement propertiesElement,

			XmlDocument xml
		) {
			var dynamicSize = go.GetComponent<DynamicSize>();
			if (dynamicSize == null ||
				dynamicSize.Size.MoveDuration <= 0 ||
				(dynamicSize.Size.FinalWidth <= 0 && dynamicSize.Size.FinalHeight <= 0)) {
				return;
			}

			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			if (spriteRenderer == null || /*!spriteRenderer.enabled ||*/ spriteRenderer.sprite == null) {
				return;
			}

			// Base sprite dimensions ("native" size)
			Bounds bounds = spriteRenderer.sprite.bounds;
			Vector3 scale = go.transform.lossyScale;

			float nativeWidth = bounds.size.x * Vectorier.Core.Game.UnitScale;
			float nativeHeight = bounds.size.y * Vectorier.Core.Game.UnitScale;

			// Current rendered (scaled) size
			float currentWidth = nativeWidth * scale.x;
			float currentHeight = nativeHeight * scale.y;

			// FinalWidth / FinalHeight are treated as native multipliers
			// 1 = native/original sprite scale
			float totalWidth = nativeWidth * (dynamicSize.Size.FinalWidth - 1f) * scale.x;
			float totalHeight = nativeHeight * (dynamicSize.Size.FinalHeight - 1f) * scale.y;

			float totalDuration = dynamicSize.Size.MoveDuration;
			int totalFrames = Mathf.CeilToInt(totalDuration * 60f);

			// Create <Dynamic> / <Transformation> elements
			XmlElement dynamicElement = XML.Utils.GetOrCreateElement("Dynamic", propertiesElement, xml);
			XmlElement transformationElement = xml.CreateElement("Transformation");
			transformationElement.SetAttribute("Name", dynamicSize.TransformationName);

			// Easing pattern selection
			int[] stepWeights = dynamicSize.Size.Easing switch {
				DynamicSize.EasingTypes.Linear  => new[] { 1 },
				DynamicSize.EasingTypes.EaseIn  => new[] { 1, 2, 3, 4, 5 },
				DynamicSize.EasingTypes.EaseOut  => new[] { 5, 4, 3, 2, 1 },
				DynamicSize.EasingTypes.EaseInOut => new[] { 1, 2, 3, 2, 1 },
				DynamicSize.EasingTypes.EaseOutIn => new[] { 3, 2, 1, 2, 3 },
				_ => new[] { 1 }
			};

			int totalWeight = stepWeights.Sum();
			int stepCount = stepWeights.Length;

			// Size deltas per weighted unit
			float widthPerUnit = totalWidth / totalWeight;
			float heightPerUnit = totalHeight / totalWeight;

			// Frame distribution proportional to weight
			int[] framesPerStep = new int[stepCount];
			int framesAllocated = 0;

			for (int i = 0; i < stepCount; i++) {
				float exactFrames = totalFrames * (float)stepWeights[i] / totalWeight;
				framesPerStep[i] = Mathf.FloorToInt(exactFrames);
				framesAllocated += framesPerStep[i];
			}

			// Distribute any leftover frames to the largest-weight steps
			int remainder = totalFrames - framesAllocated;
			while (remainder-- > 0) {
				int maxIndex = Array.IndexOf(stepWeights, stepWeights.Max());
				framesPerStep[maxIndex]++;
			}

			// Build <Size> elements
			for (int i = 0; i < stepCount; i++) {
				if (framesPerStep[i] <= 0)
					continue;

				XmlElement sizeElement = xml.CreateElement("Size");
				sizeElement.SetAttribute("Frames", framesPerStep[i].ToString());

				float stepWidth = nativeWidth * dynamicSize.Size.FinalWidth * stepWeights[i] / totalWeight;
				float stepHeight = nativeHeight * dynamicSize.Size.FinalHeight * stepWeights[i] / totalWeight;

				sizeElement.SetAttribute("Frames", framesPerStep[i].ToString());

				if (stepWidth > 0f) {
					sizeElement.SetAttribute("FinalWidth", Helpers.ToString(stepWidth));
				}

				if (stepHeight > 0f) {
					sizeElement.SetAttribute("FinalHeight", Helpers.ToString(stepHeight));
				}

				transformationElement.AppendChild(sizeElement);
			}

			// Assemble XML hierarchy
			dynamicElement.AppendChild(transformationElement);
			propertiesElement.AppendChild(dynamicElement);
		}
	}
}