using Google.Maps.Coord;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using Adrenak.UniMap;
using UnityEditor;
using System.Threading;
using System.Threading.Tasks;

namespace Google.StreetView.APIParsing.PanoReturn {
    public class Location {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Root {
        public string copyright { get; set; }
        public string date { get; set; }
        public Location location { get; set; }
        public string pano_id { get; set; }
        public string status { get; set; }
    }
}

namespace Google.Maps.APIParsing.GeocodeReturn {
    public class AddressComponent {
        public string long_name { get; set; }
        public string short_name { get; set; }
        public List<string> types { get; set; }
    }

    public class Northeast {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Southwest {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Bounds {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Location {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    public class Viewport {
        public Northeast northeast { get; set; }
        public Southwest southwest { get; set; }
    }

    public class Geometry {
        public Bounds bounds { get; set; }
        public Location location { get; set; }
        public string location_type { get; set; }
        public Viewport viewport { get; set; }
    }

    public class Result {
        public List<AddressComponent> address_components { get; set; }
        public string formatted_address { get; set; }
        public Geometry geometry { get; set; }
        public string place_id { get; set; }
        public List<string> types { get; set; }
    }

    public class Root {
        public List<Result> results { get; set; }
        public string status { get; set; }
    }
}

namespace Google.Maps.Examples {


    public class MissileBehaviour : MonoBehaviour {

    public GameObject ExplosionPrefab;


    public float Speed = 1.5f;

    public float ExplosionRadius = 10f;


    public float ExplosionExpiry = 5f;


    public LayerMask DamageLayers;

    private void FixedUpdate() {
      CharacterController controller = GetComponent<CharacterController>();
      controller.Move(transform.forward * Speed);
    }

    async private void OnControllerColliderHit(ControllerColliderHit hit) {
        GameObject explosion = Instantiate(ExplosionPrefab);
        explosion.transform.position = gameObject.transform.position;
        ActionTimer despawnTimer = explosion.AddComponent<ActionTimer>();
        despawnTimer.Action = delegate {
            Destroy(explosion);
        };
        despawnTimer.Expiry = ExplosionExpiry; 
        UniMapInitializer.Initialize();

        
        Destroy(gameObject);

        await HandlePanos(Physics.OverlapSphere(transform.position, ExplosionRadius, DamageLayers));
    }

    async private Task HandlePanos(Collider[] colliders) {
        foreach (Collider collider in colliders) {
            GameObject explodee = collider.gameObject;
            FenceChecker fenceChecker = explodee.GetComponent<FenceChecker>();

            if ((fenceChecker != null) && (!fenceChecker.Fenced())) {
                BuildingExploder exploder = explodee.GetComponent<BuildingExploder>();
                if (exploder != null) {
                        if (gameObject != null)
                            exploder.Explode(gameObject.transform.position);
                    }
                }
                int instanceID = explodee.GetInstanceID();
                Vector3 explodePosition = explodee.transform.position;
                if (FencingExample.placeIds.ContainsKey(instanceID)) {
                    string placeID = FencingExample.placeIds[instanceID];
                    await spawnSphereFromPanoID(placeID, explodePosition);
                }
        }
    }

