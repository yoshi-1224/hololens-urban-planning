namespace Mapbox.Unity.MeshGeneration.Factories
{
	using UnityEngine;
	using Mapbox.Unity.MeshGeneration.Data;
	using Mapbox.Unity.Utilities;

	[CreateAssetMenu(menuName = "Mapbox/Factories/Flat Terrain Factory")]
	public class FlatTerrainFactory : AbstractTileFactory
	{
		[SerializeField]
		private Material _baseMaterial;

		[SerializeField]
		private bool _addCollider = false;

		[SerializeField]
		private bool _addToLayer = false;

		[SerializeField]
		private int _layerId = 0;

		Mesh _cachedQuad;

		internal override void OnInitialized()
		{
			
		}

		internal override void OnRegistered(UnityTile tile)
		{
			if (_addToLayer && tile.gameObject.layer != _layerId)
			{
				tile.gameObject.layer = _layerId;
			}

			if (tile.MeshRenderer == null)
			{
				var renderer = tile.gameObject.AddComponent<MeshRenderer>();
				renderer.material = _baseMaterial;
			}

			if (tile.MeshFilter == null)
			{
				tile.gameObject.AddComponent<MeshFilter>();
			}

			// HACK: This is here in to make the system trigger a finished state.
			Progress++;
			tile.MeshFilter.sharedMesh = GetQuad(tile);
			Progress--;

			if (_addCollider && tile.Collider == null)
			{
				tile.gameObject.AddComponent<BoxCollider>();
			}
		}

		internal override void OnUnregistered(UnityTile tile)
		{

		}

		private Mesh GetQuad(UnityTile tile)
		{
			if (_cachedQuad != null)
			{
				return _cachedQuad;
			}
			return BuildQuad(tile);
		}

		Mesh BuildQuad(UnityTile tile)
		{
			var unityMesh = new Mesh();
			var verts = new Vector3[4];

            float halfExtent = CustomMap.Instance.UnityTileLocalSize / 2f;
            verts[0] = new Vector2(-halfExtent, halfExtent).ToVector3xz();
            verts[1] = new Vector2(halfExtent, halfExtent).ToVector3xz();
            verts[2] = new Vector2(-halfExtent, -halfExtent).ToVector3xz();
            verts[3] = new Vector2(halfExtent, -halfExtent).ToVector3xz();
            
            unityMesh.vertices = verts;
			var trilist = new int[6] { 0, 1, 2, 1, 3, 2 };
			unityMesh.SetTriangles(trilist, 0);
			var uvlist = new Vector2[4]
			{
				new Vector2(0,1),
				new Vector2(1,1),
				new Vector2(0,0),
				new Vector2(1,0)
			};

			unityMesh.uv = uvlist;
			unityMesh.RecalculateNormals();

			tile.MeshFilter.sharedMesh = unityMesh;
			_cachedQuad = unityMesh;

			return unityMesh;
		}

        public override void OnZoomChanged() {
            // clear the cachedQuad such that when the zoom changes
            // we can create a brand new mesh to adjust for the size change
            _cachedQuad = null;
        }
    }
}