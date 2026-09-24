using System;
using UnityEngine;

// -=-=-=- //

namespace Vectorier.Core.Helpers {
	public static class BoundNormalizer {
		public enum ObjectType {
			Quad,     // Areas, Triggers (90deg snaps, strict bounding boxes)
			Sprite,   // Free rotations, affine 2D transform matrices
			Trapezoid // Same X/Y scale, only +- positions
		}

		public struct RectTransformData {
			public float X;
			public float Y;
			public float Z;

			public float Width;
			public float Height;
			
			// Affine Matrix values (for Images)
			public float A, B, C, D, Tx, Ty;

			public bool HasMatrix;
			public bool IsValid;
		}

		public struct TrapezoidTransformData {
			public enum Types {
				Type1 = 1,
				Type2 = 2
			}

			public float X;
			public float Y;

			public float Width;
			public float Height;
			public float Height1;

			public Types Type;

			public bool IsValid;
		}

		private static float Round(float val, int precision) {
			return (float)Math.Round(val, precision, MidpointRounding.AwayFromZero);
		}

		private static float SnapToZero(float value) {
			return Mathf.Abs(value) < 0.001f ? 0f : value;
		}

		private static Vector2 ToPixelSpace(Vector3 worldPos, float ppu) {
			return new Vector2(worldPos.x * ppu, -worldPos.y * ppu);
		}

		private static float NormalizeAngle(float angle) {
			angle %= 360f;

			if (angle < 0) {
				angle += 360f;
			}

			return angle;
		}

		// -=-=-=- //

		public static ObjectType InferObjectType(GameObject go) {
			if (go != null && (go.CompareTag("Area") || go.CompareTag("Trigger") || go.CompareTag("Platform"))) {
				return ObjectType.Quad;
			}

			return ObjectType.Sprite;
		}

		public static RectTransformData Normalize(
			GameObject go,

			Vector3 rawPos,
			Vector2 unscaledSpriteBounds,
			Vector3 lossyScale,
			Vector3 eulerAngles,

			bool flipX,
			bool flipY,
			bool isCenterPivot,

			ObjectType? overrideType = null,

			int floatPrecision = 2
		) {
			ObjectType type = overrideType ?? InferObjectType(go);

			if (type == ObjectType.Quad) {
				return NormalizeQuad(
					rawPos,
					unscaledSpriteBounds,
					lossyScale,
					eulerAngles,
					flipX,
					flipY,
					isCenterPivot,
					floatPrecision
				);
			}

			return NormalizeImage(
				rawPos,
				unscaledSpriteBounds,
				lossyScale,
				eulerAngles,
				flipX,
				flipY,
				isCenterPivot,
				floatPrecision
			);
		}

		// NORMALIZERS

		private static RectTransformData NormalizeQuad(
			Vector3 rawPos,
			Vector2 unscaledSpriteBounds,
			Vector3 lossyScale,
			Vector3 eulerAngles,

			bool flipX,
			bool flipY,

			bool isCenterPivot,

			int floatPrecision
		) {
			float rotZ = NormalizeAngle(eulerAngles.z);

			if (
				Mathf.Abs(rotZ % 90f) > 0.01f &&
				Mathf.Abs((rotZ % 90f) - 90f) > 0.01f
			) {
				return new RectTransformData {
					IsValid = false
				};
			}

			float effScaleX = lossyScale.x * (flipX ? -1f : 1f);
			float effScaleY = lossyScale.y * (flipY ? -1f : 1f);

			float localW = unscaledSpriteBounds.x * effScaleX;
			float localH = unscaledSpriteBounds.y * effScaleY;

			Vector3 localOriginShift = isCenterPivot 
				? new Vector3(-localW / 2f, localH / 2f, 0f) 
				: Vector3.zero;

			Vector3[] localCorners = new Vector3[4] {
				localOriginShift,
				localOriginShift + new Vector3(localW, 0, 0),
				localOriginShift + new Vector3(0, -localH, 0),
				localOriginShift + new Vector3(localW, -localH, 0)
			};

			Quaternion rotation = Quaternion.Euler(eulerAngles);

			float minX = float.MaxValue, maxX = float.MinValue;
			float minY = float.MaxValue, maxY = float.MinValue;

			for (int i = 0; i < 4; i++) {
				Vector3 rotated = rotation * localCorners[i];

				if (rotated.x < minX) {
					minX = rotated.x;
				}

				if (rotated.x > maxX) {
					maxX = rotated.x;
				}

				if (rotated.y < minY) {
					minY = rotated.y;
				}

				if (rotated.y > maxY) {
					maxY = rotated.y;
				}
			}

			float worldWidth = maxX - minX;
			float worldHeight = maxY - minY;

			float x = rawPos.x + minX;
			float y = rawPos.y + maxY;

			if (floatPrecision >= 0) {
				x = Round(x, floatPrecision);
				y = Round(y, floatPrecision);

				worldWidth = Round(worldWidth, floatPrecision);
				worldHeight = Round(worldHeight, floatPrecision);
			}

			return new RectTransformData {
				X = x,
				Y = y,
				Z = rawPos.z,

				Width = worldWidth,
				Height = worldHeight,

				HasMatrix = false,
				IsValid = true
			};
		}