    async private Task spawnSphereFromGeocode(Google.Maps.APIParsing.GeocodeReturn.Result geocodePlace, Vector3 position) {
            using (WebClient wc = new WebClient()) {
                wc.DownloadStringCompleted += async (sender, e) => {
                    Google.StreetView.APIParsing.PanoReturn.Root panoResponse = JsonConvert.DeserializeObject<Google.StreetView.APIParsing.PanoReturn.Root>(e.Result);

                    var dir = Path.Combine(Application.dataPath.Replace("Assets", ""), "SavedPanos");
                    var panoPath = Path.Combine(dir, panoResponse.pano_id + ".png");

                    if (FencingExample.panoIds.ContainsKey(panoResponse.pano_id)) {
                        Debug.LogWarning("ALREADY LOADED: " + panoResponse.pano_id);
                    } else {
                        try {
                            Texture2D unityTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                            if (File.Exists(panoPath)) {
                                Debug.LogWarning("FILE CACHE HIT: " + panoResponse.pano_id);
                                unityTexture.LoadImage(File.ReadAllBytes(panoPath));
                                unityTexture.Apply();
                            } else {
                                var panoDownloader = new PanoDownloader();
                                Debug.LogWarning("DOWNLOADING: " + panoResponse.pano_id);
                                var texture = await panoDownloader.Download(panoResponse.pano_id, PanoSize.VeryLarge, TextureFormat.RGB24);

                                Directory.CreateDirectory(dir);
                                File.WriteAllBytes(panoPath, texture.GetTexture2D(TextureFormat.ARGB32).EncodeToPNG());
                                unityTexture.LoadImage(texture.GetTexture2D(TextureFormat.ARGB32).EncodeToPNG());
                                unityTexture.Apply();
                            }

                            Material material = new Material(Shader.Find("Unlit/Transparent"));
                            material.mainTexture = FlipTexture(unityTexture);
                            //material.mainTexture = unityTexture;

                            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.transform.localScale = new Vector3(30, 30, 30);
                            sphere.transform.Rotate(0.0f, 180.0f, 0.0f, Space.Self);
                            Renderer rend = sphere.GetComponent<Renderer>();
                            rend.material = material;
                            sphere.GetComponent<Collider>().enabled = false;
                            Vector3 realPos = FencingExample.MapsService.Projection.FromLatLngToVector3(new LatLng(panoResponse.location.lat, panoResponse.location.lng));
                            sphere.transform.position = new Vector3(realPos.x, position.y + 5, realPos.z);

                            MeshFilter viewedModelFilter = (MeshFilter)sphere.GetComponent("MeshFilter");
                            Mesh mesh = viewedModelFilter.mesh;

                            Vector3[] normals = mesh.normals;
                            for (int i = 0; i < normals.Length; i++)
                                normals[i] = -normals[i];
                            mesh.normals = normals;

                            for (int m = 0; m < mesh.subMeshCount; m++) {
                                int[] triangles = mesh.GetTriangles(m);
                                for (int i = 0; i < triangles.Length; i += 3) {
                                    int temp = triangles[i + 0];
                                    triangles[i + 0] = triangles[i + 1];
                                    triangles[i + 1] = temp;
                                }
                                mesh.SetTriangles(triangles, m);
                            }

                            FencingExample.panoIds.Add(panoResponse.pano_id, sphere); 
                        }
                        catch (System.Exception exept) {
                            Debug.LogWarning("Error:  " + exept);
                        }
                    }
                };
                await wc.DownloadStringTaskAsync(new Uri("https://maps.googleapis.com/maps/api/streetview/metadata?location=" + geocodePlace.formatted_address + "&key=AIzaSyBTX9fitwZd46TiydnrS2w1Avjvvwf6OIE"));
                
            } 
        }

        async private Task spawnSphereFromPanoID(string placeID, Vector3 position) {
            using (WebClient wc = new WebClient()) {
                wc.DownloadStringCompleted += async (sender, e) => {

                    Google.Maps.APIParsing.GeocodeReturn.Root response = JsonConvert.DeserializeObject<Google.Maps.APIParsing.GeocodeReturn.Root>(e.Result);
                    //Debug.LogWarning("response: " + e.Result.ToString());
                    if (response != null) {
                        foreach (Google.Maps.APIParsing.GeocodeReturn.Result result in response.results) {

                            await spawnSphereFromGeocode(result,position); 
                        }
                    }
                };
                await wc.DownloadStringTaskAsync(new Uri("https://maps.googleapis.com/maps/api/geocode/json?place_id=" + placeID + "&key=AIzaSyBTX9fitwZd46TiydnrS2w1Avjvvwf6OIE"));
            }
        }

        Texture2D FlipTexture(Texture2D original) {
            Texture2D flipped = new Texture2D(original.width, original.height);

            int xN = original.width;
            int yN = original.height;


            for (int i = 0; i < xN; i++) {
                for (int j = 0; j < yN; j++) {
                    var color = original.GetPixel(i, j);
                    color.a = 0.75f;
                    flipped.SetPixel(xN - i - 1, j, color);
                }
            }
            flipped.Apply();

            return flipped;
        }
    }  
}
