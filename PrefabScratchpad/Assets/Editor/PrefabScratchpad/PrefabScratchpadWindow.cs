/*
 * Copyright (C) 2020 taichis-K 
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace PrefabScratchpad
{ 
	/// <summary>
	/// Prefabへ素早くアクセスするためのスクラッチパッドウインドウ
	/// </summary>
	public class PrefabScratchpadWindow : EditorWindow
	{
		[MenuItem("Window/Prefab Scratchpad")]
		public static void ShowWindow()
		{
			var window = GetWindow<PrefabScratchpadWindow>("Prefab Scratchpad");

			LoadData(window);
		}

		static PrefabScratchpadData	m_scratchpadData;

		GUIContent btnCntent = new GUIContent();

		ReorderableList		m_reorderableList;
		Vector2				m_scrollPosition = new Vector2(0, 0);

		void OnEnable()
		{
			if(m_scratchpadData == null) {
				LoadData(this);
			}

			m_reorderableList = new ReorderableList(
				elements: m_scratchpadData.m_prefabList,
				elementType: typeof(GameObject),
				draggable: true,
				displayHeader: false,
				displayAddButton: false,
				displayRemoveButton: true
				);
			m_reorderableList.drawElementCallback += OnElementCallback;
		}

		void OnGUI()
		{
			#region Horizontal
			using(var h = new GUILayout.HorizontalScope(GUILayout.Width(this.position.width - 20))) {
				btnCntent.text = "Reset";

				if (GUILayout.Button(btnCntent, GUI.skin.button, GUILayout.Height(20)))
				{
					m_scratchpadData.m_prefabList.Clear();
					EditorUtility.SetDirty(m_scratchpadData);
				}
			}
			#endregion Horizontal

			#region Drag&Drop Area
			var evt = Event.current;
			var dropArea = GUILayoutUtility.GetRect(0.0f, 25.0f, GUILayout.ExpandWidth(true));
			GUI.Box(dropArea, "Drag & Drop");
			switch (evt.type) {
			case EventType.DragUpdated:
			case EventType.DragPerform: 
				{
					if (!dropArea.Contains(evt.mousePosition)) {
						break;
					}

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						AddPrefab(DragAndDrop.objectReferences);
						DeleteMissingPrefab();

						DragAndDrop.activeControlID = 0;
					}
					Event.current.Use();
				}
				break;
			}
			#endregion

			#region Prefab List
			using (var scrollView = new EditorGUILayout.ScrollViewScope(m_scrollPosition)) {
				m_scrollPosition = scrollView.scrollPosition;
				m_reorderableList.DoLayoutList();
			}
			#endregion
		}

		static void AddPrefab(Object[] objectReferences)
		{
			foreach(var obj in objectReferences) {
				if(PrefabUtility.IsPartOfAnyPrefab(obj)) {
					var goObj = obj as GameObject;
					if(!string.IsNullOrEmpty(goObj.scene.name)) {
						// SceneにあるPrefabならソースにする
						goObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(goObj);
					}
					foreach(var go in m_scratchpadData.m_prefabList) {
						if(go == goObj) {
							// 一番先頭にするためリストから抜く
							m_scratchpadData.m_prefabList.Remove(go);
							break;
						}
					}
					m_scratchpadData.m_prefabList.Insert(0, goObj);
					EditorUtility.SetDirty(m_scratchpadData);
				}
			}
		}

		static void DeleteMissingPrefab()
		{
			bool	removed = false;
			for(int i = m_scratchpadData.m_prefabList.Count - 1 ; i >= 0 ; --i) {
				if(!m_scratchpadData.m_prefabList[i]) {
					m_scratchpadData.m_prefabList.RemoveAt(i);
					removed = true;
				}
			}
			if(removed) {
				EditorUtility.SetDirty(m_scratchpadData);
			}
		}

		static void LoadData(PrefabScratchpadWindow window)
		{
			// このcsファイルのあるディレクトリを基準にデータパスを作る
			var mono = MonoScript.FromScriptableObject(window);
			var scriptPath = AssetDatabase.GetAssetPath(mono);
			var basePath = Path.GetDirectoryName(scriptPath).Replace('\\', '/');
			var path = basePath + "/PrefabScratchpadData.asset";

			m_scratchpadData = AssetDatabase.LoadAssetAtPath<PrefabScratchpadData>(path);
			if(m_scratchpadData != null) {
				DeleteMissingPrefab();
			}

			if(m_scratchpadData == null) {
				m_scratchpadData = ScriptableObject.CreateInstance<PrefabScratchpadData>();
				AssetDatabase.CreateAsset(m_scratchpadData, path);
			}
		}

		void OnElementCallback(Rect rect, int index, bool isActive, bool isFocused)
		{
			if(m_scratchpadData == null) {
				return;
			}
			EditorGUI.ObjectField(rect, m_scratchpadData.m_prefabList[index], typeof(GameObject), false);
		}
	}

}