		private static RectTransformData NormalizeImage(
			Vector3 rawPos,
			Vector2 unscaledSpriteBounds, // Already in Unity World Space (e.g., 0.93m x 0.98m)
			Vector3 lossyScale,
			Vector3 eulerAngles,
	
			bool flipX,
			bool flipY,

			bool isCenterPivot,
			int floatPrecision
		) {
			float PPU = Game.Properties.UnitScale; // e.g., 100
			float effScaleX = lossyScale.x * (flipX ? -1f : 1f);
			float effScaleY = lossyScale.y * (flipY ? -1f : 1f);

			// 1. Native dimensions in PIXELS (use bounds * PPU to get true sprite pixel size)
			float nativeW = unscaledSpriteBounds.x * PPU; // 0.93 * 100 = 93
			float nativeH = unscaledSpriteBounds.y * PPU; // 0.98 * 100 = 98

			// 2. Pivot shift in WORLD units (DO NOT multiply by PPU here)
			Vector3 localOriginShift = isCenterPivot 
				? new Vector3(-unscaledSpriteBounds.x / 2f, unscaledSpriteBounds.y / 2f, 0f) 
				: Vector3.zero;

			// Corner points relative to transform origin in WORLD units
			Vector3 localTopLeft = localOriginShift;
			Vector3 localTopRight = localOriginShift + new Vector3(unscaledSpriteBounds.x * effScaleX, 0f, 0f);
			Vector3 localBottomLeft = localOriginShift + new Vector3(0f, -unscaledSpriteBounds.y * effScaleY, 0f);
			Vector3 localBottomRight = localOriginShift + new Vector3(unscaledSpriteBounds.x * effScaleX, -unscaledSpriteBounds.y * effScaleY, 0f);

			Quaternion rot = Quaternion.Euler(eulerAngles);

			// 3. Transform directly into PIXEL space
			Vector2 ptTL = ToPixelSpace(rawPos + rot * localTopLeft, PPU);
			Vector2 ptTR = ToPixelSpace(rawPos + rot * localTopRight, PPU);
			Vector2 ptBL = ToPixelSpace(rawPos + rot * localBottomLeft, PPU);
			Vector2 ptBR = ToPixelSpace(rawPos + rot * localBottomRight, PPU);

			Vector2 vecW = ptTR - ptTL;
			Vector2 vecH = ptBL - ptTL;

			float A = vecW.x;
			float B = vecW.y;
			float C = vecH.x;
			float D = vecH.y;

			// 4. Check for Identity placement
			bool isIdentity = Mathf.Approximately(B, 0f) && 
							  Mathf.Approximately(C, 0f) && 
							  A > 0f && D > 0f;

			float imagePosX = rawPos.x * PPU;
			float imagePosY = -rawPos.y * PPU;

			float minX = Mathf.Min(ptTL.x, Mathf.Min(ptTR.x, Mathf.Min(ptBL.x, ptBR.x)));
			float minY = Mathf.Min(ptTL.y, Mathf.Min(ptTR.y, Mathf.Min(ptBL.y, ptBR.y)));
			float maxX = Mathf.Max(ptTL.x, Mathf.Max(ptTR.x, Mathf.Max(ptBL.x, ptBR.x)));
			float maxY = Mathf.Max(ptTL.y, Mathf.Max(ptTR.y, Mathf.Max(ptBL.y, ptBR.y)));

			float boundingW = maxX - minX;
			float boundingH = maxY - minY;

			float topLeftX = isIdentity ? minX : imagePosX + Mathf.Min(0f, A) + Mathf.Min(0f, C);
			float topLeftY = isIdentity ? minY : imagePosY + Mathf.Min(0f, B) + Mathf.Min(0f, D);

			float Tx = imagePosX - topLeftX;
			float Ty = imagePosY - topLeftY;

			if (floatPrecision >= 0) {
				topLeftX = Round(topLeftX, floatPrecision);
				topLeftY = Round(topLeftY, floatPrecision);
				boundingW = Round(boundingW, floatPrecision);
				boundingH = Round(boundingH, floatPrecision);
				A = SnapToZero(Round(A, floatPrecision));
				B = SnapToZero(Round(B, floatPrecision));
				C = SnapToZero(Round(C, floatPrecision));
				D = SnapToZero(Round(D, floatPrecision));
				Tx = SnapToZero(Round(Tx, floatPrecision));
				Ty = SnapToZero(Round(Ty, floatPrecision));
			}

			return new RectTransformData {
				X = topLeftX,
				Y = topLeftY,
				Z = rawPos.z,
				Width = isIdentity ? nativeW * effScaleX : boundingW,
				Height = isIdentity ? nativeH * effScaleY : boundingH,
				A = A, B = B, C = C, D = D, Tx = Tx, Ty = Ty,
				HasMatrix = !isIdentity,
				IsValid = true
			};
		}

