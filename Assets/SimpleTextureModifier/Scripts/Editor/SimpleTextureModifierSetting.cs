using UnityEngine;
using UnityEditor;
using System.Collections;

public class SimpleTextureModifierSetting {
	[PreferenceItem("TextureModifier")]
    public static void ShowPreference() {
        bool bValue;
		bValue = EditorPrefs.GetBool(SimpleTextureModifier.KEY, true);
		bValue = EditorGUILayout.Toggle(SimpleTextureModifier.KEY, bValue);

        if (GUI.changed)
			EditorPrefs.SetBool(SimpleTextureModifier.KEY, bValue);
    }
}