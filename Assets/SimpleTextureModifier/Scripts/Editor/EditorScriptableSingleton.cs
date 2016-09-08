using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

public class EditorScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
{
	private static T s_Instance;
	public static T instance {
		get {
			if (EditorScriptableSingleton<T>.s_Instance == null) {
				EditorScriptableSingleton<T>.CreateOrLoad();
			}
			return EditorScriptableSingleton<T>.s_Instance;
		}
	}

	protected EditorScriptableSingleton() {
		if (EditorScriptableSingleton<T>.s_Instance != null) {
			Debug.LogError("EditorScriptableSingleton already exists. Did you query the singleton in a constructor?");
		} else {
			EditorScriptableSingleton<T>.s_Instance = (this as T);
		}
	}

	private static void CreateOrLoad() {
		string filePath = EditorScriptableSingleton<T>.GetFilePath();
		if (!string.IsNullOrEmpty(filePath)) {
			var objs=InternalEditorUtility.LoadSerializedFileAndForget(filePath);
			if (objs.Length > 0)
				s_Instance = objs [0] as T;
		}
		if (EditorScriptableSingleton<T>.s_Instance == null) {
			T t = ScriptableObject.CreateInstance<T>();
			t.hideFlags = HideFlags.HideAndDontSave;
			s_Instance = t;
			var b = s_Instance as EditorScriptableSingleton<T>;
			b.OnCreateInstance();
		}
	}

	protected virtual void OnCreateInstance() {
		return;
	}

	public static void Save(bool saveAsText=true) {
		var t = s_Instance as EditorScriptableSingleton<T>;
		t.SaveData(saveAsText);
	}

	public virtual void SaveData(bool saveAsText=true) {
		if (EditorScriptableSingleton<T>.s_Instance == null)
		{
			Debug.Log("Cannot save ScriptableSingleton: no instance!");
			return;
		}
		string filePath = EditorScriptableSingleton<T>.GetFilePath();
		if (!string.IsNullOrEmpty(filePath)) {
			string directoryName = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directoryName)) {
				Directory.CreateDirectory(directoryName);
			}
			InternalEditorUtility.SaveToSerializedFileAndForget(new T[]{EditorScriptableSingleton<T>.s_Instance},filePath,saveAsText);
		}
	}
	private static string GetFilePath() {
		var type = typeof(T);
		return System.IO.Directory.GetCurrentDirectory () + "/ProjectSettings/" + type.Name + ".asset";
	}
}
	