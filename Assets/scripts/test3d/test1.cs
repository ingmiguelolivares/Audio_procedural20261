using UnityEngine;
using System.Collections.Generic;

public class PrefabsInsideModel : MonoBehaviour
{
    [Header("Prefab a Instanciar (con o sin Rigidbody)")]
    public GameObject prefab;

    [Header("Parámetros")]
    [Tooltip("Número de instancias del prefab")]
    public int instanceCount = 10;

    [Tooltip("Velocidad de movimiento al usar las teclas WASD/flechas")]
    public float speed = 5f;

    // Lista de Rigidbodies para poder actualizarlos en Update
    private List<Rigidbody> rigidbodiesList = new List<Rigidbody>();

    // Guardaremos los límites min y max de la caja delimitadora en mundo
    private Vector3 minBounds;
    private Vector3 maxBounds;

    void Start()
    {
        // 1) Intentar obtener el Renderer en este objeto (o en hijos)
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            Debug.LogError("No se encontró ningún Renderer en este objeto ni en sus hijos.");
            return;
        }

        // 2) Calcular los límites en mundo
        Bounds bounds = rend.bounds;
        minBounds = bounds.min;
        maxBounds = bounds.max;

        // 3) Instanciar los objetos
        for (int i = 0; i < instanceCount; i++)
        {
            // Crear la instancia del prefab
            GameObject instance = Instantiate(prefab);
            instance.name = "InstanciaPrefab_" + i;

            // Hacer a la instancia hija del objeto que tiene este script
            // El segundo parámetro 'true' indica que mantendrá su posición en espacio mundial
            // (puedes usar false si deseas que se reposicione en el sistema de coordenadas local).
            instance.transform.SetParent(transform, true);

            // Ubicarla en un punto aleatorio DENTRO de la bounding box en espacio mundial
            float randomX = Random.Range(minBounds.x+1, maxBounds.x-1);
            float randomY = Random.Range(minBounds.y+1, maxBounds.y-3);
            float randomZ = Random.Range(minBounds.z+1, maxBounds.z-1);
            instance.transform.position = new Vector3(randomX, randomY, randomZ);

            // Asegurar que tenga un Rigidbody
            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = instance.AddComponent<Rigidbody>();
            }

            // Guardar en lista para moverlas luego
            rigidbodiesList.Add(rb);
        }
    }

    void Update()
    {
        // 4) Mover y confinar cada instancia
        float h = Input.GetAxis("Horizontal"); // A/D o flechas
        float v = Input.GetAxis("Vertical");   // W/S o flechas

        foreach (Rigidbody rb in rigidbodiesList)
        {
            // Velocidad deseada en XZ (o podrías modificar Y si lo deseas)
            Vector3 desiredVel = new Vector3(h, 0, v) * speed;

            // Conservamos la componente vertical actual del rigidbody
            rb.linearVelocity = new Vector3(desiredVel.x, rb.linearVelocity.y, desiredVel.z);

            // Limitar posición dentro de la bounding box (coordenadas de mundo)
            Vector3 pos = rb.transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            pos.z = Mathf.Clamp(pos.z, minBounds.z, maxBounds.z);

            rb.transform.position = pos;
        }
    }
}
