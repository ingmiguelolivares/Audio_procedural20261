using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropsSpawner : MonoBehaviour
{

    // 100x3 matrix to hold prefab references (can be null)
    public GameObject[,] prefabMatrix = new GameObject[100, 3];

    // Reference to the prefab to instantiate
    public GameObject Snare, Kick, Bass, Melody, Chords, All, None;

    // Spacing between instantiated objects
    public Vector3 spacing = new Vector3(2, 0, 50);

    void Start()
    {
        // Example: Filling some positions in the matrix with prefabs
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                
                // Fill some positions with the defaultPrefab, others remain null
                if (Random.value > 0.7f) // Randomly decide if the prefab should be instantiated
                {
                    var randomNumber = (int)Random.Range(0, 7);
                    switch (randomNumber)
                    {
                        case (0):
                            prefabMatrix[i, j] = Melody;
                            break;
                        case (1):
                            prefabMatrix[i, j] = Snare;
                            break;
                        case (2):
                            prefabMatrix[i, j] = Bass;
                            break;
                        case (3):
                            prefabMatrix[i, j] = Chords;
                            break;
                        case (4):
                            prefabMatrix[i, j] = Kick;
                            break;
                        case (5):
                            prefabMatrix[i, j] = All;
                            break;
                        case (6):
                            prefabMatrix[i, j] = None;
                            break;
                       
                    }

                    
                }
            }
        }

        // Instantiate non-null prefabs
        InstantiateNonNullPrefabs();
    }

    void InstantiateNonNullPrefabs()
    {
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                // If there's a prefab in this position, instantiate it
                if (prefabMatrix[i, j] != null)
                {
                    Vector3 position = new Vector3(transform.position.x + j - 1 , transform.position.y, ((transform.position.z + i) * 8)+10 ); // Calculate position
                    Instantiate(prefabMatrix[i, j], position, Quaternion.identity);  // Instantiate the prefab
                    //print((transform.position.z + i) * spacing.z);
                }
            }
        }
    }
}
