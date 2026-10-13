using UnityEditor;
using UnityEngine;

/// <summary>
/// Proyecto: Síntesis de Audio Procedural con Inteligencia Artificial.
/// Desarrollado por: Miguel Ángel Olivares Tenorio.
/// Descripción: Inspector educativo para configurar la API key de Groq solo en el equipo local del docente o estudiante.
/// </summary>
[CustomEditor(typeof(GroqTimbreGenerator))]
public class GroqTimbreGeneratorEditor : Editor
{
    private string pendingApiKey = string.Empty;

    /// <summary>
    /// Dibuja el Inspector personalizado y evita que la API key quede serializada en escenas, prefabs o builds.
    /// </summary>
    public override void OnInspectorGUI()
    {
        GroqTimbreGenerator generator = (GroqTimbreGenerator)target;

        EditorGUILayout.LabelField("Groq API Key", EditorStyles.boldLabel);
        bool configured = GroqTimbreGenerator.HasEditorStoredApiKey();
        EditorGUILayout.HelpBox(configured ? "CONFIGURADA en este equipo local." : "NO CONFIGURADA en este equipo.", configured ? MessageType.Info : MessageType.Warning);

        pendingApiKey = EditorGUILayout.PasswordField("Nueva clave", pendingApiKey);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Actualizar clave local"))
            {
                GroqTimbreGenerator.SaveEditorApiKey(pendingApiKey);
                generator.SetSessionApiKey(pendingApiKey);
                pendingApiKey = string.Empty;
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Eliminar clave local"))
            {
                GroqTimbreGenerator.DeleteEditorApiKey();
                generator.ClearSessionApiKey();
                pendingApiKey = string.Empty;
                EditorUtility.SetDirty(generator);
            }
        }

        EditorGUILayout.HelpBox(
            "La clave se guarda con EditorPrefs, fuera de Assets. EditorPrefs no cifra secretos; úsalo solo como configuración local educativa. No se incluye en escenas, prefabs ni builds WebGL.",
            MessageType.None);

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "m_Script", "legacySerializedGroqApiKey");
        serializedObject.ApplyModifiedProperties();
    }
}
