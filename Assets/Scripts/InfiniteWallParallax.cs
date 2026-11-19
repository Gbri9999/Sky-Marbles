using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(100)]
public class InfiniteWallParallax : MonoBehaviour
{
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField, Range(0f, 1f)] private float verticalParallax = 0.22f;

    private Camera sceneCamera;
    private Transform originalGrid;
    private Transform repeatingGrid;
    private Vector3 startingGridPosition;
    private float startingCameraY;
    private float repeatHeight;

    private void Start()
    {
        sceneCamera = GetComponent<Camera>();
        if (sceneCamera == null || wallTilemap == null || wallTilemap.layoutGrid == null)
        {
            Debug.LogWarning("InfiniteWallParallax needs a Camera and a wall Tilemap.", this);
            enabled = false;
            return;
        }

        originalGrid = wallTilemap.transform.parent;
        startingGridPosition = originalGrid.localPosition;
        startingCameraY = transform.position.y;

        Grid sourceGrid = wallTilemap.layoutGrid as Grid;
        BoundsInt bounds = wallTilemap.cellBounds;
        if (sourceGrid == null || !TryFindWallPattern(bounds, out int firstPatternRow))
        {
            Debug.LogWarning("The wall Tilemap needs two adjacent painted rows on both sides to repeat.", this);
            enabled = false;
            return;
        }

        // Keep the painted map and its starting floor. Only the extra wall layer loops.
        GameObject gridObject = new GameObject("Repeating Parallax Walls", typeof(Grid));
        repeatingGrid = gridObject.transform;
        repeatingGrid.SetParent(transform, false);
        repeatingGrid.localPosition = startingGridPosition;
        repeatingGrid.localRotation = originalGrid.localRotation;
        repeatingGrid.localScale = originalGrid.localScale;

        Grid grid = gridObject.GetComponent<Grid>();
        grid.cellSize = sourceGrid.cellSize;
        grid.cellGap = sourceGrid.cellGap;
        grid.cellLayout = sourceGrid.cellLayout;
        grid.cellSwizzle = sourceGrid.cellSwizzle;

        GameObject tilemapObject = new GameObject("Looped Wall Tiles", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(repeatingGrid, false);
        tilemapObject.transform.localPosition = wallTilemap.transform.localPosition;
        tilemapObject.transform.localRotation = wallTilemap.transform.localRotation;
        tilemapObject.transform.localScale = wallTilemap.transform.localScale;

        Tilemap repeated = tilemapObject.GetComponent<Tilemap>();
        repeated.tileAnchor = wallTilemap.tileAnchor;
        repeated.color = wallTilemap.color;
        TilemapRenderer originalRenderer = wallTilemap.GetComponent<TilemapRenderer>();
        TilemapRenderer repeatedRenderer = tilemapObject.GetComponent<TilemapRenderer>();
        repeatedRenderer.sharedMaterial = originalRenderer.sharedMaterial;
        repeatedRenderer.sortingLayerID = originalRenderer.sortingLayerID;
        repeatedRenderer.sortingOrder = originalRenderer.sortingOrder + 1;

        float cellHeight = Mathf.Abs(sourceGrid.cellSize.y * originalGrid.localScale.y);
        repeatHeight = cellHeight * 2f;
        int halfRows = Mathf.CeilToInt(sceneCamera.orthographicSize / cellHeight) + 4;

        // The last two complete rows have the painted checker pattern on both sides.
        for (int y = -halfRows; y <= halfRows; y++)
        {
            int sourceY = firstPatternRow + PositiveMod(y - firstPatternRow, 2);
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                TileBase tile = wallTilemap.GetTile(new Vector3Int(x, sourceY, 0));
                if (tile != null)
                    repeated.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    private void LateUpdate()
    {
        if (repeatingGrid == null) return;

        float scroll = (transform.position.y - startingCameraY) * verticalParallax;
        Vector3 originalPosition = startingGridPosition;
        originalPosition.y -= scroll;
        originalGrid.localPosition = originalPosition;

        Vector3 repeatedPosition = startingGridPosition;
        repeatedPosition.y -= Mathf.Repeat(scroll, repeatHeight);
        repeatingGrid.localPosition = repeatedPosition;
    }

    private static int PositiveMod(int value, int divisor)
    {
        return (value % divisor + divisor) % divisor;
    }

    private bool TryFindWallPattern(BoundsInt bounds, out int firstRow)
    {
        // Tilemap.cellBounds can retain empty rows after tiles are erased in the
        // editor. Search the painted tiles, rather than trusting bounds.yMax.
        for (int y = bounds.yMax - 2; y >= bounds.yMin; y--)
        {
            if (HasWallsOnBothSides(bounds, y) && HasWallsOnBothSides(bounds, y + 1))
            {
                firstRow = y;
                return true;
            }
        }

        firstRow = 0;
        return false;
    }

    private bool HasWallsOnBothSides(BoundsInt bounds, int y)
    {
        bool left = false;
        bool right = false;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            if (wallTilemap.GetTile(new Vector3Int(x, y, 0)) == null) continue;
            if (x < 0) left = true;
            else if (x > 0) right = true;
            else return false; // This row contains the floor, not just the walls.
        }
        return left && right;
    }
}
