
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// Terrain Type Definition
[System.Serializable]
public class TerrainType
{
    public string Name;
    public float RiverChance;   
    public Vector2 RiverStart;
    public Vector2 RiverEnd;
    public Func<int, int, System.Random, float> HeightFunction;

    public TerrainType(string name, float riverChance, Vector2 riverStart, Vector2 riverEnd, Func<int, int, System.Random, float> heightFunction)
    {
        Name = name;
        RiverChance = riverChance;
        RiverStart = riverStart;
        RiverEnd = riverEnd;
        HeightFunction = heightFunction;
    }
}

public class TerrainGenerator : MonoBehaviour
{
    [Header("References")]
    public Terrain terrain;
    public Gradient terrainGradient;

    [Header("Generation Parameters")]
    public int mapSize = 512;
    public float heightScale = 50f;
    public int smoothingKernelSize = 5;
    public int smoothingIterations = 2;
    
    [Header("River Parameters")]
    public float riverWidth = 10f;
    public float riverDepth = 5f;
    public float riverCurveFrequency = 0.01f;
    public float riverBankNoiseScale = 0.05f;
    public float riverBankNoiseAmplitude = 3f;
    
    private int selectedTerrainIndex = 0;
    private List<TerrainType> terrainTypes;
    private TerrainType currentTerrainType;

    [Header("Randomization")]
    public bool useRandomSeed = true;
    public int fixedSeed = 12345;
    private System.Random prng;

    [Header("River Visualization")]
    public Material riverLineMaterial;
    public float lineWidth = 2f;
    public float heightOffset = 5f;

    private GameObject riverVisualization;

    // Type-specific parameters
    private float valleyFlatNoiseScale;
    private float valleySlopeNoiseScale;

    private List<Vector2> generatedRiverPath = new List<Vector2>();
    
    void Start()
    {
        InitializeDefaultTerrainTypes();
    }

    // Getters
    
    public float[,] GetHeightmap()
    {
        TerrainData terrainData = terrain.terrainData;
        return terrainData.GetHeights(0, 0, mapSize, mapSize);
    }
    
    public TerrainType GetCurrentTerrainType()
    {
        return currentTerrainType;
    }

    public List<Vector2> getRiverPath()
    {
        return generatedRiverPath;
    }

    void InitializeDefaultTerrainTypes()
    {
        terrainTypes = new List<TerrainType>
        {
            new TerrainType("Plains", 1.0f, new Vector2(0.5f, 0), new Vector2(0.5f, 1f), (x, y, rand) =>
            {
                float noiseScale = 0.005f + GetRandomFloat(rand, 0.005f, 4);
                return Mathf.PerlinNoise(x * noiseScale, y * noiseScale) * 0.1f;
            }),

            new TerrainType("Seaside", 0.5f, new Vector2(0.5f, 0), new Vector2(0.5f, 1f), (x, y, rand) =>
            {
                float noiseScale = 0.01f + GetRandomFloat(rand, 0.01f, 4);
                float noise = Mathf.PerlinNoise(x * noiseScale, y * noiseScale) * 0.1f;
                float fractalCoast = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.1f - 0.05f;
                float seaThreshold = mapSize * 0.8f;
                float sea = ((y + fractalCoast * mapSize) < seaThreshold) ? 0.1f : 0f;
                return noise + sea;
            }),

            new TerrainType("SeasideCliff", 0.0f, new Vector2(0.5f, 0), new Vector2(0.5f, 1f), (x, y, rand) =>
            {
                float NoiseScale = 0.01f + GetRandomFloat(rand, 0.01f, 4);
                float noise = Mathf.PerlinNoise(x *NoiseScale, y * NoiseScale) * 0.1f;
                
                float fractalCoast = Mathf.PerlinNoise(x * 0.7f, y * 0.7f) * 0.15f - 0.07f;
                                
                float seaThreshold = mapSize * 0.8f;
                
                float effectiveY = y + fractalCoast * mapSize;
                effectiveY = Mathf.Clamp(effectiveY, 0, mapSize); 
                
                float sea = (effectiveY < seaThreshold) ? 0.5f : 0f;
                
                return noise + sea;
                
            }),

            new TerrainType("Cauldron", 0.0f, new Vector2(0.5f, 0), new Vector2(0.5f, 1f), (x, y, rand) =>
            {
                float noiseScale = 0.05f + GetRandomFloat(rand, 0.05f, 4);

                float dx = x - mapSize / 2f;
                float dy = y - mapSize / 2f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                float t = distance / (mapSize * 0.65f);
                float baseElevation = 0f;
                if(t < 0.5f)
                {
                    baseElevation = 0.1f;
                } else if(t < 0.7f)
                {
                    baseElevation = Mathf.Lerp(0.1f, 0.6f, (t - 0.5f) / 0.2f);
                } else {
                    baseElevation = 0.8f; 
                }
                float noise = (Mathf.PerlinNoise(x * noiseScale, y * noiseScale) - 0.5f) * 0.1f;

                return baseElevation + noise;
            }),

            new TerrainType("Valley", 1.0f, new Vector2(0, 0.5f), new Vector2(1f, 0.5f), (x, y, rand) =>
            {
                float dy = Mathf.Abs(y - mapSize / 2f);
                float normalized = dy / (mapSize / 2f);
                
                // Determine noise scale based on position
                float noiseScale = normalized <= 0.5f 
                    ? valleyFlatNoiseScale 
                    : valleySlopeNoiseScale;

                float valleyElevation = 0.2f;
                if (normalized > 0.5f)
                {
                    float t = (normalized - 0.4f) / 0.6f;
                    valleyElevation = Mathf.SmoothStep(0.2f, 0.9f, t);
                }
                
                float noise = Mathf.PerlinNoise(x * noiseScale, y * noiseScale) * 0.1f;
                return valleyElevation + noise;
            })
        };
    }

