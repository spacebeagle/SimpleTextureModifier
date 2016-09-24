using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using System;

namespace SimpleTextureModifier {
[InitializeOnLoad]
public class StartupTextureModifier {
    static StartupTextureModifier() {
        Debug.Log("Initialized TextureModifier");
        EditorUserBuildSettings.activeBuildTargetChanged += OnChangePlatform;
    }

    [UnityEditor.MenuItem("Assets/Texture Util/Reimport All Texture", false, 1)]
    static void OnChangePlatform() {
        Debug.Log(" TextureModifier Convert Compress Texture");
        string labels = "t:Texture";
        string clabels = "t:Texture";
		foreach(var type in TextureModifier.compressOutputs){
			clabels+=" l:"+type.ToString();
		}
		string rlabels = "t:Texture";
		foreach(var type in TextureModifier.RGBA16bitsOutputs){
			rlabels+=" l:"+type.ToString();
		}
		string plabels = "t:Texture";
		foreach(var type in TextureModifier.PNGOutputs){
			plabels+=" l:"+type.ToString();
		}
		string jlabels = "t:Texture";
		foreach(var type in TextureModifier.JPGOutputs){
			jlabels+=" l:"+type.ToString();
		}
		AssetDatabase.StartAssetEditing ();
        {
            var assets = AssetDatabase.FindAssets(labels, null);
            foreach (var asset in assets) {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                var obj = AssetDatabase.LoadAssetAtPath(path,typeof(Texture));
                var importer = AssetImporter.GetAtPath(path);
                if (obj != null && importer != null) {
                    List<string> lb = new List<string>(AssetDatabase.GetLabels(obj));
                    importer.userData = String.Join(",", lb.ToArray());
                    AssetDatabase.WriteImportSettingsIfDirty(path);
                }
            }
        }
        {
			var assets = AssetDatabase.FindAssets (clabels, null);
			foreach (var asset in assets) {
				var path = AssetDatabase.GUIDToAssetPath (asset);
				if (CheckTargetCompressTexture (path))
					AssetDatabase.ImportAsset (path);
			}
		}
		{
			var assets = AssetDatabase.FindAssets (rlabels, null);
			foreach (var asset in assets) {
				var path = AssetDatabase.GUIDToAssetPath (asset);
				if (CheckTargetPNGTexture (path))
					AssetDatabase.ImportAsset (path);
			}
		}
		{
			var assets = AssetDatabase.FindAssets (plabels, null);
			foreach (var asset in assets) {
				var path = AssetDatabase.GUIDToAssetPath (asset);
				if (CheckTargetJPGTexture (path))
					AssetDatabase.ImportAsset (path);
			}
		}
		AssetDatabase.StopAssetEditing ();
	}

	static bool CheckTargetCompressTexture(string path){
		if (String.IsNullOrEmpty (path))
			return false;
		Texture2D tex=AssetDatabase.LoadAssetAtPath(path,typeof(Texture2D)) as Texture2D;
		switch (EditorUserBuildSettings.activeBuildTarget) {
		case BuildTarget.Android:
			if(tex.format==TextureFormat.ETC_RGB4)
				return false;
			break;
#if UNITY_5
		case BuildTarget.iOS:
#else
		case BuildTarget.iPhone:
#endif
			if(tex.format==TextureFormat.PVRTC_RGB4 || tex.format==TextureFormat.PVRTC_RGBA4)
				return false;
			break;
		default:
			if(tex.format==TextureFormat.DXT1 || tex.format==TextureFormat.DXT5)
				return false;
			break;
		}
		return true;
	}

	static bool CheckTargetRGBA16bitsTexture(string path){
		if (String.IsNullOrEmpty (path))
			return false;
		Texture2D tex=AssetDatabase.LoadAssetAtPath(path,typeof(Texture2D)) as Texture2D;
		if(tex.format==TextureFormat.RGBA4444 || tex.format==TextureFormat.ARGB4444)
			return false;
		return true;
	}
	static bool CheckTargetPNGTexture(string path){
		if (String.IsNullOrEmpty (path))
			return false;
		Texture2D tex=AssetDatabase.LoadAssetAtPath(path+"RGBA",typeof(Texture2D)) as Texture2D;
		if(tex!=null)
			return false;
		return true;
	}
	static bool CheckTargetJPGTexture(string path){
		if (String.IsNullOrEmpty (path))
			return false;
		Texture2D tex=AssetDatabase.LoadAssetAtPath(path+"RGB",typeof(Texture2D)) as Texture2D;
		if(tex!=null)
			return false;
		return true;
	}
}

public class TextureModifier : AssetPostprocessor {
	public static readonly string KEY = "Texture Output Enable";
	public static readonly string FORCESTMSETTING = "Force STM Setting";

