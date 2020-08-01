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
		public string FolderName { get { return Path.GetFileName(InputFilePath); } }
		public string InputFilePath;
		public string OutputFilePath;
	}

	[MenuItem("Export/UnityPackage", false, 1)]
	public static void Export()
	{
		var exportRootPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "UnityPackages");
		if(!Directory.Exists(exportRootPath)) {
			Directory.CreateDirectory(exportRootPath);
		}

		var exportDatas = Directory.GetDirectories(Application.dataPath + "/Editor", "*")
		.Select(dir => {
			var inputFilePath = "Assets" + dir.Substring(Application.dataPath.Length);
			var outputFilePath = Path.Combine(exportRootPath, Path.GetFileName(inputFilePath) + ".unitypackage");
			return new ExportData() { InputFilePath = inputFilePath, OutputFilePath = outputFilePath };
		})
		.ToList();

		foreach(var data in exportDatas) {
			EditorUtility.DisplayProgressBar("Export Unity Packages", data.FolderName, 0f);
			AssetDatabase.ExportPackage(data.InputFilePath, data.OutputFilePath, ExportPackageOptions.Recurse);
		}

		EditorUtility.ClearProgressBar();
		EditorUtility.DisplayDialog("Export UnityPackages", "Complete", "OK");

		// 保存先フォルダを開く
		System.Diagnostics.Process.Start(exportRootPath);
	}
}