    public void SetTypeGenerateTerrain(int typeIndex)
    {
        selectedTerrainIndex = Mathf.Clamp(typeIndex, 0, terrainTypes.Count - 1);
        currentTerrainType = terrainTypes[selectedTerrainIndex];
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        if (terrain == null) return;

        TerrainData terrainData = terrain.terrainData;
        terrainData.heightmapResolution = mapSize + 1;
        terrainData.size = new Vector3(mapSize, heightScale, mapSize);
        GenerateNewSeed();

        if(currentTerrainType.Name == "Valley")
        {
            valleyFlatNoiseScale = 0.01f + GetRandomFloat(prng, 0.01f, 4);
            valleySlopeNoiseScale = 0.25f + GetRandomFloat(prng, 0.1f, 4);
        }
        
        float[,] heights = GenerateHeightmap(currentTerrainType);

        if (ShouldGenerateRiver())
        {
            GenerateRiver(heights);
        }

        SmoothTerrain(heights, kernelSize: smoothingKernelSize, iterations: smoothingIterations);

        terrainData.SetHeights(0, 0, heights);
        ApplyHeightmapTexture(heights);
    }

    float[,] GenerateHeightmap(TerrainType terrainType)
    {
        float[,] heights = new float[mapSize, mapSize];
        
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                heights[y, x] = Mathf.Max(0f, currentTerrainType.HeightFunction(x, y, prng));
            }
        }
        return heights;
    }

    void SmoothTerrain(float[,] heights, int kernelSize = 5, int iterations = 2)
    {
        float[,] buffer = new float[mapSize, mapSize];
        
        for (int i = 0; i < iterations; i++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    buffer[y, x] = CalculateSmoothedValue(x, y, heights, kernelSize);
                }
            }
            
            // Swap buffers
            (heights, buffer) = (buffer, heights);
        }
    }

    float CalculateSmoothedValue(int x, int y, float[,] heights, int kernelSize)
    {
        float sum = 0;
        int count = 0;
        int halfKernel = kernelSize / 2;

        for (int ky = -halfKernel; ky <= halfKernel; ky++)
        {
            for (int kx = -halfKernel; kx <= halfKernel; kx++)
            {
                int sampleX = Mathf.Clamp(x + kx, 0, mapSize - 1);
                int sampleY = Mathf.Clamp(y + ky, 0, mapSize - 1);
                sum += heights[sampleY, sampleX];
                count++;
            }
        }

        return sum / count;
    }

    void ApplyHeightmapTexture(float[,] heights)
    {
        Texture2D texture = new Texture2D(mapSize, mapSize)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] colors = new Color[mapSize * mapSize];
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                colors[y * mapSize + x] = terrainGradient.Evaluate(heights[y, x]);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        terrain.materialTemplate = new Material(Shader.Find("Standard")) 
        {
            mainTexture = texture
        };
    }

    // River

    void GenerateRiver(float[,] heights)
    {
        Vector2 start = currentTerrainType.RiverStart * mapSize;
        Vector2 end = currentTerrainType.RiverEnd * mapSize;
        generatedRiverPath = GenerateRiverPath(start, end);
        
        foreach (var point in generatedRiverPath)
        {
            CreateRiverAtPoint(heights, point);
        }

        VisualizeRiverPath();
    }

    public List<Vector2> GenerateRiverPath(Vector2 start, Vector2 end)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 direction = (end - start).normalized;
        float length = Vector2.Distance(start, end);
        int steps = Mathf.CeilToInt(length);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 basePoint = Vector2.Lerp(start, end, t);

            // Add perpendicular offset for curvature
            float offset = Mathf.Sin(t * Mathf.PI * riverCurveFrequency) * 
                          (float)prng.NextDouble() * riverWidth * 0.5f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 curvedPoint = basePoint + perpendicular * offset;

            path.Add(curvedPoint);
        }

        return path;
    }

    void CreateRiverAtPoint(float[,] heights, Vector2 point)
    {
        int centerX = Mathf.RoundToInt(point.x);
        int centerY = Mathf.RoundToInt(point.y);

        int radius = Mathf.CeilToInt(riverWidth * 0.5f);
        int startX = Mathf.Clamp(centerX - radius, 0, mapSize - 1);
        int endX = Mathf.Clamp(centerX + radius, 0, mapSize - 1);
        int startY = Mathf.Clamp(centerY - radius, 0, mapSize - 1);
        int endY = Mathf.Clamp(centerY + radius, 0, mapSize - 1);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), point);
                float normalizedDistance = distance / (riverWidth * 0.5f);

                if (normalizedDistance <= 1f)
                {
                    // Calculate bank noise
                    float bankNoise = Mathf.PerlinNoise(
                        x * riverBankNoiseScale, 
                        y * riverBankNoiseScale
                    ) * riverBankNoiseAmplitude;

                    // Calculate depth with smooth transition
                    float depthFactor = Mathf.SmoothStep(1f, 0f, normalizedDistance);
                    float depth = riverDepth * depthFactor + bankNoise;

                    // Apply depth to heightmap
                    heights[y, x] = Mathf.Max(0, heights[y, x] - depth / heightScale);
                }
            }
        }
    }

    [ContextMenu("Visualize River Path")]
    public void VisualizeRiverPath()
    {
        if (generatedRiverPath == null || generatedRiverPath.Count == 0)
        {
            Debug.LogWarning("No river path generated!");
            return;
        }

        // Clear existing visualization
        if (riverVisualization != null)
        {
            DestroyImmediate(riverVisualization);
        }

        // Create container object
        riverVisualization = new GameObject("RiverPathVisualization");
        riverVisualization.transform.SetParent(transform);

        // Create and configure LineRenderer
        LineRenderer lineRenderer = riverVisualization.AddComponent<LineRenderer>();
        lineRenderer.positionCount = generatedRiverPath.Count;
        
        // Material handling
        if (riverLineMaterial == null)
        {
            riverLineMaterial = new Material(Shader.Find("Sprites/Default"));
            riverLineMaterial.color = Color.blue;
        }
        lineRenderer.material = riverLineMaterial;
        
        // Visual settings
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.cyan;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;

        // Get terrain position
        Vector3 terrainPos = terrain.transform.position;
        TerrainData terrainData = terrain.terrainData;

        // Convert river points to world positions
        Vector3[] positions = new Vector3[generatedRiverPath.Count];
        for (int i = 0; i < generatedRiverPath.Count; i++)
        {
            Vector2 point = generatedRiverPath[i];
            
            // Ensure point is within terrain bounds
            int x = Mathf.Clamp(Mathf.RoundToInt(point.x), 0, terrainData.heightmapResolution - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(point.y), 0, terrainData.heightmapResolution - 1);

            // Get height at point
            float height = terrainData.GetHeight(y, x);
            
            // Convert to world space
            positions[i] = new Vector3(
                terrainPos.x + x,
                terrainPos.y + height + heightOffset,
                terrainPos.z + y
            );
        }

        lineRenderer.SetPositions(positions);
    }

    // Helpers

    bool ShouldGenerateRiver()
    {
        return (float)prng.NextDouble() <= currentTerrainType.RiverChance;
    }

    void GenerateNewSeed()
    {
        prng = new System.Random(useRandomSeed ? Environment.TickCount : fixedSeed);
    }

    static float GetRandomFloat(System.Random rand, float limit, int precision = 4)
    {
        float randomValue = (float)rand.NextDouble() * limit;

        return (float)Math.Round(randomValue, precision, MidpointRounding.AwayFromZero);
    }

    public float GetHeightAt(int x, int z)
    {
        if (x < 0 || x >= mapSize || z < 0 || z >= mapSize)
        {
            Debug.LogWarning("Coordinates out of bounds!");
            return 0f;
        }

        float[,] heights = GetHeightmap();
        return heights[z, x] * heightScale; // Scale to world units
    }


    // Context

    [ContextMenu("Generate Plains")]
    void GeneratePlains() => SetTypeGenerateTerrain(0);

    [ContextMenu("Generate Seaside")]
    void GenerateSeaside() => SetTypeGenerateTerrain(1);

    [ContextMenu("Generate Cliffs")]
    void GenerateCliffs() => SetTypeGenerateTerrain(2);

    [ContextMenu("Generate Cauldron")]
    void GenerateCauldron() => SetTypeGenerateTerrain(3);

    [ContextMenu("Generate Valley")]
    void GenerateValley() => SetTypeGenerateTerrain(4);

}
