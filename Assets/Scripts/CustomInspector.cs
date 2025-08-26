using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameLogic))]
public class CustomInspector : Editor
{
    public override void OnInspectorGUI() {
        serializedObject.Update();

        SerializedProperty challengeModeProp = serializedObject.FindProperty("challengeMode");
        EditorGUILayout.PropertyField(challengeModeProp, new GUIContent("Challenge Mode"));

        if (challengeModeProp.boolValue) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("totalNumberOfTrials"), new GUIContent("Total Number Of Trials"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trialDurationDecrement"), new GUIContent("Trial Duration Decrement"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trialDuration"), new GUIContent("Trial Duration"));
            SerializedProperty useDistractorsProp = serializedObject.FindProperty("useDistractors");
            EditorGUILayout.PropertyField(useDistractorsProp, new GUIContent("Use Distractors"));
            
            if (useDistractorsProp.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("distractorSpawnChance"), new GUIContent("Distractor Spawn Chance"));
        }
        else {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("numberOfTrials"), new GUIContent("Number Of Trials"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("trialDuration"), new GUIContent("Trial Duration"));
        }

        DrawPropertiesExcluding(
            serializedObject,
            "challengeMode",
            "totalNumberOfTrials",
            "trialDurationDecrement",
            "distractorSpawnChance",
            "useDistractors",
            "numberOfTrials",
            "trialDuration",
            "sessionLock",
            "goOnLock"
        );

        serializedObject.ApplyModifiedProperties();
    }
}
