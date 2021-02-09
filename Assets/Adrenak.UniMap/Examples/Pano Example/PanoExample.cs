using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Adrenak.UniMap.Examples {
	public class PanoExample : MonoBehaviour {
		public PanoRenderer view;
		public List<string> ids;
		public float delay;
		public PanoSize size;
		public TextureFormat format = TextureFormat.RGBA32;

		IEnumerator Start() {
            UniMapInitializer.Initialize();

			while (true) {
				foreach (var id in ids) {
					//Debug.LogWarning("DOWNLOADING");
					view.downloader.DownloadAndForget("CAoSLEFGMVFpcE5JQTRuZHBEMjF6Sklpd3ItVVBrcFN0WWtIRDFJa0t5c0tyTGNf", size, format);
					yield return new WaitForSeconds(delay);
				}
			}
		}
	}
}