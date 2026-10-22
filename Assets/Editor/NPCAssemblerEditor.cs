#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCAssembler))]
public class NPCAssemblerEditor : Editor {
        static private int muertesRabasedas = 0;
        static private float gompocidad = 0;

        public override void OnInspectorGUI() {
                DrawDefaultInspector();

                NPCAssembler assembler = (NPCAssembler)target;

                if (GUILayout.Button("Matar a Rabasedas")) {
                        muertesRabasedas += 1;
                        Debug.Log($"Rabasedas ha muerto {muertesRabasedas} veces");
                }

                EditorGUILayout.Space(10);
                if (GUILayout.Button("Probar generación aleatoria")) {
                        if (assembler.randomConfig != null) {
                                assembler.GenerateRandomNPC(assembler.randomConfig);
                        }
                }

                EditorGUILayout.Space(10);
                if (GUILayout.Button("Por favor borrar eso")) {
                        assembler.ClearCurrentPieces();
                }

                EditorGUILayout.Space(10);
		gompocidad = EditorGUILayout.Slider("Gompocidad", gompocidad, -50.0f, 700.0f);
                EditorGUILayout.Space(10);
        }
}
#endif
