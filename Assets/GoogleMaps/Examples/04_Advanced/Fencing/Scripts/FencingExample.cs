using System;
using System.Collections.Generic;
using Google.Maps.Feature;
using Google.Maps.Examples.Shared;
using UnityEngine;
using UnityEngine.Events;
using Google.Maps.Coord;
using System.Collections;


namespace Google.Maps.Examples {
    
    /// <summary>
    /// Example script demonstrating how to implement client-side geo-fencing based upon building
    /// usage types.
    /// </summary>
    ///
    [RequireComponent(typeof(DynamicMapsService), typeof(BuildingTexturer), typeof(ErrorHandling))]
    public class FencingExample : MonoBehaviour {
        public static Dictionary<int,string> placeIds = new System.Collections.Generic.Dictionary<int, string>();
        public static Dictionary<string, GameObject> panoIds = new System.Collections.Generic.Dictionary<string, GameObject>();
        /// <summary>
        /// Layer on which the player resides.
        /// </summary>
        private const int PLAYER_LAYER = 0;

    /// <summary>
    /// Layer on which the fencing objects are spawned.
    /// </summary>
    private const int FENCING_LAYER = 9;



        /// <summary>
        /// The <see cref="MapsService"/> with which to register our event handlers.
        /// </summary>
        public static MapsService MapsService;

    /// <summary>
    /// The object to use as the player's avatar.
    /// </summary>
    public GameObject Player;

    /// <summary>
    /// The object marking the location to spawn missiles from.
    /// </summary>

    /// <summary>
    /// A prefab for the missiles shot by the player.
    /// </summary>
    public GameObject MissilePrefab;

    /// <summary>
    /// A material to use to mark out the fenced-off areas.
    /// </summary>
    public Material FencedZoneMaterial;

    /// <summary>
    /// Margin to add around fenced zone.
    /// </summary>
    public float FenceMargin = 20f;

    /// <summary>
    /// A material to apply to chunks of destroyed buildings.
    /// </summary>
    public Material BuildingChunkMaterial;

    /// <summary>
    /// Set up Unity physics.
    /// </summary>
    private void PhysicsSetup() {
      Physics.gravity = new Vector3(0, -200, 0);
      Physics.autoSyncTransforms = true;

      // Allow player to enter fenced zones.
      Physics.IgnoreLayerCollision(FENCING_LAYER, PLAYER_LAYER);
    }

    /// <summary>
    /// Setup <see cref="MapsService"/>. Give buildings mesh colliders and make them explodeable.
    /// </summary>
    private void MapsServiceSetup() {
      MapsService = GetComponent<MapsService>();

      MapsService.Events.ExtrudedStructureEvents.DidCreate.AddListener(args => {
        // Assign every building a mesh collider, so player and bullets can collide with them.
        MeshFilter meshFilter = args.GameObject.GetComponent<MeshFilter>();
        MeshCollider collider = args.GameObject.AddComponent<MeshCollider>();

        collider.sharedMesh = meshFilter.sharedMesh;

        // Assign every building a building exploder.
        BuildingExploder exploder = args.GameObject.AddComponent<BuildingExploder>();
        exploder.ChunkMaterial = BuildingChunkMaterial;

        // Allow the fencing status of the building to be checked before damaging it.
        args.GameObject.AddComponent<FenceChecker>();
      });
    }


    /// <summary>
    /// Create a fence covering the specified bounds.
    /// </summary>
    /// <param name="bounds">Bounds around which to create the fence.</param>
    private GameObject CreateFence(Bounds bounds) {
      GameObject fencedZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
      fencedZone.transform.position = bounds.center;
      fencedZone.transform.localScale =
          new Vector3(bounds.size.x, bounds.size.y, bounds.size.z)
          + Vector3.one*FenceMargin*2f;
      fencedZone.GetComponent<Renderer>().sharedMaterial = FencedZoneMaterial;
      fencedZone.AddComponent<Fence>();
      fencedZone.layer = FENCING_LAYER;
      fencedZone.name = "Fence";

      return fencedZone;
    }

    /// <summary>
    /// Set up fence generation.
    /// </summary>
    private void FencingSetup() {
      MapsService.Events.ExtrudedStructureEvents.DidCreate.AddListener(args => {
          MapFeatureMetadata mapMeta = args.MapFeature.Metadata;
          string placeID = mapMeta.PlaceId;
          int gameID = args.GameObject.GetInstanceID();
          //Debug.Log("placeID: " + placeID + "gameID: " + gameID);
          FencingExample.placeIds.Add(gameID, placeID);
      });
    }

    /// <summary>
    /// Component awakening.
    /// </summary>
    private void Awake() {
      PhysicsSetup();
      MapsServiceSetup();
      FencingSetup();

            // Get required Building Texturer component on this GameObject.
      BuildingTexturer buildingTexturer = GetComponent<BuildingTexturer>();

      // Get the required Dynamic Maps Service on this GameObject.
      DynamicMapsService dynamicMapsService = GetComponent<DynamicMapsService>();

      // Sign up to event called after each new building is loaded, so can assign Materials to this
      // new building. Note that:
      // - DynamicMapsService.MapsService is auto-found on first access (so will not be null).
      // - This event must be set now during Awake, so that when Dynamic Maps Service starts loading
      //   the map during Start, this event will be triggered for all Extruded Structures.
      dynamicMapsService.MapsService.Events.ExtrudedStructureEvents.DidCreate.AddListener(
          args => buildingTexturer.AssignNineSlicedMaterials(args.GameObject));
    }

    /// <summary>
    /// Fire a missile from the player with the same orientation as the player.
    /// </summary>
    private void Fire(Vector3 targetPos) {
      GameObject missile = Instantiate(MissilePrefab);

      missile.transform.position = new Vector3(Player.transform.position.x, Player.transform.position.y + 1.5f, Player.transform.position.z);
      missile.transform.LookAt(targetPos);
      ActionTimer despawnTimer = missile.AddComponent<ActionTimer>();
      despawnTimer.Action = delegate {
        Destroy(missile);
      };
      despawnTimer.Expiry = 32;

      // Put the missile on a special layer so it can be made to collide with the fencing zones.
      missile.layer = MissilePrefab.layer;

      Collider missileCollider = missile.GetComponent<Collider>();
      Collider playerCollider = Player.GetComponent<Collider>();
      Physics.IgnoreCollision(missileCollider, playerCollider);
    }


  

    /// <summary>
    /// Enumerates clicks/touches from different input sources.
    /// </summary>
    private IEnumerable<Vector2> GetScreenClicks() {
      if (Input.GetMouseButtonDown(0)) {
        yield return Input.mousePosition;
      }
    }

    /// <summary>
    /// Update logic.
    /// </summary>
    private void Update() {
  
      foreach (Vector2 screenPosition in GetScreenClicks()) {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~(1 << FENCING_LAYER))) {
              Fire(new Vector3(hit.point.x, hit.point.y, hit.point.z) );
          
        }
      }
    }
  }
}
