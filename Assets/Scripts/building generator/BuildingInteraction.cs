// using System.Collections.Generic;
// using UnityEngine;

// public class BuildingInteraction : MonoBehaviour
// {
//     [System.Serializable]
//     public class Material
//     {
//         public string materialName; // e.g., "Wood", "Iron", "Gold"
//         public string rarity;       // e.g., "Common", "Rare", "Epic"
//         public int weight;          // Weight determines the chance of being selected (higher = more common)

//         public Material(string name, string rarity, int weight)
//         {
//             this.materialName = name;
//             this.rarity = rarity;
//             this.weight = weight;
//         }
//     }
//     public string buildingName;
//     public bool isSearched = false;

//     public List<Material> materials = new List<Material>();
//     public InventoryManager inventory;

//     void Start()
//     {
//         inventory = FindAnyObjectByType<InventoryManager>();
//         // Add materials to the list
//         materials.Add(new Material("Wood", "Common", 50));
//         materials.Add(new Material("Stone", "Common", 50));
//         materials.Add(new Material("Iron", "Rare", 30));
//         materials.Add(new Material("Electronics", "Epic", 10));
//         materials.Add(new Material("Weapon", "Legendary", 5));
//     }

//     public void SearchBuilding()
//     {
//         if(isSearched == false){
//             isSearched = true;
//             for(int i = 0; i < 5; i++){

//                 var mat = GetRandomMaterial();
//                 inventory.AddMaterial(mat.materialName, Random.Range(1, 3));
//             }
//         }
//     }

//     public Material GetRandomMaterial()
//     {
//         int totalWeight = 0;

//         // Calculate total weight
//         foreach (var mat in materials)
//         {
//             totalWeight += mat.weight;
//         }

//         // Choose a random value within the total weight
//         int randomValue = Random.Range(0, totalWeight);

//         // Determine which material corresponds to the random value
//         foreach (var mat in materials)
//         {
//             if (randomValue < mat.weight)
//             {
//                 return mat;
//             }
//             randomValue -= mat.weight;
//         }

//         return null; // Should not reach here if weights are set correctly
//     }
// }
