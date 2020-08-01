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
			GetWindow<PrefabScratchpadWindow>("Prefab Scratchpad");
		}


		GUIContent btnCntent = new GUIContent();

		List<GameObject>	m_prefabList = new List<GameObject>();
		ReorderableList		m_reorderableList;
		Vector2				m_scrollPosition = new Vector2(0, 0);

		void OnEnable()
		{
			m_reorderableList = new ReorderableList(
				elements: m_prefabList,
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
					m_prefabList.Clear();
				}
			}
			#endregion Horizontal

			#region Drag&Drop Area
			var evt = Event.current;
			var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
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

		void AddPrefab(Object[] objectReferences)
		{
			foreach(var obj in objectReferences) {
				if(PrefabUtility.IsPartOfAnyPrefab(obj)) {
					foreach(var go in m_prefabList) {
						if(go == obj) {
							// 一番先頭にするためリストから抜く
							m_prefabList.Remove(go);
							break;
						}
					}
					m_prefabList.Insert(0, obj as GameObject);
				}
			}
			// ついでにmissiingがあったら削除する
			for(int i = m_prefabList.Count - 1 ; i >= 0 ; --i) {
				if(!m_prefabList[i]) {
					m_prefabList.RemoveAt(i);
				}
			}
		}

		void OnElementCallback(Rect rect, int index, bool isActive, bool isFocused)
		{
			EditorGUI.ObjectField(rect, m_prefabList[index], typeof(GameObject), false);
		}
	}

}

