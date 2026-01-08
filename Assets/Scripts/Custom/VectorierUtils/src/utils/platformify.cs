using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Collections.Generic;

using Vectorier;
using vu = VectorierUtils;

// -=-=-=- //
// Idea by TheLastCube

public class CollisionToPlatform : MonoBehaviour {
	public static class PlatformifyConfigLoader {
		public class CollisionMap {
			public float Offset;
			public float Padding;
			public float Opacity;
			public int Order;
			public bool RandomizeScale;
			public string Prefix;

			public string[] LeftCorner;
			public string[] RightCorner;
			public string[] LeftWall;
			public string[] RightWall;
			public string[] Floor;
		}

		public class TextureMap {
			public string Name;
			public float Width;
			public float Height;
			public float X;
			public float Y;
			public string Prefix;
			public float Opacity;
			public string Function; // ToLower, ToUpper, None
			public string Method; // Contains, Equals, StartsWith
		}

		public class ConfigData {
			public CollisionMap Collision = new CollisionMap();
			public List<TextureMap> Textures = new List<TextureMap>();
		}

		public static ConfigData Load(string path) {
			XDocument doc = XDocument.Load(path);
			var config = new ConfigData();

			// -=-=-=- //
			// parse <Collision>
			var collision = doc.Descendants("Collision").FirstOrDefault();

			if (collision != null) {
				config.Collision = new CollisionMap {
					Offset = (float.TryParse((string)collision.Attribute("Offset"), NumberStyles.Float, CultureInfo.InvariantCulture, out var off) ? off : 0f) * Vectorier.Core.Game.UnitValue,
					Padding = float.TryParse((string)collision.Attribute("Padding"), NumberStyles.Float, CultureInfo.InvariantCulture, out var pad) ? pad : 0f,
					RandomizeScale = bool.TryParse((string)collision.Attribute("RandomizeScale"), out var rand) && rand,
					Opacity = float.TryParse((string)collision.Attribute("Opacity"), NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha) ? alpha : 0f,
					Order = int.TryParse((string)collision.Attribute("Order"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ord) ? ord : 0,
					Prefix = (string)collision.Attribute("Prefix"),

					LeftCorner = ((string)collision.Element("LeftCorner")?.Attribute("Textures") ?? "")
						.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
					RightCorner = ((string)collision.Element("RightCorner")?.Attribute("Textures") ?? "")
						.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
					LeftWall = ((string)collision.Element("LeftWall")?.Attribute("Textures") ?? "")
						.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
					RightWall = ((string)collision.Element("RightWall")?.Attribute("Textures") ?? "")
						.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
					Floor = ((string)collision.Element("Floor")?.Attribute("Textures") ?? "")
						.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
				};
			}

			// -=-=-=- //

			// parse <Textures>
			var textureGroups = doc.Descendants("Image")
				.GroupBy(obj => new {
					X = (string)obj.Attribute("X"),
					Y = (string)obj.Attribute("Y"),
					Width = (string)obj.Attribute("Width"),
					Height = (string)obj.Attribute("Height"),
					Prefix = (string)obj.Attribute("Prefix") ?? "Object",
					Opacity = (string)obj.Attribute("Opacity"),
					Function = (string)obj.Attribute("Function") ?? "None",
					Method = (string)obj.Attribute("Method") ?? "Equals"
				});

			foreach (var group in textureGroups) {
				config.Textures.Add(new TextureMap {
					Name = string.Join("|", group.Select(g => (string)g.Attribute("Name"))),
					X = float.TryParse(group.First().Attribute("X")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : 0f,
					Y = float.TryParse(group.First().Attribute("Y")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ? y : 0f,
					Width = float.TryParse(group.First().Attribute("Width")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) ? w : 0f,
					Height = float.TryParse(group.First().Attribute("Height")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var h) ? h : 0f,
					Prefix = group.First().Attribute("Prefix")?.Value ?? "Object",
					Opacity = float.TryParse(group.First().Attribute("Opacity")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var o) ? o : 1f,
					Function = group.First().Attribute("Function")?.Value ?? "None",
					Method = group.First().Attribute("Method")?.Value ?? "Equals"
				});
			}

			// -=-=-=- //

			return config;
		}
	}

	// -=-=-=- //

	[MenuItem("Vectorier/⚙ Utils/🧱 Make platform from collision #P")]
	private static void ConvertCollision() {
		var cFile = vu.Utils.GetScriptPath();

		// path to the XML
		var configPath = Path.Combine(
			Path.GetDirectoryName(cFile),
			Path.ChangeExtension(cFile, ".xml")
		);

		// normalize relative path
		configPath = vu.String.GetRelativePath(Application.dataPath, configPath);

		// load full config
		var config = PlatformifyConfigLoader.Load(configPath);

		List<GameObject> newPlatforms = new List<GameObject>();

		// loop over ALL selected objects
		foreach (GameObject selected in Selection.gameObjects) {
			if (selected == null) continue;

			// skip if parent name contains "platform"
			if (selected.transform.parent != null &&
				selected.transform.parent.name.ToLower().Contains("platform")) {
				continue;
			}

			// skip if prefab instance
			if (PrefabUtility.IsPartOfAnyPrefab(selected)) {
				continue;
			}

			SpriteRenderer targetCollidableTextureSR = selected.GetComponent<SpriteRenderer>();
			if (targetCollidableTextureSR == null) {
				continue;
			}

			string spriteName = targetCollidableTextureSR.sprite.name;

			// -=-=-=- //
			// loop through each mapping
			foreach (var map in config.Textures) {
				string checkName = spriteName;

				// apply string function
				if (map.Function == "ToLower") {
					checkName = checkName.ToLower();
				} else if (map.Function == "ToUpper") {
					checkName = checkName.ToUpper();
				}

				// check Method rule
				bool match = false;

				// support multiple names separated by '|'
				var nameVariants = map.Name.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var variant in nameVariants) {
					string target = variant.Trim();

					switch (map.Method) {
						case "Contains":
							if (checkName.Contains(target)) {
								match = true;
							}

							break;
						case "Equals":
							if (checkName == target) {
								match = true;
							}

							break;
						case "StartsWith":
							if (checkName.StartsWith(target)) {
								match = true;
							}

							break;
					}

					// stop if any variant matches
					if (match) {
						break;
					}
				}

				if (!match) {
					continue;
				}

				GameObject newPlatform = null;

				if (map.Prefix == config.Collision.Prefix) {
					// platformify
					newPlatform = ConvertToGroundPlatform(selected, config.Collision);
				} else {
					// generic bridge/platform type
					newPlatform = ConvertToBridgePlatform(selected, map);
				}

				if (newPlatform != null) {
					newPlatforms.Add(newPlatform);
				}

				// stop after the first match
				break;
			}
		}

		// -=-=-=- //

		if (newPlatforms.Count > 0) {
			Selection.objects = newPlatforms.ToArray();
		}
	}

	// -=-=-=- //
	// converters

	private static GameObject ConvertToGroundPlatform(GameObject go, PlatformifyConfigLoader.CollisionMap config) {
		if (go == null) {
			return null;
		}

		SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
		if (sr == null || sr.sprite == null) {
			return null;
		}

		// Record undo for sprite renderer (we may change sprite / color)
		Undo.RecordObject(sr, "Update Collision Sprite");

		// if selected sprite is not already 'collision' it will be swapped,
		// but theres requirement to preserve its world size so the transform doesn't
		// get corrupted (and so Undo works cleanly).
		bool needsSwap = sr.sprite.name != "collision";
		Vector3 originalLocalScale = go.transform.localScale;
		Vector3 worldSizeBefore = Vector3.zero;

		if (needsSwap) {
			// read world size from bounds before changing sprite
			worldSizeBefore = sr.bounds.size; // world-space size (x, y, z)
		}

		// if the selected object is v_black (or any other), don't rename it —
		// keep its GameObject name intact (this preserves "v_black (3)" etc).
		// we'll still replace the sprite if needed.

		if (needsSwap) {
			// Load collision sprite
			Sprite collSprite = Resources.Load<Sprite>("Textures/collision");

			if (collSprite == null) {
				Debug.LogWarning("Textures/collision not found in Resources. Skipping conversion.");
				return null;
			}

			sr.sprite = collSprite;
			go.name = sr.sprite.name;

			// calculate new scale so the collision sprite keeps the same world size

			// newLocalScale = worldSizeBefore / collSprite.bounds.size (component-wise)
			Vector3 newLocalScale = new Vector3(
				(worldSizeBefore.x > 0f && collSprite.bounds.size.x > 0f)
					? (worldSizeBefore.x / collSprite.bounds.size.x)
					: originalLocalScale.x,

				(worldSizeBefore.y > 0f && collSprite.bounds.size.y > 0f)
					? (worldSizeBefore.y / collSprite.bounds.size.y)
					: originalLocalScale.y,

				originalLocalScale.z
			);

			// Record transform change for undo, then apply
			Undo.RecordObject(go.transform, "Adjust Collision Scale");
			go.transform.localScale = newLocalScale;
		}

		// tag as platform
		go.tag = "Platform";

		// set visual and render properties
		Color col = sr.color;
		col.a = config.Opacity; // use configured opacity
		sr.color = col;

		// use the configured order for collision
		sr.sortingOrder = config.Order;

		// create parent container
		GameObject platformGO = new GameObject("???x???");
		platformGO.transform.position = go.transform.position;
		Undo.RegisterCreatedObjectUndo(platformGO, "Create Platform Parent");

		// re-parent the selected object (keeps world position by default as there was SetTransformParent overload used earlier)
		Undo.SetTransformParent(go.transform, platformGO.transform, "Reparent Collision");

		// NOTE: do NOT rename 'go' here — preserve original selected name (e.g., 'v_black (3)')
		// determine collision bounds (using updated sprite + scale)
		Bounds bounds = sr.bounds;

		// --- LEFT CORNER ---
		Vector3 leftPos = new Vector3(bounds.min.x, bounds.max.y, go.transform.position.z);
		string leftCornerName = config.LeftCorner[UnityEngine.Random.Range(0, config.LeftCorner.Length)];
		GameObject leftCorner = CreateSprite(leftCornerName, leftPos, config.Order);
		if (leftCorner != null) {
			SetGameObjectOffset(leftCorner, -config.Offset, config.Offset);
			Undo.RegisterCreatedObjectUndo(leftCorner, "Create Left Corner");
			leftCorner.transform.SetParent(platformGO.transform, true);
		}

		// --- RIGHT CORNER ---
		Vector3 rightPos = new Vector3(bounds.max.x, bounds.max.y, go.transform.position.z);
		string rightCornerName = config.RightCorner[UnityEngine.Random.Range(0, config.RightCorner.Length)];
		GameObject rightCorner = CreateSprite(rightCornerName, rightPos, config.Order);
		if (rightCorner != null) {
			SetGameObjectOffset(rightCorner, config.Offset, config.Offset);
			MoveGameObject(rightCorner, -100f);
			Undo.RegisterCreatedObjectUndo(rightCorner, "Create Right Corner");
			rightCorner.transform.SetParent(platformGO.transform, true);
		}

		// --- EXTENSIONS ---
		FillFloor(leftCorner, rightCorner, platformGO, config);
		ExtendWall(leftCorner, platformGO, sr, config.LeftWall, false, config);
		ExtendWall(rightCorner, platformGO, sr, config.RightWall, true, config);
		ExtendGround(platformGO, sr, config);
		ExtendCollision(platformGO, sr, config);

		// name the parent based on collision or black fill inside it
		platformGO.name = SetGameObjectName(platformGO, config.Prefix);

		return platformGO;
	}

	private static GameObject ConvertToBridgePlatform(GameObject go, PlatformifyConfigLoader.TextureMap config) {
		if (go == null) {
			return null;
		}

		SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

		if (sr == null) {
			return null;
		}

		var collisionOffsetX = config.X * Vectorier.Core.Game.UnitValue;
		var collisionOffsetY = config.Y * Vectorier.Core.Game.UnitValue;

		// make source sprite visible (full alpha) & adjust sorting
		Undo.RecordObject(sr, "Update Source Sprite");
		Color c = sr.color;
		c.a = 1f;
		sr.color = c;
		sr.sortingOrder = 256;

		// create parent
		GameObject platformGO = new GameObject("???x???");
		Undo.RegisterCreatedObjectUndo(platformGO, "Create Bridge Platform");
		platformGO.transform.position = go.transform.position;

		// re-parent source into platformGO
		Undo.SetTransformParent(go.transform, platformGO.transform, "Reparent Source");

		// rename safely
		Undo.RecordObject(go, "Rename Source");
		go.name = sr.sprite.name;

		// store local scales of source sprite
		float nX = go.transform.localScale.x;
		float nY = go.transform.localScale.y;

		// create 'collision' child
		GameObject collisionChild = new GameObject("collision");
		Undo.RegisterCreatedObjectUndo(collisionChild, "Create Collision Child");

		collisionChild.transform.SetParent(platformGO.transform, false);
		collisionChild.transform.localPosition = Vector3.zero;
		collisionChild.transform.localRotation = Quaternion.identity;

		// assign collision sprite
		Sprite collSprite = Resources.Load<Sprite>("Textures/collision");
		SpriteRenderer collisionSR = null;

		if (collSprite != null) {
			collisionSR = collisionChild.AddComponent<SpriteRenderer>();
			collisionSR.sprite = collSprite;
			collisionSR.color = new Color(1f, 1f, 1f, config.Opacity);
			collisionSR.sortingOrder = (config.Opacity >= c.a) ? 257 : 255;
		} else {
			Debug.LogWarning("Textures/collision not found in Resources — child will be empty.");
		}

		// scale collision by formula
		float scaleX = Vectorier.Core.Game.UnitValue * config.Width * nX;
		float scaleY = Vectorier.Core.Game.UnitValue * config.Height * nY;
		Undo.RecordObject(collisionChild.transform, "Scale Collision Child");
		collisionChild.transform.localScale = new Vector3(scaleX, scaleY, 1f);

		// move parent down by offsetY * nY
		Undo.RecordObject(platformGO.transform, "Offset Platform Parent");
		platformGO.transform.position += new Vector3(collisionOffsetX * nX, collisionOffsetY * -nY, 0f);

		// shift all children inside the parent UP except collision
		foreach (Transform child in platformGO.transform) {
			if (child == collisionChild) {
				continue;
			}

			Undo.RecordObject(child.transform, "Offset Platform Child");
			child.localPosition += new Vector3(collisionOffsetX * -nX, collisionOffsetY * nY, 0f);
		}

		// force collision child back to (0,0)
		if (collisionChild != null) {
			Undo.RecordObject(collisionChild.transform, "Reset Collision Position");
			collisionChild.transform.localPosition = Vector3.zero;
		}

		// name platform
		platformGO.name = SetGameObjectName(platformGO, config.Prefix);

		return platformGO;
	}

	// -=-=-=- //
	// ground platform helpers

	private static void FillFloor(
		GameObject leftCorner,
		GameObject rightCorner,

		GameObject platformGO,

		PlatformifyConfigLoader.CollisionMap config
	) {
		if (leftCorner == null || rightCorner == null) {
			return;
		}

		SpriteRenderer leftSR = leftCorner.GetComponent<SpriteRenderer>();
		SpriteRenderer rightSR = rightCorner.GetComponent<SpriteRenderer>();

		if (leftSR == null || rightSR == null) {
			return;
		}

		// top Y of left corner
		float yPos = leftSR.bounds.max.y;

		// left edge of right corner
		float endX = rightSR.bounds.min.x;

		GameObject previous = leftCorner;

		while (true) {
			string floorName = config.Floor[UnityEngine.Random.Range(0, config.Floor.Length)];

			GameObject floorTile = CreateSprite(
				floorName,
				new Vector3(previous.transform.position.x, yPos, previous.transform.position.z),
				config.Order
			);

			if (floorTile == null) {
				break;
			}

			floorTile.transform.SetParent(platformGO.transform, true);

			// randomize scale if enabled
			if (config.RandomizeScale) {
				float scaleX = UnityEngine.Random.Range(0.5f, 1.0f);
				// float scaleY = UnityEngine.Random.Range(0.5f, 1.0f);
				floorTile.transform.localScale = new Vector3(scaleX, scaleX, 1f);
			}

			// place next to previous
			AlignNextToPreviousHorizontal(previous, floorTile);

			// apply consistent offset
			SetGameObjectOffset(floorTile, -config.Offset, 0f);

			Undo.RegisterCreatedObjectUndo(floorTile, "Create Floor Tile");

			// stop if the new tile's right edge would exceed the right corner's left edge
			SpriteRenderer sr = floorTile.GetComponent<SpriteRenderer>();
			if (sr == null) {
				break;
			}

			float rightEdge = sr.bounds.max.x;

			if (rightEdge >= endX) {
				// clamp last tile to not exceed right corner
				Vector3 pos = floorTile.transform.position;
				pos.x -= (rightEdge - endX);
				floorTile.transform.position = pos;

				break;
			}

			previous = floorTile;
		}
	}

	private static void ExtendWall(
		GameObject corner,

		GameObject platformGO,
		SpriteRenderer collisionSR,

		string[] wallSprites,
		bool isRightWall,

		PlatformifyConfigLoader.CollisionMap config
	) {
		if (corner == null || collisionSR == null) {
			return;
		}

		SpriteRenderer cornerSR = corner.GetComponent<SpriteRenderer>();

		if (cornerSR == null) {
			return;
		}

		float startY = cornerSR.bounds.min.y - config.Offset;
		float bottomY = collisionSR.bounds.min.y;
		float availableHeight = startY - bottomY;

		if (availableHeight <= 0f) {
			return;
		}

		if (wallSprites.Length == 0) {
			return;
		}

		string sampleName = wallSprites[0];
		Sprite sampleSprite = Resources.Load<Sprite>("Textures/" + sampleName);

		if (sampleSprite == null) {
			return;
		}

		float spriteHeight = sampleSprite.bounds.size.y;
		float spriteWidth = sampleSprite.bounds.size.x;

		// determine best number of walls and uniform scale
		int bestN = 1;
		float bestScale = 1f;

		for (int n = 1; n < 100; n++) {
			float scale = availableHeight / (n * spriteHeight);

			if (scale <= 1.0f) {
				bestN = n;
				bestScale = scale;
				break;
			}
		}

		bestScale += 2 * Vectorier.Core.Game.UnitValue;

		// place walls
		float currentY = startY;

		for (int i = 0; i < bestN; i++) {
			string wallName = wallSprites[UnityEngine.Random.Range(0, wallSprites.Length)];

			GameObject wallTile = CreateSprite(
				wallName,
				Vector3.zero,
				config.Order
			);

			if (wallTile == null) {
				break;
			}

			wallTile.transform.SetParent(platformGO.transform, true);
			wallTile.transform.localScale = new Vector3(bestScale, bestScale, 1f);

			// calculate X placement based on left or right wall
			float xPos;

			if (isRightWall) {
				xPos = cornerSR.bounds.max.x - wallTile.GetComponent<SpriteRenderer>().bounds.max.x;
			} else {
				xPos = corner.transform.position.x;
			}

			// set final position
			wallTile.transform.position = new Vector3(xPos, currentY, corner.transform.position.z);

			// inside offset
			SetGameObjectOffset(wallTile, 0f, config.Offset);

			Undo.RegisterCreatedObjectUndo(wallTile, "Create Wall");

			currentY -= spriteHeight * bestScale;
		}
	}

	private static void ExtendGround(
		GameObject platformGO,

		SpriteRenderer collisionSR,

		PlatformifyConfigLoader.CollisionMap config
	) {
		if (platformGO == null || collisionSR == null) {
			return;
		}

		// find lowest Y among left corner/wall sprites
		float lowestY = float.MaxValue;

		foreach (Transform child in platformGO.transform) {
			SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

			if (sr == null) {
				continue;
			}

			if (child.name.StartsWith("v_CornerUp_L_") || child.name.StartsWith("v_LongCornerUp_L_") || child.name.Contains("Wall")) {
				if (sr.bounds.min.y < lowestY) {
					lowestY = sr.bounds.min.y;
				}
			}
		}

		if (lowestY == float.MaxValue) {
			return;
		}

		// identify left and right corners
		GameObject leftCorner = null;
		GameObject rightCorner = null;

		foreach (Transform child in platformGO.transform) {
			if (child.name.StartsWith("v_CornerUp_L_")) {
				leftCorner = child.gameObject;
			}

			if (child.name.StartsWith("v_CornerUp_R_")) {
				rightCorner = child.gameObject;
			}
		}

		if (leftCorner == null || rightCorner == null) {
			return;
		}

		SpriteRenderer leftSR = leftCorner.GetComponent<SpriteRenderer>();
		SpriteRenderer rightSR = rightCorner.GetComponent<SpriteRenderer>();

		if (leftSR == null || rightSR == null) {
			return;
		}

		// calculate adjusted start and end positions based on corner bounds
		Vector3 leftPos = new Vector3(
			leftSR.bounds.min.x + leftSR.bounds.size.x * (config.Padding / 100f),
			leftSR.bounds.max.y - leftSR.bounds.size.y * (config.Padding / 100f),
			leftSR.bounds.center.z
		);

		Vector3 rightPos = new Vector3(
			rightSR.bounds.min.x + rightSR.bounds.size.x * ((config.Padding - 100) / -100f), // 87.5% from left (or 12.5% from right)
			rightSR.bounds.max.y - rightSR.bounds.size.y * (config.Padding / 100f), // 12.5% from top
			rightSR.bounds.center.z
		);

		// create black at leftPos
		GameObject blackFill = CreateSprite("v_black", leftPos, config.Order - 1);

		if (blackFill == null) {
			return;
		}

		blackFill.transform.SetParent(platformGO.transform, true);
		Undo.RegisterCreatedObjectUndo(blackFill, "Create Black Ground Fill");

		SpriteRenderer blackSR = blackFill.GetComponent<SpriteRenderer>();

		if (blackSR == null || blackSR.sprite == null) {
			return;
		}

		// scale v_black to reach rightPos horizontally and lowestY vertically
		Vector3 blackScale = blackFill.transform.localScale;

		// horizontal scale
		float targetWidth = rightPos.x - leftPos.x;
		float blackWidth = blackSR.sprite.bounds.size.x;
		blackScale.x = targetWidth / blackWidth;

		// vertical scale
		float targetHeight = leftPos.y - lowestY;
		float blackHeight = blackSR.sprite.bounds.size.y;
		blackScale.y = targetHeight / blackHeight;

		blackFill.transform.localScale = blackScale;

		// position remains anchored at leftPos
		blackFill.transform.position = leftPos;
	}

	private static void ExtendCollision(
		GameObject platformGO,

		SpriteRenderer collisionSR,

		PlatformifyConfigLoader.CollisionMap config
		) {
		if (platformGO == null || collisionSR == null) {
			return;
		}

		float maxTopY = float.MinValue;

		// check for "_LongCornerUp_" in any child
		foreach (Transform child in platformGO.transform) {
			if (child.name.Contains("_LongCornerUp_") || child.name.Contains("_Wall_")) {
				return;
			}

			SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

			if (sr == null) {
				continue;
			}

			float childTopY = sr.bounds.max.y;
	
			// find the child with highest top Y
			if (childTopY > maxTopY) {
				maxTopY = childTopY;
			}
		}

		// extend collision scale to match top
		if (maxTopY > collisionSR.bounds.max.y) {
			collisionSR.transform.localScale = new Vector3(
				collisionSR.transform.localScale.x,
				1f - config.Offset,
				collisionSR.transform.localScale.z
			);
		}
	}

	// -=-=-=- //
	// helpers 1

	private static void AlignNextToPreviousHorizontal(GameObject previous, GameObject next) {
		if (previous == null || next == null) {
			return;
		}

		SpriteRenderer prevSR = previous.GetComponent<SpriteRenderer>();
		SpriteRenderer nextSR = next.GetComponent<SpriteRenderer>();

		if (prevSR == null || nextSR == null) {
			return;
		}

		// keep Y fixed, move X by previous sprite width
		Vector3 pos = next.transform.position;

		// right edge of previous
		pos.x = prevSR.bounds.max.x;
		next.transform.position = pos;
	}

	// -=-=-=- //
	// helpers 2

	private static GameObject CreateSprite(string spriteName, Vector3 worldPos, int sortingOrder = 0) {
		Sprite sprite = Resources.Load<Sprite>("Textures/" + spriteName);

		if (sprite == null) {
			Debug.LogError($"Sprite '{spriteName}' not found in Resources.");
			return null;
		}

		GameObject go = new GameObject(spriteName);
		go.transform.position = worldPos;
		go.transform.localScale = Vector3.one;

		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = sprite;
		sr.sortingOrder = sortingOrder;

		return go;
	}

	private static string SetGameObjectName(GameObject go, string prefix) {
		float xScale = 0;
		float yScale = 0;

		foreach (Transform child in go.transform) {
			SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

			if (sr == null) {
				continue;
			}

			if (sr.sprite.name == "collision") {
				// calculate scale-based name
				xScale = Mathf.Ceil(child.transform.localScale.x / Vectorier.Core.Game.UnitValue);
				yScale = Mathf.Ceil(child.transform.localScale.y / Vectorier.Core.Game.UnitValue);

				break;
			} else if (sr.sprite.name == "v_black") {
				// calculate scale-based name
				xScale = Mathf.Ceil(child.transform.localScale.x / Vectorier.Core.Game.UnitValue);
				yScale = Mathf.Ceil(child.transform.localScale.y / Vectorier.Core.Game.UnitValue);

				xScale /= 2.56f;
				yScale /= 2.56f;

				break;
			}
		}

		xScale = Mathf.RoundToInt(xScale);
		yScale = Mathf.RoundToInt(yScale);

		return $"{prefix}{xScale}x{yScale}";
	}

	// add raw offset
	private static void SetGameObjectOffset(GameObject go, float x, float y, float z = 0f) {
		if (go != null) {
			go.transform.position += new Vector3(x, y, z);
		}
	}

	// move relative GameObject to its own sprite width
	private static void MoveGameObject(GameObject go, float percentageX) {
		if (go == null) {
			return;
		}

		SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
		if (sr == null || sr.sprite == null) {
			return;
		}

		float spriteWidth = sr.bounds.size.x;
		float moveAmount = (percentageX / 100f) * spriteWidth;

		go.transform.position += new Vector3(moveAmount, 0f, 0f);
	}
}