		public static TrapezoidTransformData NormalizeTrapezoid(
			GameObject go,
			Vector3 rawPos,
			Vector2 unscaledSpriteBounds, // e.g., (10, 5)
			Vector3 scale,
			Vector3 eulerAngles,
			bool flipX,
			bool flipY,
			bool isType1 // true for trapezoid_type1, false for trapezoid_type2
		) {
			float unitScale = Game.Properties.UnitScale; // e.g., 100

			// calc base bounds in px space (eg. 1000x500)
			float baseW = unscaledSpriteBounds.x * unitScale;
			float baseH = unscaledSpriteBounds.y * unitScale;

			// determine pivot offset relative to sprite bottom-left
			// Type 1 default pivot: Bottom-Left (0, 0)
			// Type 2 default pivot: Top-Left (0, baseH)
			bool isCenter = IsCenterPivot(go);
			Vector2 pivotOffset = isCenter 
				? new Vector2(baseW / 2f, baseH / 2f) 
				: (isType1 ? Vector2.zero : new Vector2(0f, baseH));

			// local corners relative to pivot in px space
			// C0: Bottom-Left, C1: Top-Left, C2: Top-Right, C3: Bottom-Right
			Vector2[] localCorners = new Vector2[4] {
				new Vector2(0f - pivotOffset.x, 0f - pivotOffset.y),
				new Vector2(0f - pivotOffset.x, baseH - pivotOffset.y),
				new Vector2(baseW - pivotOffset.x, baseH - pivotOffset.y),
				new Vector2(baseW - pivotOffset.x, 0f - pivotOffset.y)
			};

			// effective scale & 3D rot transform
			float effScaleX = scale.x * (flipX ? -1f : 1f);
			float effScaleY = scale.y * (flipY ? -1f : 1f);
			Quaternion rot = Quaternion.Euler(eulerAngles);

			Vector2[] worldPixelCorners = new Vector2[4];
			for (int i = 0; i < 4; i++) {
				// Convert local pixel coordinates back to world meters for 3D rotation
				Vector3 localMeters = new Vector3(
					(localCorners[i].x * effScaleX) / unitScale,
					(localCorners[i].y * effScaleY) / unitScale,
					0f
				);

				Vector3 worldMeters = rawPos + rot * localMeters;
				worldPixelCorners[i] = ToPixelSpace(worldMeters, unitScale);
			}

			// bbox calc in game px space
			float minX = float.MaxValue, maxX = float.MinValue;
			float minY = float.MaxValue, maxY = float.MinValue;

			for (int i = 0; i < 4; i++) {
				if (worldPixelCorners[i].x < minX) minX = worldPixelCorners[i].x;
				if (worldPixelCorners[i].x > maxX) maxX = worldPixelCorners[i].x;
				if (worldPixelCorners[i].y < minY) minY = worldPixelCorners[i].y;
				if (worldPixelCorners[i].y > maxY) maxY = worldPixelCorners[i].y;
			}

			float width = maxX - minX;
			float height = maxY - minY;

			// eval effective slope orientation & type swapp
			// determine horizontal orientation of original left (C0, C1) vs right (C2, C3)
			float avgXOrigLeft = (worldPixelCorners[0].x + worldPixelCorners[1].x) / 2f;
			float avgXOrigRight = (worldPixelCorners[2].x + worldPixelCorners[3].x) / 2f;
			bool isFlippedHorizontally = avgXOrigLeft > avgXOrigRight;

			// determine vertical orientation of original Top (C1, C2) vs Bottom (C0, C3)
			float avgYOrigTop = (worldPixelCorners[1].y + worldPixelCorners[2].y) / 2f;
			float avgYOrigBottom = (worldPixelCorners[0].y + worldPixelCorners[3].y) / 2f;
			bool isFlippedVertically = avgYOrigTop > avgYOrigBottom;

			// horizontal or vertical inversions toggle effective slope direction
			bool effectiveIsType1 = isType1 ^ isFlippedHorizontally ^ isFlippedVertically;

			// h and enum out assign
			float h = effectiveIsType1 ? 1f : (height + 1f);
			float h1 = effectiveIsType1 ? (height + 1f) : 1f;

			TrapezoidTransformData.Types finalType = effectiveIsType1 
				? TrapezoidTransformData.Types.Type1 
				: TrapezoidTransformData.Types.Type2;

			return new TrapezoidTransformData {
				X = SnapToZero(Round(minX, 0)),
				Y = SnapToZero(Round(minY, 0)),

				Width = Round(width, 0),
				Height = Round(h, 0),
				Height1 = Round(h1, 0),

				Type = finalType,
				IsValid = true
			};
		}

