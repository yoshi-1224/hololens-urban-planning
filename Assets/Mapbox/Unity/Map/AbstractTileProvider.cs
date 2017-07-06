namespace Mapbox.Unity.Map
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Mapbox.Map;

	public abstract class AbstractTileProvider : MonoBehaviour, ITileProvider
	{
		public event Action<UnwrappedTileId> OnTileAdded = delegate { };
		public event Action<UnwrappedTileId> OnTileRemoved = delegate { };
        public event Action OnZoomChanged;

        protected IMap _map;

		protected List<UnwrappedTileId> _activeTiles;

		public void Initialize(IMap map)
		{
			_activeTiles = new List<UnwrappedTileId>();
			_map = map;
			OnInitialized();
		}

		protected void AddTile(UnwrappedTileId tile)
		{
			if (_activeTiles.Contains(tile))
			{
				return;
			}

			_activeTiles.Add(tile);
			OnTileAdded(tile);
		}

		protected void RemoveTile(UnwrappedTileId tile)
		{
			if (!_activeTiles.Contains(tile))
			{
				return;
			}
			_activeTiles.Remove(tile);
			OnTileRemoved(tile);
		}

        /// <summary>
        /// this is different to just removing all tiles in that it also clears
        /// the tile cache in MapVisualizer when CustomMap class handles the 
        /// OnZoomSelected event.
        /// </summary>
        protected void RemoveAllTiles() {
            foreach(UnwrappedTileId tile in _activeTiles) {
                OnTileRemoved(tile);
            }
            _activeTiles.Clear();

            // this is called here since the child class
            // cannot reference it from their scope
            OnZoomChanged(); 
        }

		internal abstract void OnInitialized();
	}
}
