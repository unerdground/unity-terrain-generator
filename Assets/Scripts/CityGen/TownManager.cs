using UnityEngine;

public class TownManager : MonoBehaviour
{
    [Header("References")]
    public TerrainGenerator terrainGenerator;
    public DistrictManager districtManager;

    [Header("Town Parameters")]
    public int initialTerrainTypeIndex = 0;

    void Start()
    {
        GenerateTown();
    }

    [ContextMenu("Generate Town")]
    public void GenerateTown()
    {
        terrainGenerator.SetTypeGenerateTerrain(initialTerrainTypeIndex);

        // Create test district after terrain generation
        // CreateTestDistrict();

        districtManager.setTerrainGenerator(terrainGenerator);
    }

    void CreateTestDistrict()
    {
        District testDistrict = new District("Central District", "Residential");

        int mapSize = terrainGenerator.mapSize;
        int centerX = mapSize / 2;
        int centerZ = mapSize / 2;
        int districtWidth = 100; // Width of the district in units
        int districtHeight = 100; // Height of the district in units

        // Define the four corners of the district
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(centerX - districtWidth / 2, 0, centerZ - districtHeight / 2), // Bottom-left
            new Vector3(centerX + districtWidth / 2, 0, centerZ - districtHeight / 2), // Bottom-right
            new Vector3(centerX + districtWidth / 2, 0, centerZ + districtHeight / 2), // Top-right
            new Vector3(centerX - districtWidth / 2, 0, centerZ + districtHeight / 2)  // Top-left
        };

        // Set the Y-coordinates using the terrain heightmap
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i].y = terrainGenerator.GetHeightAt((int)corners[i].x, (int)corners[i].z);
        }

        // Add the points to the district
        foreach (var corner in corners)
        {
            Point point = new Point(corner);
            testDistrict.Points.Add(point);
        }

        // Add district to DistrictManager
        districtManager.AddDistrict(testDistrict);

        // Visualize districts
        districtManager.VisualizeDistricts();
    }

    // Getters

    public float[,] GetHeightmap()
    {
        return terrainGenerator.GetHeightmap();
    }

}