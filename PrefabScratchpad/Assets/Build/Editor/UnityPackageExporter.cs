using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class UnityPackageExporter
{
	class ExportData
	{
		public string	packageName;
		public string	packagePath;
		public List<string>	assets = new List<string>();
	}

	[MenuItem("Export/UnityPackage: PrefabScratchpad", false, 1)]
	public static void Export()
	{
		var exportRootPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "UnityPackages");
		if(!Directory.Exists(exportRootPath)) {
			Directory.CreateDirectory(exportRootPath);
		}

		var exportData = GetExportData(Application.dataPath+"/Editor/PrefabScratchpad", exportRootPath, "PrefabScratchpad");

		EditorUtility.DisplayCancelableProgressBar("Export Unity Package", exportData.packageName, 0f);
		AssetDatabase.ExportPackage(exportData.assets.ToArray(), exportData.packagePath);

		EditorUtility.ClearProgressBar();
		EditorUtility.DisplayDialog("Export UnityPackages", "Complete", "OK");

		// 保存先フォルダを開く
		System.Diagnostics.Process.Start(exportRootPath);
	}

	static ExportData GetExportData(string dir, string exportRootPath, string packageName)
	{
		var data = new ExportData();
		data.packageName = packageName;
		data.packagePath = Path.Combine(exportRootPath, packageName+".unitypackage");
		GetExportDataRecursive(data, dir);
		return data;
	}

	static void GetExportDataRecursive(ExportData data, string dir)
	{
		var files = Directory.GetFiles(dir);
		foreach(var filename in files) {
			// ScriptableObjectのインスタンスとそのmetaファイルを除外
			if(filename.Contains(".asset")) {
				continue;
			}
			var inputFilePath = "Assets" + filename.Substring(Application.dataPath.Length);
			data.assets.Add(inputFilePath);
		}

		var dirs = Directory.GetDirectories(dir);
		foreach(var d in dirs) {
			GetExportDataRecursive(data, d);
		}
	}

}
