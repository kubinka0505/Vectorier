using System;
using UnityEngine;

namespace Vectorier.Core.Helpers {
	public static class BoundNormalize {
		public struct NormalizedTransform {
			public float X;
			public float Y;
			public float Z;
			public float Width;
			public float Height;
			public bool IsValid;
		}

		public static NormalizedTransform Normalize(
			Vector3 rawPos,
			Vector2 unscaledSpriteBounds,
			Vector3 lossyScale,
			Vector3 eulerAngles,
			bool flipX,
			bool flipY,
			bool isCenterPivot,
			int floatPrecision = 2
		) {
			// Guard: Reject free rotations that are not multiples of 90 degrees on Z
			float rotZ = NormalizeAngle(eulerAngles.z);
			if (Mathf.Abs(rotZ % 90f) > 0.01f && Mathf.Abs((rotZ % 90f) - 90f) > 0.01f) {
				return new NormalizedTransform { IsValid = false };
			}

			// 1. Account for SpriteRenderer Flips in Local Scale
			float effScaleX = lossyScale.x * (flipX ? -1f : 1f);
			float effScaleY = lossyScale.y * (flipY ? -1f : 1f);

			float localW = unscaledSpriteBounds.x * effScaleX;
			float localH = unscaledSpriteBounds.y * effScaleY;

			// 2. Define Local Corners Relative to Pivot
			// In Unity space, local top-left origin sits at (0, 0) extending to (localW, -localH)
			Vector3 localOriginShift = isCenterPivot 
				? new Vector3(-localW / 2f, localH / 2f, 0f) 
				: Vector3.zero;

			Vector3[] localCorners = new Vector3[4] {
				localOriginShift,
				localOriginShift + new Vector3(localW, 0, 0),
				localOriginShift + new Vector3(0, -localH, 0),
				localOriginShift + new Vector3(localW, -localH, 0)
			};

			// 3. Transform Local Corners using Full 3D Quaternion Rotation
			Quaternion rotation = Quaternion.Euler(eulerAngles);

			float minX = float.MaxValue, maxX = float.MinValue;
			float minY = float.MaxValue, maxY = float.MinValue;

			for (int i = 0; i < 4; i++) {
				Vector3 rotated = rotation * localCorners[i];

				if (rotated.x < minX) minX = rotated.x;
				if (rotated.x > maxX) maxX = rotated.x;
				if (rotated.y < minY) minY = rotated.y;
				if (rotated.y > maxY) maxY = rotated.y;
			}

			// Calculate World Dimensions
			float worldWidth = maxX - minX;
			float worldHeight = maxY - minY;

			// Position in World Space
			float x = rawPos.x + minX;
			float y = rawPos.y + maxY;

			// 4. Rounding to Target Precision
			if (floatPrecision >= 0) {
				x = (float)Math.Round(x, floatPrecision, MidpointRounding.AwayFromZero);
				y = (float)Math.Round(y, floatPrecision, MidpointRounding.AwayFromZero);
				worldWidth = (float)Math.Round(worldWidth, floatPrecision, MidpointRounding.AwayFromZero);
				worldHeight = (float)Math.Round(worldHeight, floatPrecision, MidpointRounding.AwayFromZero);
			}

			return new NormalizedTransform {
				X = x,
				Y = y,
				Z = rawPos.z,
				Width = worldWidth,
				Height = worldHeight,
				IsValid = true
			};
		}

		private static float NormalizeAngle(float angle) {
			angle %= 360f;
			if (angle < 0) angle += 360f;
			return angle;
		}
	}
}