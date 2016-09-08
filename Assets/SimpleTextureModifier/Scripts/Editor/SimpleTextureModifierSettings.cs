using UnityEngine;
using UnityEditor;
using System.Collections;

namespace SimpleTextureModifier {
public class SimpleTextureModifierSettings : EditorScriptableSingleton<SimpleTextureModifierSettings> {
	[SerializeField]
	bool m_Key;
	static public bool Key {
		get { return instance.m_Key; }
		set { instance.m_Key = value; }}
	[SerializeField]
	bool m_ForceSTMSetting;
	static public bool ForceSTMSetting {
		get { return instance.m_ForceSTMSetting; }
		set { instance.m_ForceSTMSetting = value; }}
	protected override void OnCreateInstance() {
		m_Key = true;
		m_ForceSTMSetting = true;
	}
}
}