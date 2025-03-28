using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapGridGenerator : MonoBehaviour
{
    [Header("Settings")]
    public bool drawGizmos = true;

    [Header("Output")]
    public Vector2Int gridSize;
    public Vector2[,] positionGrid;

    private Tilemap _tilemap;
    private BoundsInt _bounds;

    void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        GeneratePositionGrid();
    }

    void GeneratePositionGrid()
    {
        _bounds = _tilemap.cellBounds;
        TileBase[] allTiles = _tilemap.GetTilesBlock(_bounds);

        gridSize = new Vector2Int(_bounds.size.x, _bounds.size.y);
        positionGrid = new Vector2[gridSize.x, gridSize.y];

        for (int x = 0; x < _bounds.size.x; x++)
        {
            for (int y = 0; y < _bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * _bounds.size.x];
                if (tile != null)
                {
                    Vector3Int cellPosition = new Vector3Int(
                        _bounds.xMin + x,
                        _bounds.yMin + y,
                        0
                    );
                    positionGrid[x, y] = _tilemap.GetCellCenterWorld(cellPosition);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
        if (!drawGizmos || positionGrid == null) return;

        Gizmos.color = Color.green;
        foreach (Vector2 pos in positionGrid)
        {
            if (pos != Vector2.zero)
            {
                Gizmos.DrawSphere(pos, 0.1f);

            }
        }
#endif
    }
}