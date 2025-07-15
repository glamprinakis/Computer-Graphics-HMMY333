using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class Grid : MonoBehaviour
{
    public GameObject[] treePrefabs;
    public Material terrainMaterial;
    public Material edgeMaterial;
    public float waterLevel = .4f;
    public float scale = .1f;
    public float treeNoiseScale = .05f;
    public float treeDensity = .5f;
    public int size = 100;
    public NavMeshSurface surface;

    Cell[,] grid;

    void Awake()
    {
        // Initializing the noise map
        float[,] noiseMap = new float[size, size];

        // Generating random offsets for the noise map
        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        // Looping through each point in the map
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Assigning noise values to the map
                float noiseValue = Mathf.PerlinNoise(x * scale + xOffset, y * scale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        // Initializing the falloff map
        float[,] falloffMap = new float[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Assigning values to the falloff map
                float xv = x / (float)size * 2 - 1;
                float yv = y / (float)size * 2 - 1;
                float v = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Mathf.Pow(v, 3f) / (Mathf.Pow(v, 3f) + Mathf.Pow(2.2f - 2.2f * v, 3f));
            }
        }

        // Initializing the grid
        grid = new Cell[size, size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Calculating noise values by subtracting the falloff from the noise map
                float noiseValue = noiseMap[x, y];
                noiseValue -= falloffMap[x, y];

                // Checking if the value is less than the water level to determine if the cell is water
                bool isWater = noiseValue < waterLevel;

                // Creating a new cell
                Cell cell = new Cell(isWater);
                grid[x, y] = cell;
            }
        }

        // Calling various functions to handle drawing and generation
        DrawTerrainMesh(grid);
        DrawEdgeMesh(grid);
        DrawTexture(grid);
        GenerateTrees(grid);

        // Building the navigation mesh
        surface.BuildNavMesh();

        // After everything else is done, manually assign the material to the MeshRenderer component
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material = terrainMaterial;
        }
        else
        {
            Debug.LogError("No MeshRenderer component found on this GameObject.");
        }
    }

    void DrawTerrainMesh(Cell[,] grid)
    {
        // Initializing a new mesh
        Mesh mesh = new Mesh();

        // Initializing lists to hold vertices, triangles, and UVs
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Looping through each cell in the grid
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Cell cell = grid[x, y];

                // If the cell is not water, add its vertices, triangles, and UVs
                if (!cell.isWater)
                {
                    // Defining the vertices for the current cell
                    Vector3 a = new Vector3(x - .5f, 0, y + .5f);
                    Vector3 b = new Vector3(x + .5f, 0, y + .5f);
                    Vector3 c = new Vector3(x - .5f, 0, y - .5f);
                    Vector3 d = new Vector3(x + .5f, 0, y - .5f);

                    // Defining the UVs for the current cell
                    Vector2 uvA = new Vector2(x / (float)size, y / (float)size);
                    Vector2 uvB = new Vector2((x + 1) / (float)size, y / (float)size);
                    Vector2 uvC = new Vector2(x / (float)size, (y + 1) / (float)size);
                    Vector2 uvD = new Vector2((x + 1) / (float)size, (y + 1) / (float)size);

                    // Combining vertices and UVs into arrays
                    Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                    Vector2[] uv = new Vector2[] { uvA, uvB, uvC, uvB, uvD, uvC };

                    // Adding vertices, triangles, and UVs to their respective lists
                    for (int k = 0; k < 6; k++)
                    {
                        vertices.Add(v[k]);
                        triangles.Add(triangles.Count);
                        uvs.Add(uv[k]);
                    }
                }
            }
        }

        // Converting lists to arrays and assigning them to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        // Recalculating normals
        mesh.RecalculateNormals();

        // Adding a MeshFilter component to the game object and assigning the mesh to it
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Adding a MeshRenderer component to the game object and assigning the terrain material to it
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;
    }


    void DrawEdgeMesh(Cell[,] grid)
    {
        // Initializing a new mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Looping through each cell in the grid
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Cell cell = grid[x, y];

                // If the cell is not water, check surrounding cells
                if (!cell.isWater)
                {
                    // Check cell to the left, if it exists and is water, add edge vertices and triangles
                    // The same process is repeated for the right, down, and up cells
                    // This will create a vertical wall where the terrain meets the water, giving the appearance of a coastline
                    // The new vertices are offset by the position of the GameObject, ensuring they are positioned correctly in world space
                    // The y component of the lower vertices is set to -5, creating a downward extending edge

                    // Look at the cell to the left of the current one
                    if (x > 0)
                    {
                        Cell left = grid[x - 1, y];
                        if (left.isWater)
                        {
                            // If the left cell is water, create four vertices for a quad,
                            // forming the edge between this land cell and the water cell.
                            Vector3 a = new Vector3(x - .5f, 0, y + .5f) + transform.position;
                            Vector3 b = new Vector3(x - .5f, 0, y - .5f) + transform.position;
                            Vector3 c = new Vector3(x - .5f, -5, y + .5f) + transform.position; 
                            Vector3 d = new Vector3(x - .5f, -5, y - .5f) + transform.position;

                            // The quad is divided into two triangles for rendering.
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };

                            // Add these vertices to the mesh, and the index of each vertex to the triangles list.
                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }
                    // Similar blocks of code for the right, down, and up cells.
                    // Each creates an edge mesh where there is a transition from land to water.

                    // The right check (x < size - 1) creates a quad on the right side of the land cell.
                    // The down check (y > 0) creates a quad on the bottom side of the land cell.
                    // The up check (y < size - 1) creates a quad on the top side of the land cell.

                    // In each case, four vertices are defined for the quad and added to the vertices list.
                    // Their indices are added to the triangles list to create two triangles, which are rendered as a quad.
                    // The vertices are arranged in a specific order to ensure that the triangles' normals (their "front" sides) point outwards.
                    if (x < size - 1)
                    {
                        Cell right = grid[x + 1, y];
                        if (right.isWater)
                        {
                            Vector3 a = new Vector3(x + .5f, 0, y - .5f) + transform.position;
                            Vector3 b = new Vector3(x + .5f, 0, y + .5f) + transform.position;
                            Vector3 c = new Vector3(x + .5f, -5, y - .5f) + transform.position; 
                            Vector3 d = new Vector3(x + .5f, -5, y + .5f) + transform.position; 
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }
                    if (y > 0)
                    {
                        Cell down = grid[x, y - 1];
                        if (down.isWater)
                        {
                            Vector3 a = new Vector3(x - .5f, 0, y - .5f) + transform.position;
                            Vector3 b = new Vector3(x + .5f, 0, y - .5f) + transform.position;
                            Vector3 c = new Vector3(x - .5f, -5, y - .5f) + transform.position; 
                            Vector3 d = new Vector3(x + .5f, -5, y - .5f) + transform.position; 
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }
                    if (y < size - 1)
                    {
                        Cell up = grid[x, y + 1];
                        if (up.isWater)
                        {
                            Vector3 a = new Vector3(x + .5f, 0, y + .5f) + transform.position;
                            Vector3 b = new Vector3(x - .5f, 0, y + .5f) + transform.position;
                            Vector3 c = new Vector3(x + .5f, -5, y + .5f) + transform.position; 
                            Vector3 d = new Vector3(x - .5f, -5, y + .5f) + transform.position; 
                            Vector3[] v = new Vector3[] { a, b, c, b, d, c };
                            for (int k = 0; k < 6; k++)
                            {
                                vertices.Add(v[k]);
                                triangles.Add(triangles.Count);
                            }
                        }
                    }
                }
            }
        }
        // Assign the vertices and triangles to the mesh and recalculate its normals
        // Then create a new GameObject for the edge mesh, set it as a child of the terrain GameObject
        // And add MeshFilter and MeshRenderer components, assigning the mesh and the edge material
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        GameObject edgeObj = new GameObject("Edge");
        edgeObj.transform.SetParent(transform);

        MeshFilter meshFilter = edgeObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = edgeObj.AddComponent<MeshRenderer>();
        meshRenderer.material = edgeMaterial;


    }

    void DrawTexture(Cell[,] grid)
    {
        // Create a new texture and a color array to hold the color of each pixel
        Texture2D texture = new Texture2D(size, size);
        Color[] colorMap = new Color[size * size];

        // Loop through each cell in the grid and set the corresponding pixel in the color array to green if the cell is land, blue if it's water
        // Then apply the color array to the texture
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Cell cell = grid[x, y];
                if (cell.isWater)
                    colorMap[y * size + x] = Color.blue;
                else
                    colorMap[y * size + x] = Color.green;
            }
        }
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(colorMap);
        texture.Apply();

        // Retrieve the MeshRenderer component and assign the terrain material and the new texture to it
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = terrainMaterial;
        meshRenderer.material.mainTexture = texture;
    }

    void GenerateTrees(Cell[,] grid)
    {
        // This function works similarly to the initial terrain generation, but uses different noise scale and offset values to create more variety
        // The noise map is used to determine where to place trees, with lower noise values corresponding to denser forests

        // Initialize a new noise map with random offset
        float[,] noiseMap = new float[size, size];
        (float xOffset, float yOffset) = (Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));
        // Fill the noise map with Perlin noise values

        // Loop through each cell in the grid and if the cell is not water, generate a random value v
        // If the noise value is less than v, instantiate a tree prefab at the cell's position with random rotation and scale
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noiseValue = Mathf.PerlinNoise(x * treeNoiseScale + xOffset, y * treeNoiseScale + yOffset);
                noiseMap[x, y] = noiseValue;
            }
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Cell cell = grid[x, y];
                if (!cell.isWater)
                {
                    // Get position offset
                    Vector3 positionOffset = transform.position;

                    float v = Random.Range(0f, treeDensity);
                    if (noiseMap[x, y] < v)
                    {
                        GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                        GameObject tree = Instantiate(prefab, transform);
                        tree.transform.position = new Vector3(x, 0, y) + positionOffset; // Add position offset here
                        tree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                        tree.transform.localScale = Vector3.one * Random.Range(.8f, 1.2f);
                    }
                }
            }
        }
    }

    public Vector3 GetRandomNonWaterCellPosition(string biome)
    {
        // Create a list to store non-water cell positions
        List<Vector3> nonWaterCellPositions = new List<Vector3>();

        // Set default grid origin
        Vector3 gridOrigin = Vector3.zero;

        // Determine the grid based on biome
        switch (biome)
        {
            case "Forest":
                gridOrigin = new Vector3(0, 0, 0);
                break;
            case "Tropical":
                gridOrigin = new Vector3(-170, 0, 0);
                break;
            case "Savana":
                gridOrigin = new Vector3(-170, 0, -170);
                break;
            case "SnowForest":
                gridOrigin = new Vector3(0, 0, -170);
                break;
        }

        // Iterate through the grid cells
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Cell cell = grid[x, y];
                if (!cell.isWater)
                {
                    // Add the position of the non-water cell to the list
                    nonWaterCellPositions.Add(new Vector3(x, 0, y) + gridOrigin);
                }
            }
        }

        // Check if there are any non-water cell positions
        if (nonWaterCellPositions.Count == 0)
        {
            Debug.LogWarning("No non-water cells found in the grid.");
            return Vector3.zero;
        }

        // Select a random non-water cell position from the list
        Vector3 randomPosition = nonWaterCellPositions[Random.Range(0, nonWaterCellPositions.Count)];
        return randomPosition;
    }

}