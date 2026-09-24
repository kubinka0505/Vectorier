using UnityEngine;

using System.IO;

// -=-=-=- //

namespace Vectorier {
	public static class Files {
		public static readonly string Moves = Path.Combine(
			Vectorier.Settings.Core.GameDirectoryLegacy,
			"Moves_new" + "." + Vectorier.Core.Game.Properties.Extensions.File.XML
		);
	}
}