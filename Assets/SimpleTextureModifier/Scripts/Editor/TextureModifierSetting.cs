using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SimpleTextureModifier {
public class TextureModifierSetting {
	[PreferenceItem("TextureModifier")]
    public static void ShowPreference() {
		bool bValue;
		bValue = SimpleTextureModifierSettings.Key;
		bValue = EditorGUILayout.Toggle(TextureModifier.KEY, bValue);
		bool fValue;
		fValue = SimpleTextureModifierSettings.ForceSTMSetting;
		fValue = EditorGUILayout.Toggle(TextureModifier.FORCESTMSETTING, fValue);
		if (GUI.changed) {
			SimpleTextureModifierSettings.Key = bValue;
			SimpleTextureModifierSettings.ForceSTMSetting = fValue;
			SimpleTextureModifierSettings.Save();
		}
    }
}
}
	