using UnityEngine;

using System.IO;

// -=-=-=- //

namespace Vectorier {
	public static class Files {
		public static readonly string Moves = Path.Combine(Vectorier.Settings.GameDirectory, "Moves_new" + "." + Vectorier.Core.Game.Extensions.File.XML);
	}
}