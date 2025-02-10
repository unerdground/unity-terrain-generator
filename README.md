# **Unity Terrain Generator**  

## **Disclaimer**  
This project is not a fully functional or complete solution but rather a **template** that requires significant customization and fine-tuning to fit specific use cases.  

## **Overview**  
This Unity project includes a **basic first-person/third-person player controller** and a **randomized terrain generation system**. The terrain generator supports custom terrain types with predefined height functions and the possibility of generating river basins based on probability.  

Terrain generation is **seed-based**, allowing for consistent terrain replication as long as the same seed and parameters are used.  

## **Prerequisites**  
- Unity **2021.3.25f1**  

## **Usage**  

### **Importing the Project**  
- Import the entire folder as a Unity project, or  
- Manually integrate specific components by copying the contents of the **Scripts** folder into your existing project.  

### **Setting Up the Player Controller**  
1. Create an empty GameObject and attach the following components:  
   - **CharacterController**  
   - **PlayerController** (located in `Scripts/Player/`)  
2. Add a **capsule** (or a custom player model) as a child of the GameObject.  
3. Create a **First-Person View (FPV) camera**:  
   - Attach a camera as a child of the player model.  
   - Position it appropriately for first-person gameplay.  
4. Set up the **Third-Person View (TPV) camera**:  
   - Create an empty child GameObject as the **camera pivot**.  
   - Add a disabled camera as a child of this pivot (this will serve as the third-person camera).  
5. Assign the respective **cameras and transforms** in the **PlayerController** script.  
6. Adjust parameters as needed for optimal gameplay.  

### **Setting Up the Terrain Generator**  
1. If not already present, create a **Terrain** in your scene.  
2. Create an empty GameObject and add the following components from `Scripts/CityGen/`:  
   - **TownManager**  
   - **DistrictManager**  
   - **TerrainGenerator**  
3. Assign the appropriate references:  
   - Link the **TerrainGenerator** and **DistrictManager** to **TownManager**.  
   - Assign the terrain object to **TerrainGenerator**.  
4. Enter **Play Mode** to generate terrain dynamically.  

---

## **Player Controller**  
The player controller features a **standard 2m tall capsule model** with a small sphere in front to indicate forward direction in third-person view.  

### **Key Features:**  
- **Movement & Gravity:** Uses standard keyboard/mouse controls and is subject to gravity.  
- **Flight Mode (Debug Feature):**  
  - Press **'X'** to toggle flight mode.  
  - In flight mode, gravity is disabled, and the player can move freely in all directions.  
  - **Controls:**  
    - **Space**: Ascend  
    - **Left Ctrl**: Descend  
    - Movement speed is significantly increased.  
- **Camera Modes:**  
  - Switch between **first-person** and **third-person** views by pressing **'V'**.  
  - In third-person mode, a placeholder **"parallax" effect** causes the player model to rotate slightly slower than the camera.  
  - Rapid mouse movements may break the effect, causing a lag between player model rotaion catching up with camera forward orientation.  

---

## **Terrain System**  
The terrain system is divided into three main modules:  

### **1. Town Controller**  
- **Entry point** of the project. 
- Placeholder for the town generation pipeline.
- Currently only responsible for initiating terrain generation.  

### **2. District Controller**  
A placeholder module for **district-based world generation**. Districts are defined by:  
- **Name**  
- **Type** *(currently unused)*  
- **Population** *(currently unused)*  
- **List of points defining the district borders**  

Each **Point** consists of:  
- **Position** *(3D coordinate)*  
- **List of districts it belongs to**  

Districts are represented as **polygons** in a **2D space** (X, Z), with the **Y coordinate reserved for future use**.  
Currently, the only functionality is to create a **test district** in the center of the map.  

### **3. Terrain Generator**  
This module handles **terrain generation**, including height maps, river placement, and terrain type selection.  

#### **Terrain Generation Features:**  
- Generates terrain based on:  
  - **Size**  
  - **Type**  
  - **Smoothing parameters**  
- Supports **river basin generation**, with river probability determined by the selected terrain type.  
- River paths are rendered as **lines** on the terrain.  

#### **Terrain Type Definition:**  
Each terrain type is defined by:  
- **Name** *(string)*  
- **RiverChance** *(float, 0.0 - 1.0)*  
- **RiverStart Position** *(Vector2, 0.0 - 1.0) → Represents percentage of map size (x * mapSize, y * mapSize)*  
- **RiverEnd Position** *(Vector2, 0.0 - 1.0)*  
- **Height Function** *(custom function for height calculation, using x, y, and a System.Random instance for seed-based randomness)*  

#### **Available Terrain Types:**  
- **Plains** – Standard flat terrain.  
- **Seaside** – Flat terrain with a lowered sea area.  
- **Cliffs** – Elevated flat terrain with steep drops to sea level.  
- **Cauldron** – A flat area surrounded by steep mountains.  
- **Valley** – A central flat area walled by mountains on opposite sides.  

**Note:** While the **Cauldron** and **Valley** terrain types technically function, their results does not appear natural. Modifying their height and smoothing functions is strongly recommended for improved quality.  

#### **Dynamic Terrain Regeneration:**  
- The default starting terrain type is set via **TownManager** (using a type index).  
- While in **Editor Play Mode**, the terrain can be regenerated dynamically by selecting different terrain types through the **context menu** in the **Terrain Generator** component on the TownManager entity.  

#### **Terrain Visualization:**  
- Terrain is colored using an **elevation-based gradient**.  
- Gradient colors and levels can be adjusted in the **Terrain Generator parameters**.  
- **Note:** The gradient is only visible while the project is running in **Play Mode**.  

---

## **Default Terrain Generator Parameters**  
The following settings have been fine-tuned to produce **natural-looking** terrain for the **Plains, Seaside, and Cliffs** terrain types:  

| **Parameter**                | **Value**  |  
|------------------------------|-----------|  
| **Height Scale**             | 250       |  
| **Smoothing Kernel Size**    | 5         |  
| **Smoothing Iterations**     | 6         |  
| **River Width**              | 35        |  
| **River Depth**              | 0.01      |  
| **River Curve Frequency**    | 5         |  
| **River Bank Noise Scale**   | 0.6       |  
| **River Bank Noise Amplifier** | 1.2       |  

---

