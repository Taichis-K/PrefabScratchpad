/*
 * Copyright (C) 2020 taichis-K 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabScratchpad
{
	/// <summary>
	/// PrefabScratchpadの保存データ
	/// </summary>
	public class PrefabScratchpadData : ScriptableObject
	{
		/// <summary>
		/// Prefabオブジェクトリスト
		/// </summary>
		public List<GameObject>	m_prefabList = new List<GameObject>();
	}
}

