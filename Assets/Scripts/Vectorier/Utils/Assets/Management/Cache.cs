using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

using Vectorier;

// -=-=-=- //

public static class AssetCache {
    private static readonly object _lock = new object();
    private static Dictionary<string, string> _hashes = new Dictionary<string, string>();

    private static string _cacheFile;
    private static bool _initialized;
    private static int _dirty;

    // Cached ONLY ON MAIN THREAD
    private static string _assetsRoot;
    private static string _projectRoot;

    // ----------------------------
    // INIT (MUST BE MAIN THREAD)
    // ----------------------------

    public static void Init(string cacheFilePath) {
        if (_initialized) return;

        _cacheFile = cacheFilePath;

        // Safe Unity calls ONLY here
        _assetsRoot = Application.dataPath.Replace('\\', '/');
        _projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
            .Replace('\\', '/');

        _initialized = true;

        Load();
    }

    // ----------------------------
    // PATH NORMALIZATION (THREAD SAFE)
    // ----------------------------

    private static string NormalizePath(string path) {
        if (string.IsNullOrEmpty(path))
            return path;

        path = Vectorier.Utils.Paths.From(path).Normalize();

        // already relative Unity path
        if (path.StartsWith("Assets" + Path.AltDirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase)) {
            return path;
        }

        // absolute Unity Assets path
        if (!string.IsNullOrEmpty(_assetsRoot) &&
            path.StartsWith(_assetsRoot, StringComparison.OrdinalIgnoreCase)) {

            path = path.Substring(_assetsRoot.Length).TrimStart(Path.AltDirectorySeparatorChar);
            return "Assets" + Path.AltDirectorySeparatorChar.ToString() + path;
        }

        // fallback: strip project root if needed
        if (!string.IsNullOrEmpty(_projectRoot) &&
            path.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase)) {

            return path.Substring(_projectRoot.Length).TrimStart(Path.AltDirectorySeparatorChar);
        }

        return path;
    }

    // ----------------------------
    // HASH ACCESS
    // ----------------------------

    public static bool TryGetHash(string path, out string hash) {
        lock (_lock) {
            return _hashes.TryGetValue(NormalizePath(path), out hash);
        }
    }

	public static void SetHash(string path, string hash) {
		lock (_lock) {
			path = NormalizePath(path);

			if (_hashes.TryGetValue(path, out var old) && old == hash)
				return;

			_hashes[path] = hash;
			_dirty++;
		}
	}

	public static bool ShouldCopy(string path, string hash) {
		path = NormalizePath(path);

		lock (_lock) {
			return !_hashes.TryGetValue(path, out var old) || old != hash;
		}
	}

    public static bool HasChanged(string path) {
        path = NormalizePath(path);

        var hash = ComputeMD5(path);

        lock (_lock) {
            if (_hashes.TryGetValue(path, out var old) && old == hash) {
                return false;
            }

            _hashes[path] = hash;
            _dirty++;

            return true;
        }
    }

    // ----------------------------
    // BULK OPS
    // ----------------------------

    public static void SetMany(IEnumerable<KeyValuePair<string, string>> entries) {
        lock (_lock) {
            foreach (var kv in entries) {
                _hashes[NormalizePath(kv.Key)] = kv.Value;
            }
        }
    }

    private static Dictionary<string, string> Snapshot() {
        lock (_lock) {
            return new Dictionary<string, string>(_hashes);
        }
    }

    // ----------------------------
    // LOAD / SAVE
    // ----------------------------

    public static void Load() {
        if (!File.Exists(_cacheFile)) {
            _hashes = new Dictionary<string, string>();
            Save();
            return;
        }

        try {
            var serializer = new XmlSerializer(typeof(CacheData));
            using var stream = File.OpenRead(_cacheFile);

            var data = (CacheData)serializer.Deserialize(stream);

			_hashes = data?.Entries?
				.Where(e =>
					!string.IsNullOrWhiteSpace(e.P) &&
					!string.IsNullOrWhiteSpace(e.H)
				)
				.ToDictionary(
					e => NormalizePath(e.P),
					e => e.H
				)
				?? new Dictionary<string, string>();
        }
        catch {
            _hashes = new Dictionary<string, string>();
            Save();
        }
    }

	public static void Save() {
		if (_dirty == 0) {
			return;
		}

		var snapshot = Snapshot();

		var data = new CacheData {
			Entries = snapshot.Select(kv => new E {
				H = kv.Value,
				P = kv.Key,
			}).ToList()
		};

		var serializer = new XmlSerializer(typeof(CacheData));

		var ns = new XmlSerializerNamespaces();
		ns.Add("", "");

		var settings = new XmlWriterSettings {
			OmitXmlDeclaration = true,
			Indent = true
		};

		var sb = new StringWriter();
		using (var writer = XmlWriter.Create(sb, settings)) {
			serializer.Serialize(writer, data, ns);
		}

		File.WriteAllText(_cacheFile, sb.ToString());

		_dirty = 0;
	}

    // ----------------------------
    // MD5
    // ----------------------------

    public static string ComputeMD5(string file) {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(file);

        var hash = md5.ComputeHash(stream);

        return BitConverter.ToString(hash)
            .Replace("-", "")
            .ToLowerInvariant();
    }

    // ----------------------------
    // EQUIVALENCE
    // ----------------------------

    public static bool Equivalent(string a, string b) {
        a = NormalizePath(a);
        b = NormalizePath(b);

        if (!File.Exists(a) || !File.Exists(b)) {
            return false;
        }

        var fa = new FileInfo(a);
        var fb = new FileInfo(b);

        if (fa.Length != fb.Length) {
            return false;
        }

        if (fa.LastWriteTimeUtc == fb.LastWriteTimeUtc) {
            return true;
        }

        if (!TryGetHash(a, out var ha)) {
            ha = ComputeMD5(a);
            SetHash(a, ha);
        }

        if (!TryGetHash(b, out var hb)) {
            hb = ComputeMD5(b);
            SetHash(b, hb);
        }

        return ha == hb;
    }

    // ----------------------------
    // FLUSH
    // ----------------------------

    public static void Flush() {
        if (_dirty == 0) return;

        Save();
        _dirty = 0;
    }

    // ----------------------------
    // DATA MODEL
    // ----------------------------

    [Serializable]
    public class CacheData {
        public List<E> Entries = new List<E>();
    }

    [Serializable]
    public class E {
        [XmlAttribute] public string H; // Hash
        [XmlAttribute] public string P; // Path
    }
}