	public enum TextureModifierType {
		None,
		PremultipliedAlpha,
		AlphaBleed,
		FloydSteinberg,
		Reduced16bits,
        C16bits,
        CCompressed,
        CCompressedNA,
        CCompressedWA,
        T32bits,
		T16bits,
		TCompressed,
        TCompressedNA,
        TCompressedWA,
		TPNG,
		TJPG,
	}

	static TextureFormat CompressionFormat {
		get {
			switch (EditorUserBuildSettings.activeBuildTarget) {
			case BuildTarget.Android:
				return TextureFormat.ETC_RGB4;
#if UNITY_5
			case BuildTarget.iOS:
#else
			case BuildTarget.iPhone:
#endif
				return TextureFormat.PVRTC_RGB4;
			default:
				return TextureFormat.DXT1;
			}
		}
	}

	static TextureFormat CompressionWithAlphaFormat {
		get {
			switch (EditorUserBuildSettings.activeBuildTarget) {
			case BuildTarget.Android:
				return TextureFormat.ETC_RGB4;
#if UNITY_5
			case BuildTarget.iOS:
#else
			case BuildTarget.iPhone:
#endif
				return TextureFormat.PVRTC_RGBA4;
			default:
				return TextureFormat.DXT5;
			}
		}
	}

	struct Position2 {
		public int x,y;
		public Position2(int p1, int p2)
		{
			x = p1;
			y = p2;
		}
	}
	
	readonly static List<List<Position2>> bleedTable;
	static TextureModifier(){
		bleedTable=new List<List<Position2>>();
		for(int i=1;i<=8;i++){
			var bT=new List<Position2>();
			for(int x=-i;x<=i;x++){
				bT.Add(new Position2(x,i));
				bT.Add(new Position2(-x,-i));
			}
			for(int y=-i+1;y<=i-1;y++){
				bT.Add(new Position2(i,y));
				bT.Add(new Position2(-i,-y));
			}
			bleedTable.Add(bT);
		}
	}

	readonly static Type inspectorWindowType = Assembly.GetAssembly(typeof(EditorWindow)).GetType ("UnityEditor.InspectorWindow");
	readonly static Type labelGUIType = Assembly.GetAssembly(typeof(EditorWindow)).GetType ("UnityEditor.LabelGUI");
	readonly static FieldInfo m_LabelGUIField = inspectorWindowType.GetField("m_LabelGUI"
	                                                                         ,BindingFlags.GetField | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance);
	readonly static FieldInfo m_CurrentAssetsSetField = labelGUIType.GetField("m_CurrentAssetsSet"
	                                                                          ,BindingFlags.GetField | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance);

	static void ApplySettings() {
		var editors = Resources.FindObjectsOfTypeAll (typeof(Editor)) as Editor[];
		List<UnityEngine.Object> objs=new List<UnityEngine.Object>(Selection.objects);
		foreach(var obj in objs){
			if (obj is Texture2D) {
				TextureImporter importer = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (obj)) as TextureImporter;
				if (importer == null)
					continue;
				foreach (var editor in editors) {
					if (editor.target == importer) {
						var serializedObject = editor.serializedObject;
						serializedObject.ApplyModifiedPropertiesWithoutUndo ();
					}
				}
			}
		}
	}

