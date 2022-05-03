using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StixGames.TileComposer
{
    [SelectionBase]
    [AddComponentMenu("Stix Games/Tile Composer/Example Models/Example Tile")]
    public class ExampleTile : MonoBehaviour
    {
        [Tooltip("The example tile will represent a tile of this type")]
        public string TileType;

        [Header("Internal")] public GameObject CurrentTile;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(TileType))
            {
                return;
            }
        
            var exampleModel = GetComponentInParent<ExampleModel>();
            if (exampleModel == null)
            {
                Debug.LogError("A example tile is not part of an example model", this);
                return;
            }

            var baseTiles = exampleModel.TileCollection.GetTiles(true).Where(x => x.BaseTile == null).ToArray();

            CloneTarget(baseTiles);
        }

        public void CloneTarget(IList<Tile> baseTiles)
        {
            var targetTile = baseTiles.SingleOrDefault(x => x.TileType == TileType);

            if (targetTile == null)
            {
                Debug.LogError("A selected tile type no longer exists", this);
                return;
            }

            if (CurrentTile != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(CurrentTile.gameObject);
                }
                else
                {
                    DestroyImmediate(CurrentTile.gameObject);
                }
            }

            // Clone the tile as a child of this object
            var t = transform;
            var tile = Instantiate(targetTile, t.position, t.rotation, t);
            CurrentTile = tile.gameObject;
            tile.gameObject.SetActive(true);
            
            // Now destroy the tile component, to make sure selection base is the example tile
            if (Application.isPlaying)
            {
                Destroy(tile);
            }
            else
            {
                DestroyImmediate(tile);
            }
        }

        private void OnDisable()
        {
            Destroy(CurrentTile.gameObject);
        }
    }
}