		// -=-=-=- //

		public static RectTransformData ExtractNormalized(
			GameObject go,
			int precision = 2
		) {
			if (go == null) {
				return default;
			}

			Vector3 rawPos = go.transform.position;
			Vector3 lossyScale = go.transform.lossyScale;
			Vector3 eulerAngles = go.transform.eulerAngles;
			Vector2 unscaledSpriteBounds = Vector2.zero;

			bool flipX = false;
			bool flipY = false;

			var sr = go.GetComponent<SpriteRenderer>();
			if (sr != null) {
				if (sr.sprite != null) {
					unscaledSpriteBounds = sr.sprite.bounds.size;
				}

				flipX = sr.flipX;
				flipY = sr.flipY;
			}

			bool isCenter = IsCenterPivot(go);

			// Match method signature: (go, rawPos, unscaledSpriteBounds, lossyScale, eulerAngles, flipX, flipY, isCenter, overrideType, precision)
			var normalized = BoundNormalizer.Normalize(
				go,
				rawPos,
				unscaledSpriteBounds,
				lossyScale,
				eulerAngles,
				flipX,
				flipY,
				isCenter,
				null, // infer type via tag
				precision
			);

			if (!normalized.IsValid) {
				return normalized;
			}

			float scaleFactor = 1f / Game.Properties.UnitScale;

			return new BoundNormalizer.RectTransformData {
				X = normalized.X * scaleFactor,
				Y = normalized.Y * scaleFactor,
				Z = normalized.Z * scaleFactor,

				Width = normalized.Width * scaleFactor,
				Height = normalized.Height * scaleFactor,

				A = normalized.A,
				B = normalized.B,
				C = normalized.C,
				D = normalized.D,
				Tx = normalized.Tx,
				Ty = normalized.Ty,

				HasMatrix = normalized.HasMatrix,

				IsValid = true
			};
		}

		public static bool IsCenterPivot(GameObject go) {
			if (go == null) {
				return false;
			}

			var sr = go.GetComponent<SpriteRenderer>();
			if (sr == null || sr.sprite == null) {
				return false;
			}

			Vector2 pivot = sr.sprite.pivot;
			Rect rect = sr.sprite.rect;

			return Mathf.Approximately(pivot.x, rect.width / 2f) &&
				   Mathf.Approximately(pivot.y, rect.height / 2f);
		}
	}
}