	static void SetLabelSetingsDirty() {
		if (m_LabelGUIField == null || m_CurrentAssetsSetField == null)
			return;
		var editorWindows = Resources.FindObjectsOfTypeAll (typeof(EditorWindow)) as EditorWindow[];
//		Debug.Log("m_LabelGUIField="+m_LabelGUIField);
		foreach (var ew in editorWindows) {
//			Debug.Log("editorWindow="+ew.title);
//			if (ew.title=="UnityEditor.InspectorWindow" || ew.title=="Inspector") { // "UnityEditor.InspectorWindow"<=v4 "Inspector">=v5
			if (ew.GetType().FullName=="UnityEditor.InspectorWindow") {
                var labelGUIObject = m_LabelGUIField.GetValue(ew);
				m_CurrentAssetsSetField.SetValue(labelGUIObject,null);
			}
		}
		var editors = Resources.FindObjectsOfTypeAll (typeof(Editor)) as Editor[];
		List<UnityEngine.Object> objs=new List<UnityEngine.Object>(Selection.objects);
		foreach(var obj in objs){
			if (obj is Texture2D) {
				TextureImporter importer = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (obj)) as TextureImporter;
				if (importer == null)
					continue;
				foreach (var editor in editors) {
					if (editor.target == importer) {
						var serializedObject = editor.serializedObject;
						var property=serializedObject.FindProperty ("correctGamma");
						EditorApplication.delayCall=()=>{
							EditorApplication.delayCall=()=>{
								property.boolValue = !property.boolValue;
								property.boolValue = !property.boolValue;
								editor.Repaint();
							};
						};
					}
				}
			}
		}
	}

	public readonly static List<TextureModifierType> effecters=new List<TextureModifierType>{TextureModifierType.PremultipliedAlpha,TextureModifierType.AlphaBleed};
    public readonly static List<TextureModifierType> modifiers = new List<TextureModifierType> { TextureModifierType.FloydSteinberg, TextureModifierType.Reduced16bits };
    public readonly static List<TextureModifierType> outputs = new List<TextureModifierType>{TextureModifierType.TJPG,TextureModifierType.TPNG,TextureModifierType.T32bits,TextureModifierType.T16bits,TextureModifierType.C16bits
                                                                            ,TextureModifierType.CCompressed,TextureModifierType.CCompressedNA,TextureModifierType.CCompressedWA
																			,TextureModifierType.TCompressed,TextureModifierType.TCompressedNA,TextureModifierType.TCompressedWA};
    public readonly static List<TextureModifierType> compressOutputs = new List<TextureModifierType>{
                                                                             TextureModifierType.CCompressed,TextureModifierType.CCompressedNA,TextureModifierType.CCompressedWA
                                                                             ,TextureModifierType.TCompressed,TextureModifierType.TCompressedNA,TextureModifierType.TCompressedWA};
	public readonly static List<TextureModifierType> RGBA16bitsOutputs = new List<TextureModifierType>{TextureModifierType.C16bits};
	public readonly static List<TextureModifierType> PNGOutputs = new List<TextureModifierType>{TextureModifierType.TPNG};
	public readonly static List<TextureModifierType> JPGOutputs = new List<TextureModifierType>{TextureModifierType.TJPG};

	static void ClearLabel(List<TextureModifierType> types, bool ImportAsset = true) {
		List<UnityEngine.Object> objs=new List<UnityEngine.Object>(Selection.objects);
		foreach(var obj in objs){
			if(obj is Texture2D){
				List<string> labels=new List<string>(AssetDatabase.GetLabels(obj));
				var newLabels=new List<string>();
				labels.ForEach((string l)=>{
					if(Enum.IsDefined(typeof(TextureModifierType),l)){
						if(!types.Contains((TextureModifierType)Enum.Parse(typeof(TextureModifierType),l)))
							newLabels.Add(l);
					}
				});
				AssetDatabase.SetLabels(obj,newLabels.ToArray());
                var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
				if(newLabels.Count>0)
					importer.userData = String.Join(",", newLabels.ToArray());
				else
					importer.userData = null;
				EditorUtility.SetDirty(obj);
                AssetDatabase.WriteImportSettingsIfDirty(AssetDatabase.GetAssetPath(obj));
            }
		}
	}

	static void SetLabel(string label,List<TextureModifierType> types){
		ClearLabel(types,false);
		List<UnityEngine.Object> objs=new List<UnityEngine.Object>(Selection.objects);
		foreach(var obj in objs){
			if(obj is Texture2D){
				List<string> labels=new List<string>(AssetDatabase.GetLabels(obj));
				labels.Add(label);
				AssetDatabase.SetLabels(obj,labels.ToArray());
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
				if(labels.Count>0)
	                importer.userData = String.Join(",", labels.ToArray());
				else
					importer.userData = null;
                EditorUtility.SetDirty(obj);
                AssetDatabase.WriteImportSettingsIfDirty(AssetDatabase.GetAssetPath(obj));
			}
		}
	}

	[UnityEditor.MenuItem("Assets/Texture Util/Clear Texture Effecter Label",false,20)]
	static void ClearTextureEffecterLabel(){
		ApplySettings ();
		ClearLabel(effecters);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label PremultipliedAlpha",false,20)]
	static void SetLabelPremultipliedAlpha(){
		ApplySettings ();
		SetLabel(TextureModifierType.PremultipliedAlpha.ToString(),effecters);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label AlphaBleed",false,20)]
	static void SetLabelAlphaBleed(){
		ApplySettings ();
		SetLabel(TextureModifierType.AlphaBleed.ToString(),effecters);
		SetLabelSetingsDirty ();
	}

	[UnityEditor.MenuItem("Assets/Texture Util/Clear Texture Modifier Label",false,40)]
	static void ClearTextureModifierLabel(){
		ApplySettings ();
		ClearLabel(modifiers);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label FloydSteinberg",false,40)]
	static void SetLabelFloydSteinberg(){
		ApplySettings ();
		SetLabel(TextureModifierType.FloydSteinberg.ToString(),modifiers);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Reduced16bits",false,40)]
	static void SetLabelReduced16bits(){
		ApplySettings ();
		SetLabel(TextureModifierType.Reduced16bits.ToString(),modifiers);
		SetLabelSetingsDirty ();
	}

	[UnityEditor.MenuItem("Assets/Texture Util/Clear Texture Output Label",false,60)]
	static void ClearTextureOutputLabel(){
		ApplySettings ();
		ClearLabel(outputs);
		SetLabelSetingsDirty ();
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert 16bits", false, 60)]
    static void SetLabelC16bits() {
		ApplySettings ();
        SetLabel(TextureModifierType.C16bits.ToString(), outputs);
		SetLabelSetingsDirty ();
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert Compressed", false, 60)]
    static void SetLabelCCompressed() {
		ApplySettings ();
        SetLabel(TextureModifierType.CCompressed.ToString(), outputs);
		SetLabelSetingsDirty ();
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert Compressed no alpha", false, 60)]
    static void SetLabelCCompressedNA() {
		ApplySettings ();
        SetLabel(TextureModifierType.CCompressedNA.ToString(), outputs);
		SetLabelSetingsDirty ();
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Convert Compressed with alpha", false, 60)]
    static void SetLabelCCompressedWA() {
		ApplySettings ();
        SetLabel(TextureModifierType.CCompressedWA.ToString(), outputs);
		SetLabelSetingsDirty ();
	}
#if false
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture 16bits", false, 60)]
	static void SetLabel16bits(){
		ApplySettings ();
		SetLabel(TextureModifierType.T16bits.ToString(),outputs);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture 32bits",false,60)]
	static void SetLabel32bits(){
		ApplySettings ();
		SetLabel(TextureModifierType.T32bits.ToString(),outputs);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture Compressed",false,60)]
	static void SetLabelCompressed(){
		ApplySettings ();
		SetLabel(TextureModifierType.TCompressed.ToString(),outputs);
		SetLabelSetingsDirty ();
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture Compressed no alpha", false, 60)]
    static void SetLabelCompressedNA() {
		ApplySettings ();
        SetLabel(TextureModifierType.TCompressedNA.ToString(), outputs);
		SetLabelSetingsDirty ();
	}
    [UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture Compressed with alpha", false, 60)]
	static void SetLabelCompressedWA(){
		ApplySettings ();
		SetLabel(TextureModifierType.TCompressedWA.ToString(),outputs);
		SetLabelSetingsDirty ();
	}
#endif
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture PNG",false,60)]
	static void SetLabelPNG(){
		ApplySettings ();
		SetLabel(TextureModifierType.TPNG.ToString(),outputs);
		SetLabelSetingsDirty ();
	}
	[UnityEditor.MenuItem("Assets/Texture Util/Set Label Texture JPG",false,60)]
	static void SetLabelJPG(){
		ApplySettings ();
		SetLabel(TextureModifierType.TJPG.ToString(),outputs);
		SetLabelSetingsDirty ();
	}

	TextureModifierType effecterType=TextureModifierType.None;
	TextureModifierType modifierType=TextureModifierType.None;
	TextureModifierType outputType=TextureModifierType.None;

	void OnPreprocessTexture(){
		//return;
		var importer = (assetImporter as TextureImporter);
		UnityEngine.Object obj=AssetDatabase.LoadAssetAtPath(assetPath,typeof(Texture2D));
		var labels=new List<string>(AssetDatabase.GetLabels(obj));
        if (labels == null || labels.Count == 0) {
			if(!String.IsNullOrEmpty(importer.userData)) {
				labels = importer.userData.Split ("," [0]).ToList ();
				AssetDatabase.SetLabels(obj,labels.ToArray());
				SetLabelSetingsDirty ();
			}
		}
		foreach(string label in labels){
            if (Enum.IsDefined(typeof(TextureModifierType), label))
            {
				TextureModifierType type=(TextureModifierType)Enum.Parse(typeof(TextureModifierType),label);
				if(effecters.Contains(type)){
					effecterType=type;
				}
				if(modifiers.Contains(type)){
					modifierType=type;
				}
				if(outputs.Contains(type)){
					outputType=type;
				}
			}
		}
		if (!String.IsNullOrEmpty (importer.spritePackingTag))
			return;
		if(effecterType!=TextureModifierType.None || modifierType!=TextureModifierType.None || outputType!=TextureModifierType.None){
			if(!SimpleTextureModifierSettings.ForceSTMSetting)
				return;
			importer.alphaIsTransparency=false;
//			importer.compressionQuality = (int)TextureCompressionQuality.Best;
			if(importer.textureFormat==TextureImporterFormat.Automatic16bit)
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			else if(importer.textureFormat==TextureImporterFormat.AutomaticCompressed)
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			else if(importer.textureFormat==TextureImporterFormat.RGB16)
				importer.textureFormat = TextureImporterFormat.RGB24;
			else if(importer.textureFormat==TextureImporterFormat.RGBA16)
				importer.textureFormat = TextureImporterFormat.RGBA32;
			else if(importer.textureFormat==TextureImporterFormat.ARGB16)
				importer.textureFormat = TextureImporterFormat.ARGB32;
		}
	}
	
	void OnPostprocessTexture (Texture2D texture){
		if(effecterType==TextureModifierType.None && modifierType==TextureModifierType.None && outputType==TextureModifierType.None)
			return;
		AssetDatabase.StartAssetEditing();
		var pixels = texture.GetPixels ();
		switch (effecterType){
		case TextureModifierType.PremultipliedAlpha:{
			pixels=PremultipliedAlpha(pixels);
			break;
		}
		case TextureModifierType.AlphaBleed:{
			pixels=AlphaBleed(pixels,texture.width,texture.height);
			break;
		}}
		switch (modifierType){
		case TextureModifierType.FloydSteinberg:{
			pixels=FloydSteinberg(pixels,texture.width,texture.height);
			break;
		}
		case TextureModifierType.Reduced16bits:{
			pixels=Reduced16bits(pixels,texture.width,texture.height);
			break;
		}}
        //return;
		if (SimpleTextureModifierSettings.Key) {
            switch (outputType) {
                case TextureModifierType.C16bits: {
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, TextureFormat.RGBA4444, TextureCompressionQuality.Best); 
                    break;
                }
                case TextureModifierType.CCompressed: {
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, CompressionWithAlphaFormat, TextureCompressionQuality.Best);
                    break;
                }
                case TextureModifierType.CCompressedNA: {
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, CompressionFormat, TextureCompressionQuality.Best);
                    break;
                }
                case TextureModifierType.CCompressedWA: {
                    WriteAlphaTexture(pixels, texture);
                    texture.SetPixels(pixels);
                    texture.Apply(true, true);
                    EditorUtility.CompressTexture(texture, CompressionFormat, TextureCompressionQuality.Best);
                    break;
                }
                case TextureModifierType.TCompressed: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, CompressionWithAlphaFormat, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.TCompressedNA: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, CompressionFormat, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.TCompressedWA: {
                   WriteAlphaTexture(pixels, texture);
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, CompressionFormat, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.T16bits: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, TextureFormat.RGBA4444, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.T32bits: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true, true);
                   WriteTexture(tex, TextureFormat.RGBA32, assetPath, ".asset");
                   break;
               }
               case TextureModifierType.TPNG: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true);
                   WritePNGTexture(tex, TextureFormat.RGBA32, assetPath, "RGBA.png");
                   break;
               }
               case TextureModifierType.TJPG: {
                   var tex = BuildTexture(texture, TextureFormat.RGBA32);
                   tex.SetPixels(pixels);
                   tex.Apply(true);
                   WriteJPGTexture(tex, TextureFormat.RGBA32, assetPath, "RGB.jpg");
                   break;
               }
               default: {
                   if (effecterType != TextureModifierType.None || modifierType != TextureModifierType.None) {
                       texture.SetPixels(pixels);
                       texture.Apply(true);
                   }
                   break;
                }
            }
        }
		AssetDatabase.Refresh();
		AssetDatabase.StopAssetEditing();
	}

	Texture2D BuildTexture(Texture2D texture,TextureFormat format){
		var tex = new Texture2D (texture.width, texture.height, format, texture.mipmapCount>1);
		tex.wrapMode = texture.wrapMode;
		tex.filterMode = texture.filterMode;
		tex.mipMapBias = texture.mipMapBias;
		tex.anisoLevel = texture.anisoLevel;
		return tex;
	}

	void WriteTexture(Texture2D texture,TextureFormat format,string path,string extension){
		EditorUtility.CompressTexture (texture,format,TextureCompressionQuality.Best);
		var writePath = path.Substring(0,path.LastIndexOf('.'))+extension;
		var writeAsset = AssetDatabase.LoadAssetAtPath (writePath,typeof(Texture2D)) as Texture2D;
		if (writeAsset == null) {
			AssetDatabase.CreateAsset (texture, writePath);
		} else {
			EditorUtility.CopySerialized (texture, writeAsset);
		}
	}

	void WritePNGTexture(Texture2D texture,TextureFormat format,string path,string extension){
		EditorUtility.CompressTexture (texture,format,TextureCompressionQuality.Best);
		byte[] pngData=texture.EncodeToPNG();
		//var nPath=path.Substring(0,path.LastIndexOf('.'))+extension;
		var writePath = Application.dataPath+(path.Substring(0,path.LastIndexOf('.'))+extension).Substring(6);
		File.WriteAllBytes(writePath, pngData);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	void WriteJPGTexture(Texture2D texture,TextureFormat format,string path,string extension){
		EditorUtility.CompressTexture (texture,format,TextureCompressionQuality.Best);
		byte[] jpgData=texture.EncodeToJPG();
		//var nPath=path.Substring(0,path.LastIndexOf('.'))+extension;
		var writePath = Application.dataPath+(path.Substring(0,path.LastIndexOf('.'))+extension).Substring(6);
		File.WriteAllBytes(writePath, jpgData);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
	}

	void WriteCompressTexture(Color[] pixels,Texture2D texture,TextureFormat format){
		var mask = BuildTexture(texture,TextureFormat.RGB24);
		for (int i = 0; i < pixels.Length; i++) {
			var a = pixels [i].a;
			pixels [i] = new Color (a, a, a);
		}
		mask.SetPixels (pixels);
		mask.Apply(true,true);
		WriteTexture(mask,CompressionFormat,assetPath,"Alpha.asset");
	}

	void WriteAlphaTexture(Color[] pixels,Texture2D texture){
		var mask = new Texture2D (texture.width, texture.height, TextureFormat.RGB24, false);
		mask.wrapMode = texture.wrapMode;
		mask.filterMode = texture.filterMode;
		mask.mipMapBias = texture.mipMapBias;
		mask.anisoLevel = texture.anisoLevel;
		var aPixels = new Color[pixels.Length];
		for (int i = 0; i < pixels.Length; i++) {
			var a = pixels [i].a;
			aPixels [i] = new Color (a, a, a);
		}
		mask.SetPixels (aPixels);
		mask.Apply(true,true);
		WriteTexture(mask,CompressionFormat,assetPath,"Alpha.asset");
	}

	static public Color[] PremultipliedAlpha(Color[] pixels){
		Color[] np= new Color[pixels.Length];
		for (int i = 0; i < pixels.Length; i++) {
			var a = pixels [i].a;
			np[i] = new Color (pixels[i].r*a,pixels[i].g*a,pixels[i].b*a,a);
		}
		return np;
	}

	enum BlockMode : byte {
		Untreated,
		Processing,
		Termination
	};
	static readonly int BlockSize=4;
	static public Color[] AlphaBleed(Color[] pixels,int width,int height){
		Color[] np= new Color[height*width];
		int blockHeight= (int)(height-1)/BlockSize+1;
		int blockWidth= (int)(width-1)/BlockSize+1;
		Color[] bc= new Color[blockHeight*blockWidth];
		BlockMode[] bf= new BlockMode[blockHeight*blockWidth];
		int remaining = 0;
		bool exitFlag = true;
		for (var yb = 0; yb < blockHeight; yb++) {
			for (var xb = 0; xb < blockWidth; xb++) {
				float r = 0.0f;
				float g = 0.0f;
				float b = 0.0f;
				float c = 0.0f;
				int n = 0;
				for (var y = 0; y < BlockSize; y++) {
					for (var x = 0; x < BlockSize; x++) {
						int xpos = xb * BlockSize + x;
						int ypos = yb * BlockSize + y;
						if (xpos < width && ypos < height) {
							int pos = ypos * width + xpos;
							float ad = pixels [pos].a;
							r += pixels [pos].r * ad;	
							g += pixels [pos].g * ad;	
							b += pixels [pos].b * ad;
							c += ad;
							if (ad <= 0.02f)
								n++;
						}
					}
				}
				var block = yb * blockWidth + xb;
				if (n > 0) {
					bf [block] = BlockMode.Processing;
					remaining++;
				} else {
					bf [block] = BlockMode.Termination;
				}
				if (c <= 0.02f) {
					bc [ block ] = new Color (0.0f, 0.0f, 0.0f, 0.0f);
					bf [ block ] = BlockMode.Untreated;
				} else {
					bc [ block ] = new Color (r / c, g / c, b / c, c / (float)(BlockSize * BlockSize));
					exitFlag = false;
				}
			}
		}
		if ( exitFlag || remaining==0 )
			return pixels;
		for (var y = 0; y < height; y++) {
			for (var x = 0; x < width; x++) {
				int pos = y * width + x;
				np [pos] = pixels [pos];
			}
		}
		BlockMode[] be= new BlockMode[blockHeight*blockWidth];
		for (int count=16; count > 0 && remaining > 0; count-- ) {
			for (int i=0;i < blockHeight*blockWidth ; i++ )
				be[i] = bf[i];
			for (var yb = 0; yb < blockHeight; yb++) {
				for (var xb = 0; xb < blockWidth; xb++) {
					var block = yb * blockWidth + xb;
					if (be [ block ] == BlockMode.Termination)
						continue;
					float r = 0.0f;
					float g = 0.0f;
					float b = 0.0f;
					float c = 0.0f;
					Color ccol = bc [yb * blockWidth + xb];
					r += (ccol.r * ccol.a * 16.0f);
					g += (ccol.g * ccol.a * 16.0f);
					b += (ccol.b * ccol.a * 16.0f);
					c += (ccol.a * 16.0f);
					int n = 0;
					for (var yp = yb - 1; yp <= yb + 1; yp++) {
						for (var xp = xb - 1; xp <= xb + 1; xp++) {
							var x = ( xp + blockWidth) % blockWidth;
							var y = ( yp + blockHeight) % blockHeight;
							if (be [y * blockWidth + x] != BlockMode.Untreated)
								n++;
							Color col = bc [y * blockWidth + x];
							r += col.r * col.a;
							g += col.g * col.a;
							b += col.b * col.a;
							c += col.a;
						}
					}
					if( n > 0 ) {
						if (c > 0.0f) {
							r /= c; g /= c; b /= c; c /= 24.0f;
						} else {
							r = 0.0f; g = 0.0f; b = 0.0f; c = 0.0f;
						}
						for (var y = 0; y < BlockSize; y++) {
							for (var x = 0; x < BlockSize; x++) {
								int xpos = xb * BlockSize + x;
								int ypos = yb * BlockSize + y;
								if (xpos < width && ypos < height) {
									int pos = ypos * width + xpos;
									if (pixels [pos].a <= 0.02f) {
										float ar = 1.0f - pixels [pos].a;
										np [pos] = new Color (r * ar + pixels [pos].r * (1.0f - ar)
											, g * ar + pixels [pos].g * (1.0f - ar)
											, b * ar + pixels [pos].b * (1.0f - ar)
											, pixels [pos].a);
									} else
										np [pos] = pixels[pos];
								}
							}
						}
						if( be [yb * blockWidth + xb] == BlockMode.Untreated )
							bc [yb * blockWidth + xb] = new Color (r, g, b, c);
						bf [yb * blockWidth + xb] = BlockMode.Termination;
						remaining--;
					}
				}
			}
		}
		return np;
	}

	static public Color[] AlphaBleedOld(Color[] pixels,int width,int height){
		Color[] np= new Color[height*width]; 
		for (var y = 0; y < height; y++) {
			for (var x = 0; x < width; x++) {
				int position=y*width+x;
				if (pixels [position].a <= 0.95f) {
					float ra=0.0f;
					float ga=0.0f;
					float ba=0.0f;
					float ca=0.0f;
					int index=0;
					foreach(var bt in bleedTable){
						float r=0.0f;
						float g=0.0f;
						float b=0.0f;
						float c=0.0f;
						foreach(var pt in bt){
							int xp=x+pt.x;
							int yp=y+pt.y;
							if (xp >= 0 && xp < width && yp >= 0 && yp < height) {
								int pos=yp*width+xp;
								float ad=pixels[pos].a;
								r+=pixels[pos].r*ad;	
								g+=pixels[pos].g*ad;	
								b+=pixels[pos].b*ad;
								c+=ad;
							}
						}
//						float fac=Mathf.Min(1.0f,(float)(8-index)/4.0f);
						float fac=Mathf.Min(1.0f,(float)(10-index)/8.0f);
						ra+=r*fac;
						ga+=g*fac;
						ba+=b*fac;
						ca+=c*fac;
						index++;
					}
					float ar=1.0f-pixels[position].a;
					np[position]=
						new Color( ra/ca*ar+pixels[position].r*pixels[position].a
					    	      ,ga/ca*ar+pixels[position].g*pixels[position].a
					        	  ,ba/ca*ar+pixels[position].b*pixels[position].a
						    	  ,pixels[position].a);

				}else
					np[position]=pixels[position];
			}
		}
		return np;
	}

	const float k1Per256 = 1.0f / 255.0f;
	const float k1Per16 = 1.0f / 15.0f;
	const float k3Per16 = 3.0f / 15.0f;
	const float k5Per16 = 5.0f / 15.0f;
	const float k7Per16 = 7.0f / 15.0f;

	static public Color[] Reduced16bits(Color[] pixels,int texw,int texh){
		Color[] np= new Color[texh*texw];
		var offs = 0;
		for (var y = 0; y < texh; y++) {
			for (var x = 0; x < texw; x++) {
				float a = pixels [offs].a;
				float r = pixels [offs].r;
				float g = pixels [offs].g;
				float b = pixels [offs].b;
				
				var a2 = Mathf.Round(a * 15.0f) * k1Per16;
				var r2 = Mathf.Round(r * 15.0f) * k1Per16;
				var g2 = Mathf.Round(g * 15.0f) * k1Per16;
				var b2 = Mathf.Round(b * 15.0f) * k1Per16;

				np [offs].a = a2;
				np [offs].r = r2;
				np [offs].g = g2;
				np [offs].b = b2;
				offs++;
			}
		}
		return np;
	}

	static public Color[] FloydSteinberg(Color[] pixels,int texw,int texh){
		var offs = 0;
		for (var y = 0; y < texh; y++) {
			for (var x = 0; x < texw; x++) {
				float a = pixels [offs].a;
				float r = pixels [offs].r;
				float g = pixels [offs].g;
				float b = pixels [offs].b;
				
				var a2 = Mathf.Round(a * 15.0f) * k1Per16;
				var r2 = Mathf.Round(r * 15.0f) * k1Per16;
				var g2 = Mathf.Round(g * 15.0f) * k1Per16;
				var b2 = Mathf.Round(b * 15.0f) * k1Per16;
				
				var ae = Mathf.Round((a - a2)*255.0f)*k1Per256;
				var re = Mathf.Round((r - r2)*255.0f)*k1Per256;
				var ge = Mathf.Round((g - g2)*255.0f)*k1Per256;
				var be = Mathf.Round((b - b2)*255.0f)*k1Per256;
				
				pixels [offs].a = a2;
				pixels [offs].r = r2;
				pixels [offs].g = g2;
				pixels [offs].b = b2;
				
				var n1 = offs + 1;
				var n2 = offs + texw - 1;
				var n3 = offs + texw;
				var n4 = offs + texw + 1;
				
				if (x < texw - 1) {
					pixels [n1].a += ae * k7Per16;
					pixels [n1].r += re * k7Per16;
					pixels [n1].g += ge * k7Per16;
					pixels [n1].b += be * k7Per16;
				}
				
				if (y < texh - 1) {
					pixels [n3].a += ae * k5Per16;
					pixels [n3].r += re * k5Per16;
					pixels [n3].g += ge * k5Per16;
					pixels [n3].b += be * k5Per16;
					
					if (x > 0) {
						pixels [n2].a += ae * k3Per16;
						pixels [n2].r += re * k3Per16;
						pixels [n2].g += ge * k3Per16;
						pixels [n2].b += be * k3Per16;
					}
					
					if (x < texw - 1) {
						pixels [n4].a += ae * k1Per16;
						pixels [n4].r += re * k1Per16;
						pixels [n4].g += ge * k1Per16;
						pixels [n4].b += be * k1Per16;
					}
				}
				offs++;
			}
		}
		return pixels;
	}
}
}