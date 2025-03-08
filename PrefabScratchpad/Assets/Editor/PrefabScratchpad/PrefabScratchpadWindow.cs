/*
 * Copyright (C) 2020 taichis-K 
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;

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

		// 検索バー用の文字列
		string m_searchText = "";
		// SearchFieldクラスのインスタンス（標準の検索バー風に描画）
		SearchField m_searchField;

		void OnEnable()
		{
			if(m_scratchpadData == null) {
				LoadData(this);
			}

			// SearchFieldの初期化
			m_searchField = new SearchField();

			m_reorderableList = new ReorderableList(
				elements: m_scratchpadData.m_prefabList,
				elementType: typeof(GameObject),
				draggable: true,
				displayHeader: false,
				displayAddButton: false,
				displayRemoveButton: true
				);
			m_reorderableList.drawElementCallback += OnElementCallback;
			// 削除ボタンの処理（全件表示時）
			m_reorderableList.onRemoveCallback += (ReorderableList list) => {
				m_scratchpadData.m_prefabList.RemoveAt(list.index);
				EditorUtility.SetDirty(m_scratchpadData);
			};
			// ドラッグ後の順序変更時（全件表示時）は特に追加処理は不要
			m_reorderableList.onReorderCallback += (ReorderableList list) => {
				EditorUtility.SetDirty(m_scratchpadData);
			};
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

			#region Search Bar（標準の検索バー風）
			EditorGUILayout.Space();
			// SearchField の OnGUI を使うと、標準と同じ見た目・クリアボタンが自動で描画される
			m_searchText = m_searchField.OnGUI(m_searchText);
			EditorGUILayout.Space();
			#endregion

			#region Prefab List
			using(var scrollView = new EditorGUILayout.ScrollViewScope(m_scrollPosition)) {
				m_scrollPosition = scrollView.scrollPosition;
				// 検索文字列が空なら既存のReorderableListで全件表示
				if(string.IsNullOrEmpty(m_searchText)) {
					m_reorderableList.DoLayoutList();
				}
				else {
					// 元リストから検索条件に一致するPrefabとその元リスト上のインデックスを取得
					List<int> filteredIndices = new List<int>();
					List<GameObject> filteredList = new List<GameObject>();
					for(int i = 0; i < m_scratchpadData.m_prefabList.Count; i++) {
						GameObject go = m_scratchpadData.m_prefabList[i];
						if(go != null && go.name.IndexOf(m_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0) {
							filteredIndices.Add(i);
							filteredList.Add(go);
						}
					}

					// 検索結果が0件の場合
					if(filteredList.Count == 0) {
						EditorGUILayout.LabelField("No matching prefabs found.");
					}
					else {
						// 一時的なReorderableListを生成（検索条件に一致するPrefabのみを表示）
						ReorderableList filteredReorderableList = new ReorderableList(
							elements: filteredList,
							elementType: typeof(GameObject),
							draggable: true,
							displayHeader: false,
							displayAddButton: false,
							displayRemoveButton: true
						);
						// 各要素の描画処理
						filteredReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
							if(index < filteredList.Count) {
								EditorGUI.ObjectField(rect, filteredList[index], typeof(GameObject), false);
							}
						};
						// 削除ボタン処理：元のリストから対象Prefabを削除
						filteredReorderableList.onRemoveCallback = (ReorderableList list) => {
							int index = list.index;
							if(index >= 0 && index < filteredList.Count) {
								int origIndex = filteredIndices[index];
								// GUIレイアウト処理が終了してから削除を行う
								EditorApplication.delayCall += () => {
									m_scratchpadData.m_prefabList.RemoveAt(origIndex);
									EditorUtility.SetDirty(m_scratchpadData);
								};
							}
						};
						// ドラッグ＆ドロップで順序変更時の処理
						filteredReorderableList.onReorderCallback = (ReorderableList list) => {
							List<int> sortedIndices = new List<int>(filteredIndices);
							sortedIndices.Sort();
							for(int j = 0; j < sortedIndices.Count && j < filteredList.Count; j++) {
								m_scratchpadData.m_prefabList[sortedIndices[j]] = filteredList[j];
							}
							EditorUtility.SetDirty(m_scratchpadData);
						};

						filteredReorderableList.DoLayoutList();
					}
				}
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

