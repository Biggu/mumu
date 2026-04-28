using Cysharp.Threading.Tasks;
using UnityEngine;

public class InGameMap
{
    private GameObject blockPrefab = null;
    private GameObject mapRoot = null;
    
    // Screen and block settings
    private const int SCREEN_WIDTH = 1280;
    private const int SCREEN_HEIGHT = 720;
    private const int BLOCK_SIZE = 60;
    
    // Grid dimensions
    private const int COLUMNS = SCREEN_WIDTH / BLOCK_SIZE;  // 21 columns
    private const int ROWS = SCREEN_HEIGHT / BLOCK_SIZE;     // 12 rows
    
    // Block falling settings
    private const float FALL_SPEED = 200f;  // Pixels per second
    
    private GameObject[,] grid = null;
    private float[] columnTimers = null;
    private const float SPAWN_INTERVAL = 0.5f;  // Seconds between spawns
    
    public async UniTask InitAsync(GameObject _mapRoot = null)
    {
        mapRoot = _mapRoot;
        blockPrefab = await ResourceManager.Instance.LoadAsync<GameObject>(ResourceManager.GetInGamePrefabPath("block"));
        
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        grid = new GameObject[COLUMNS, ROWS];
        columnTimers = new float[COLUMNS];
        
        // Start spawning blocks
        for (int x = 0; x < COLUMNS; x++)
        {
            columnTimers[x] = x * SPAWN_INTERVAL / COLUMNS;  // Stagger spawn times
        }
    }

    public void Update()
    {
        if (blockPrefab == null || grid == null)
            return;

        // Update spawn timers and spawn new blocks
        for (int x = 0; x < COLUMNS; x++)
        {
            columnTimers[x] -= Time.deltaTime;
            
            if (columnTimers[x] <= 0)
            {
                SpawnBlock(x);
                columnTimers[x] = SPAWN_INTERVAL;
            }
        }

        // Move blocks to the right
        MoveBlocks();
    }

    private void SpawnBlock(int columnX)
    {
        // Find the lowest empty row in this column
        int spawnRow = -1;
        
        for (int y = 0; y < ROWS; y++)
        {
            if (grid[columnX, y] == null)
            {
                spawnRow = y;
                break;
            }
        }

        // If column is full, don't spawn
        if (spawnRow == -1)
            return;

        // Create block at the left edge of this column
        Vector3 position = new Vector3(
            columnX * BLOCK_SIZE + BLOCK_SIZE / 2,
            spawnRow * BLOCK_SIZE + BLOCK_SIZE / 2,
            0
        );

        GameObject block = GameObject.Instantiate(blockPrefab, position, Quaternion.identity, mapRoot.transform);
        grid[columnX, spawnRow] = block;
    }

    private void MoveBlocks()
    {
        for (int x = 0; x < COLUMNS; x++)
        {
            for (int y = 0; y < ROWS; y++)
            {
                GameObject block = grid[x, y];
                if (block == null)
                    continue;

                // Move block to the right
                Vector3 position = block.transform.position;
                position.x += FALL_SPEED * Time.deltaTime;

                // Calculate new column and row
                int newColumn = Mathf.FloorToInt(position.x / BLOCK_SIZE);
                int newRow = Mathf.FloorToInt(position.y / BLOCK_SIZE);

                // Check if block has moved to a new column
                if (newColumn != x)
                {
                    // Validate new position
                    if (newColumn >= 0 && newColumn < COLUMNS && newRow >= 0 && newRow < ROWS)
                    {
                        // Check if destination is empty
                        if (grid[newColumn, newRow] == null)
                        {
                            // Move to new position
                            grid[x, y] = null;
                            grid[newColumn, newRow] = block;
                            x = newColumn;
                            y = newRow;
                        }
                        else
                        {
                            // Stop at collision - clamp to current column
                            position.x = x * BLOCK_SIZE + BLOCK_SIZE / 2;
                        }
                    }
                    else if (newColumn >= COLUMNS)
                    {
                        // Block has fallen off screen - destroy it
                        GameObject.Destroy(block);
                        grid[x, y] = null;
                    }
                }

                block.transform.position = position;
            }
        }
    }
