using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Prefabを配置しやすくするエディタ拡張
/// </summary>
public class PrefabBuilderWindow : EditorWindow
{
	/// <summary>
	/// ウィンドウ
	/// </summary>
	public static PrefabBuilderWindow window { get; set; }

	/// <summary>
	/// ウィンドウの左右の幅
	/// </summary>
	private const float WindowOffset = 6.0f;
	/// <summary>
	/// ウィンドウの最小幅（横）
	/// </summary>
	private const float MinWindowWidth = 445.0f;
	/// <summary>
	/// ウィンドウの最小幅（縦）
	/// </summary>
	private const float MaxWindowHeight = 200.0f;
	/// <summary>
	/// レイを飛ばす距離
	/// </summary>
	private const float RayLength = 1000.0f;
	/// <summary>
	/// テクスチャ同士の間隔
	/// </summary>
	private const float TextureOffset = 10.0f;
	/// <summary>
	/// ハンドルの大きさ
	/// </summary>
	private const float HandleSize = 0.08f;

	/// <summary>
	/// 検索バーの文字列
	/// </summary>
	private string searchString = "";
	/// <summary>
	/// プレハブリストを表示するスクロールの座標
	/// </summary>
	private Vector2 prefabScrollPos = new Vector2(0.0f, 0.0f);
	/// <summary>
	/// 開始位置
	/// </summary>
	private Vector3 startPos = new Vector3(0.0f, 0.0f, 0.0f);
	/// <summary>
	/// 終了位置
	/// </summary>
	private Vector3 endPos = new Vector3(2.0f, 0.0f, 0.0f);
	/// <summary>
	/// 配置のプレビュー用のゲームオブジェクト
	/// </summary>
	private GameObject tmpObj;
	/// <summary>
	/// 親オブジェクトを選択
	/// </summary>
	private Transform parentTransform;

	private Vector3 areaCenter = Vector3.zero;
	private Vector3 areaSize = Vector3.one;

	private List<Object> prefabObjectList = new List<Object>();
	private float windowHalfSize = 100.0f;
	private float textureWidth = 100.0f;
	private float prefabListHeight;

	//Scale設定関連
	private bool isRandomScale = false;
	private float selectScaleMin = 1.0f;
	private float selectScaleMax = 2.0f;
	private float selectScale = 1.0f;

	//Rotate設定関連
	private bool isRandomRotate = false;
	private float selectRotateMin = 0.0f;
	private float selectRotateMax = 360.0f;
	private float selectRotate = 0.0f;

	private float selectInfoGrid = 0.0f;
	public float selectInfoOffset = 0.0f;

	
	private int createNum = 0;
	private int selectIdx = 0;
	private int selectBrushMode = 0;

	/// <summary>
	/// 作成するかどうか
	/// </summary>
	private bool isCreateMode = false;

	/// <summary>
	/// 選択している添え字
	/// </summary>
	private int SelectIdx
	{
        get { return selectIdx; }
		set
		{
			selectIdx = value;
			CheckTmpObject();
		}
	}

	/// <summary>
	/// 仮のオブジェクトのScale
	/// </summary>
	private float SelectTmpScale
	{
		get
		{
			if (isRandomScale)
			{
				return (selectScaleMin + selectScaleMax) / 2.0f;
			}
			else
			{
				return selectScale;
			}
		}
	}

	/// <summary>
	/// 仮のオブジェクトのRotation
	/// </summary>
	private float SelectTmpRotation
	{
		get
		{
			if (isRandomRotate)
			{
				return (selectRotateMin + selectRotateMax) / 2.0f;
			}
			else
			{
				return selectRotate;
			}
		}
	}

	/// <summary>
	/// Scaleの値
	/// </summary>
	private float SelectScale
	{
		get
		{
			if (isRandomScale)
			{
				return Random.Range(selectScaleMin, selectScaleMax);
			}
			else
			{
				return selectScale;
			}
		}
	}

	/// <summary>
	/// Rotate
	/// </summary>
	private float SelectRotate
	{
		get
		{
			if (isRandomRotate)
			{
				return Random.Range(selectRotateMin, selectRotateMax);
			}
			else
			{
				return selectRotate;
			}
		}
	}

	/// <summary>
	/// オブジェクトリスト
	/// </summary>
	private List<Object> searchObjectList
	{
		get
		{
			List<Object> tmp = new List<Object>();
			for (int i = 0; i < prefabObjectList.Count; i++)
			{
				if (string.Compare(((GameObject)prefabObjectList[i]).name, searchString) <= 1)
					tmp.Add(prefabObjectList[i]);
			}
			tmp.Sort();

			return tmp;
		}
	}

	/// <summary>
	/// ウィンドウの横幅
	/// </summary>
	private float windowWidth
	{
		get { return window.position.width; }
	}
	/// <summary>
	/// 左側の幅
	/// </summary>
	private float windowLeftWidth
	{
		get { return 180.0f - WindowOffset; }
	}
	/// <summary>
	/// 右側の幅
	/// </summary>
	private float windowRightWidth
	{
		get { return windowWidth - windowLeftWidth - 80.0f; }
	}
	/// <summary>
	/// ウィンドウの縦幅
	/// </summary>
	private float windowHeight
	{
		get { return window.position.height; }
	}
	/// <summary>
	/// ドラッグするエリアの縦幅
	/// </summary>
	private float dragAreaHeight
	{
		get { return 200.0f; }
	}
	/// <summary>
	/// 範囲を表示する色
	/// </summary>
	private Color areaColor
	{
		get
		{
			return new Color(0.0f, 0.0f, 0.5f, 0.1f);
		}
	}
	/// <summary>
	/// 範囲を表示する色
	/// </summary>
	private Color areaLineColor
	{
		get
		{
			return new Color(0.0f, 0.0f, 0.5f, 1.0f);
		}
	}

	[MenuItem("Tools/PrefabBuilder")]
	private static void Open()
	{
		window = GetWindow<PrefabBuilderWindow>("PrefabBuilder");
		window.windowHalfSize = window.position.width / 2.0f;
		window.minSize = new Vector2(MinWindowWidth, MaxWindowHeight);
	}

	private void OnEnable()
	{

	}

	/// <summary>
	/// 描画処理
	/// </summary>
	private void OnGUI()
	{
		if (window == null)
			return;

		EditorGUILayout.BeginHorizontal();
		DrawLeftAreaGUI();
		DrawRightAreaGUI();
		EditorGUILayout.EndHorizontal();
		Repaint();
	}

	/// <summary>
	/// フォーカスされたとき
	/// </summary>
	void OnFocus()
	{
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
		SceneView.onSceneGUIDelegate += this.OnSceneGUI;
	}

	void OnDestroy()
	{
		isCreateMode = false;
		CheckTmpObject();
		SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
	}

	/// <summary>
	/// シーン
	/// </summary>
	/// <param name="sceneView"></param>
	void OnSceneGUI(SceneView sceneView)
	{
		if (selectBrushMode == 1)
		{
			//端から端を動かせるようにする
			Handles.color = Color.blue;
			startPos = Handles.FreeMoveHandle(startPos, Quaternion.identity, HandleUtility.GetHandleSize(startPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			endPos = Handles.FreeMoveHandle(endPos, Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			Handles.DrawLine(startPos, endPos);

			//途中の配置する座標を描画
			var lerp = 1.0f / (createNum + 1);
			for (int i = 0; i < createNum; i++)
			{
				var pos = Vector3.Lerp(startPos, endPos, (i * lerp) + lerp);
				Handles.SphereHandleCap(0, pos, Quaternion.identity, HandleUtility.GetHandleSize(pos) * HandleSize, EventType.Repaint);
			}
		}
		else if(selectBrushMode == 2)
		{
			Handles.color = Color.blue;
			Handles.DrawWireCube(areaCenter, areaSize);
			//var right =  (areaSize.x / 2.0f);
			//var top =  (areaSize.y / 2.0f);
			//var forward =  (areaSize.z / 2.0f);
			//var rightPos = Handles.FreeMoveHandle(areaCenter + new Vector3(right, 0.0f, 0.0f), Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			//var leftPos = Handles.FreeMoveHandle(areaCenter + new Vector3(-right, 0.0f, 0.0f), Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			//var topPos = Handles.FreeMoveHandle(areaCenter + new Vector3(0.0f, top, 0.0f), Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			//var bottomPos = Handles.FreeMoveHandle(areaCenter + new Vector3(0.0f, -top, 0.0f), Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			//var forwardPos = Handles.FreeMoveHandle(areaCenter + new Vector3(0.0f, 0.0f, forward), Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			//var backPos = Handles.FreeMoveHandle(areaCenter + new Vector3(0.0f, 0.0f, -forward), Quaternion.identity, HandleUtility.GetHandleSize(endPos) * HandleSize, Vector3.one, Handles.CubeHandleCap);
			//areaCenter = new Vector3((rightPos.x + leftPos.x) / 2.0f, (topPos.y + bottomPos.y) / 2.0f, (forwardPos.z + backPos.z) / 2.0f);

		}

		Handles.BeginGUI();

		if (isCreateMode && tmpObj && prefabObjectList[SelectIdx] != null)
		{
			if (Event.current.type == EventType.MouseDown && Event.current.isMouse && Event.current.button == 0)
			{
				//クリックしたとき
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit = new RaycastHit();
				if (Physics.Raycast(ray, out hit, RayLength))
				{
					var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabObjectList[SelectIdx]);
					Undo.RegisterCreatedObjectUndo(obj, "CreatePrefab");
					obj.transform.SetParent(parentTransform);
					obj.transform.position = tmpObj.transform.position;
					var rot = tmpObj.transform.eulerAngles;
                    obj.transform.localRotation = Quaternion.Euler(rot.x, SelectRotate, rot.z);
					var scale = SelectScale;
                    obj.transform.localScale = new Vector3(scale, scale, scale);
				}
			}
			else
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit h = new RaycastHit();
				if (Physics.Raycast(ray, out h, RayLength))
				{
					Vector3 pos = GridToPosition(ray.GetPoint(h.distance));
					tmpObj.transform.position = pos;
					AlignGhostToSurface(tmpObj.transform, h.normal);
				}
			}
		}


		Handles.EndGUI();
	}

	private void DrawLeftAreaGUI()
	{
		EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(windowLeftWidth), GUILayout.Height(windowHeight - WindowOffset));
		{
			SelectPrefabInfo();
			switch (selectBrushMode)
			{
				case 0: ClickToCreateToolGUI();	break;
				case 1: DeplyToolGUI();			break;
				case 2: AreaGUI();				break;
			}
		}
		EditorGUILayout.EndVertical();
	}

	private void DrawRightAreaGUI()
	{
		EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(windowRightWidth), GUILayout.Height(windowHeight - WindowOffset));
		{
			SearchBarGUI();
			SaveButtonGUI();
			PrefabListGUI();
			DragAreaGUI();
		}
		EditorGUILayout.EndVertical();
	}

	/// <summary>
	/// 選択したプレハブの情報を表示する
	/// </summary>
	private void SelectPrefabInfo()
	{
		EditorGUILayout.LabelField("PrefabInfo");
		if (prefabObjectList.Count > 0)
		{
			EditorGUILayout.LabelField(prefabObjectList[SelectIdx].name);
			var selectPrefabTexRect = GUILayoutUtility.GetRect(windowLeftWidth, windowLeftWidth,windowLeftWidth,windowLeftWidth);
			GUI.DrawTexture(selectPrefabTexRect, (Texture)AssetPreview.GetAssetPreview(prefabObjectList[SelectIdx]), ScaleMode.ScaleToFit);

			EditorGUILayout.Space();
			parentTransform = (Transform)EditorGUILayout.ObjectField("ParentObj", parentTransform, typeof(Transform), true);
			EditorGUI.BeginChangeCheck();

			//ブラシができるように
			string[] brushModeStr = new string[] { "Brush", "Line", "Area" };
			selectBrushMode = GUILayout.Toolbar(selectBrushMode, brushModeStr);
			EditorGUILayout.Space();
		}
	}

	private void ClickToCreateToolGUI()
	{
		if (prefabObjectList.Count > 0)
		{
			var isChange = EditorGUI.EndChangeCheck();
			if (isChange)
				CheckTmpObject();

			float checkBoxField = 15.0f;
			float labelField = 40.0f;
			float valueField = 35.0f;

			EditorGUILayout.Space();

			//Scale
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Scale", GUILayout.Width(labelField));
			isRandomScale = GUILayout.Toggle(isRandomScale,"", GUILayout.Width(checkBoxField));
			if (isRandomScale)
			{
				selectScaleMin = Mathf.Clamp(EditorGUILayout.FloatField(selectScaleMin, GUILayout.Width(40)), 0.0f, selectScaleMax);
				EditorGUILayout.MinMaxSlider(ref selectScaleMin, ref selectScaleMax, 0.0f, 10.0f);
				selectScaleMax = Mathf.Clamp(EditorGUILayout.FloatField(selectScaleMax, GUILayout.Width(40)), selectScaleMin, 10.0f);
			}
			else
			{
				selectScale = GUILayout.HorizontalSlider(selectScale, 0.0f, 10.0f);
				selectScale = EditorGUILayout.FloatField(selectScale, GUILayout.Width(valueField));
			}
			EditorGUILayout.EndHorizontal();

			//Rotate
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Rotate", GUILayout.Width(labelField));
			isRandomRotate = GUILayout.Toggle(isRandomRotate, "", GUILayout.Width(checkBoxField));
			if (isRandomRotate)
			{
				selectRotateMin = Mathf.Clamp(EditorGUILayout.FloatField(selectRotateMin, GUILayout.Width(40)), 0.0f, selectRotateMax);
				EditorGUILayout.MinMaxSlider(ref selectRotateMin, ref selectRotateMax, 0.0f, 360.0f);
				selectRotateMax = Mathf.Clamp(EditorGUILayout.FloatField(selectRotateMax, GUILayout.Width(40)), selectRotateMin, 360.0f);
			}
			else
			{
				selectRotate = GUILayout.HorizontalSlider(selectRotate, 0.0f, 360.0f);
				selectRotate = EditorGUILayout.FloatField(selectRotate, GUILayout.Width(valueField));
			}
			EditorGUILayout.EndHorizontal();
			var isChangeTransform = EditorGUI.EndChangeCheck();

			//Grid
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Grid", GUILayout.Width(labelField));
			selectInfoGrid = GUILayout.HorizontalSlider(selectInfoGrid, 0.0f, 10.0f);
			selectInfoGrid = EditorGUILayout.FloatField(selectInfoGrid, GUILayout.Width(valueField));
			EditorGUILayout.EndHorizontal();


			//Offset
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Offset", GUILayout.Width(labelField));
			selectInfoOffset = Mathf.Clamp(GUILayout.HorizontalSlider(selectInfoOffset, 0.0f, selectInfoGrid), 0.0f, selectInfoGrid);
			selectInfoOffset = EditorGUILayout.FloatField(selectInfoOffset, GUILayout.Width(valueField));
			EditorGUILayout.EndHorizontal();


			EditorGUILayout.Space();
			isCreateMode = GUILayout.Toggle(isCreateMode, isCreateMode ? "On" : "Off", "button");


			//Scale、Rotateを変更したとき
			if (isChangeTransform && tmpObj != null)
			{
				var scale = SelectTmpScale;
				var rot = SelectTmpRotation;
				tmpObj.transform.localRotation = Quaternion.Euler(0.0f, rot, 0.0f);
				tmpObj.transform.localScale = new Vector3(scale, scale, scale);
			}
		}
	}

	/// <summary>
	/// 一列整列するツール
	/// </summary>
	private void DeplyToolGUI()
	{
		float labelField = 40.0f;
		float valueField = 40.0f;
		float checkBoxField = 15.0f;

		EditorGUILayout.Space();
		EditorGUI.BeginChangeCheck();

		//Scale
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Scale", GUILayout.Width(labelField));
		isRandomScale = GUILayout.Toggle(isRandomScale, "", GUILayout.Width(checkBoxField));
		if (isRandomScale)
		{
			selectScaleMin = Mathf.Clamp(EditorGUILayout.FloatField(selectScaleMin, GUILayout.Width(40)), 0.0f, selectScaleMax);
			EditorGUILayout.MinMaxSlider(ref selectScaleMin, ref selectScaleMax, 0.0f, 10.0f);
			selectScaleMax = Mathf.Clamp(EditorGUILayout.FloatField(selectScaleMax, GUILayout.Width(40)), selectScaleMin, 10.0f);
		}
		else
		{
			selectScale = GUILayout.HorizontalSlider(selectScale, 0.0f, 10.0f);
			selectScale = EditorGUILayout.FloatField(selectScale, GUILayout.Width(valueField));
		}
		EditorGUILayout.EndHorizontal();


		//Rotate
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Rotate", GUILayout.Width(labelField));
		isRandomRotate = GUILayout.Toggle(isRandomRotate, "", GUILayout.Width(checkBoxField));
		if (isRandomRotate)
		{
			selectRotateMin = Mathf.Clamp(EditorGUILayout.FloatField(selectRotateMin, GUILayout.Width(40)), 0.0f, selectRotateMax);
			EditorGUILayout.MinMaxSlider(ref selectRotateMin, ref selectRotateMax, 0.0f, 360.0f);
			selectRotateMax = Mathf.Clamp(EditorGUILayout.FloatField(selectRotateMax, GUILayout.Width(40)), selectRotateMin, 360.0f);
		}
		else
		{
			selectRotate = GUILayout.HorizontalSlider(selectRotate, 0.0f, 360.0f);
			selectRotate = EditorGUILayout.FloatField(selectRotate, GUILayout.Width(valueField));
		}
		EditorGUILayout.EndHorizontal();
		var isChangeTransform = EditorGUI.EndChangeCheck();

		//StartPos EndPos
		var floatFieldWidth = 35.0f;
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Line Start Pos");
		startPos.x = EditorGUILayout.FloatField(startPos.x, GUILayout.Width(floatFieldWidth));
		startPos.y = EditorGUILayout.FloatField(startPos.y, GUILayout.Width(floatFieldWidth));
		startPos.z = EditorGUILayout.FloatField(startPos.z, GUILayout.Width(floatFieldWidth));
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Line End Pos");
		endPos.x = EditorGUILayout.FloatField(endPos.x, GUILayout.Width(floatFieldWidth));
		endPos.y = EditorGUILayout.FloatField(endPos.y, GUILayout.Width(floatFieldWidth));
		endPos.z = EditorGUILayout.FloatField(endPos.z, GUILayout.Width(floatFieldWidth));
		EditorGUILayout.EndHorizontal();


		EditorGUILayout.Space();

		//作成するオブジェクトの数
		createNum = Mathf.Clamp(EditorGUILayout.IntField("CreateNum", createNum), 0, int.MaxValue);
		var lerp = 1.0f / (createNum + 1);
		if (GUILayout.Button("CreatePrefab"))
		{
			for(int i = 0; i < createNum; i++)
			{
				var pos = Vector3.Lerp(startPos, endPos, (i * lerp) + lerp);
				var scale = SelectScale;
				var rot = SelectRotate;
				var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabObjectList[SelectIdx]);
				obj.transform.localScale = new Vector3(scale, scale, scale);
				obj.transform.rotation = Quaternion.Euler(0.0f, rot, 0.0f);
				Undo.RegisterCreatedObjectUndo(obj, "CreatePrefab");
				obj.transform.position = pos;
				if (parentTransform != null)
					obj.transform.SetParent(parentTransform);
			}
		}
	}

	private void AreaGUI()
	{
		float labelField = 40.0f;
		float valueField = 40.0f;
		float checkBoxField = 15.0f;

		EditorGUILayout.Space();

		//Scale
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Scale", GUILayout.Width(labelField));
		isRandomScale = GUILayout.Toggle(isRandomScale, "", GUILayout.Width(checkBoxField));
		if (isRandomScale)
		{
			selectScaleMin = Mathf.Clamp(EditorGUILayout.FloatField(selectScaleMin, GUILayout.Width(40)), 0.0f, selectScaleMax);
			EditorGUILayout.MinMaxSlider(ref selectScaleMin, ref selectScaleMax, 0.0f, 10.0f);
			selectScaleMax = Mathf.Clamp(EditorGUILayout.FloatField(selectScaleMax, GUILayout.Width(40)), selectScaleMin, 10.0f);
		}
		else
		{
			selectScale = GUILayout.HorizontalSlider(selectScale, 0.0f, 10.0f);
			selectScale = EditorGUILayout.FloatField(selectScale, GUILayout.Width(valueField));
		}
		EditorGUILayout.EndHorizontal();


		//Rotate
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Rotate", GUILayout.Width(labelField));
		isRandomRotate = GUILayout.Toggle(isRandomRotate, "", GUILayout.Width(checkBoxField));
		if (isRandomRotate)
		{
			selectRotateMin = Mathf.Clamp(EditorGUILayout.FloatField(selectRotateMin, GUILayout.Width(40)), 0.0f, selectRotateMax);
			EditorGUILayout.MinMaxSlider(ref selectRotateMin, ref selectRotateMax, 0.0f, 360.0f);
			selectRotateMax = Mathf.Clamp(EditorGUILayout.FloatField(selectRotateMax, GUILayout.Width(40)), selectRotateMin, 360.0f);
		}
		else
		{
			selectRotate = GUILayout.HorizontalSlider(selectRotate, 0.0f, 360.0f);
			selectRotate = EditorGUILayout.FloatField(selectRotate, GUILayout.Width(valueField));
		}
		EditorGUILayout.EndHorizontal();

		//StartPos EndPos
		var floatFieldWidth = 35.0f;
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Area Center");
		areaCenter.x = EditorGUILayout.FloatField(areaCenter.x, GUILayout.Width(floatFieldWidth));
		areaCenter.y = EditorGUILayout.FloatField(areaCenter.y, GUILayout.Width(floatFieldWidth));
		areaCenter.z = EditorGUILayout.FloatField(areaCenter.z, GUILayout.Width(floatFieldWidth));
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Area Size");
		areaSize.x = EditorGUILayout.FloatField(areaSize.x, GUILayout.Width(floatFieldWidth));
		areaSize.y = EditorGUILayout.FloatField(areaSize.y, GUILayout.Width(floatFieldWidth));
		areaSize.z = EditorGUILayout.FloatField(areaSize.z, GUILayout.Width(floatFieldWidth));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		createNum = Mathf.Clamp(EditorGUILayout.IntField("CreateNum", createNum), 0, int.MaxValue);

		if (GUILayout.Button("CreatePrefab"))
		{
			for (int i = 0; i < createNum; i++)
			{
				var pos = new Vector3(
					Random.Range(areaCenter.x + (areaSize.x / 2.0f), areaCenter.x - (areaSize.x / 2.0f)),
					Random.Range(areaCenter.y + (areaSize.y / 2.0f), areaCenter.y - (areaSize.y / 2.0f)),
					Random.Range(areaCenter.z + (areaSize.z / 2.0f), areaCenter.z - (areaSize.z / 2.0f)));
				var scale = SelectScale;
				var rot = SelectRotate;
				var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabObjectList[SelectIdx]);
				obj.transform.localScale = new Vector3(scale, scale, scale);
				obj.transform.rotation = Quaternion.Euler(0.0f, rot, 0.0f);
				Undo.RegisterCreatedObjectUndo(obj, "CreatePrefab");
				obj.transform.position = pos;
				if (parentTransform != null)
					obj.transform.SetParent(parentTransform);
			}
		}
	}

	/// <summary>
	/// 検索バーを描画する
	/// </summary>
	private void SearchBarGUI()
	{
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;
		GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
		searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"));
		if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
		{
			//キャンセルボタンを押したとき
			searchString = "";
			GUI.FocusControl(null);
		}
		GUILayout.EndHorizontal();
	}

	/// <summary>
	/// プレハブリストを保存する
	/// </summary>
	private void SaveButtonGUI()
	{
		EditorGUILayout.BeginHorizontal();
		if(GUILayout.Button("Add prefab folder"))
		{
			foreach (Object obj in Selection.GetFiltered(typeof(DefaultAsset), SelectionMode.DeepAssets))
			{
				if (obj is DefaultAsset)
				{
					string path = AssetDatabase.GetAssetPath(obj);
					if (AssetDatabase.IsValidFolder(path))
					{
						string[] fileEntries = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
						for (int i = 0; i < fileEntries.Length; i++)
						{
							var filePath = fileEntries[i];
							filePath = ConvertSystemPathToUnityPath(filePath);
							var file = AssetDatabase.LoadAssetAtPath(filePath, typeof(object));
							if (file != null)
							{
								if (file.GetType() != typeof(GameObject))
									continue;
								GameObject o = (GameObject)file;
								AddPrefabObj(o);
							}
						}

					}
				}
			}
		}
		if(GUILayout.Button("Remove all prefab list"))
		{
			prefabObjectList.Clear();
		}
		EditorGUILayout.EndHorizontal();
	}

	/// <summary>
	/// プレハブリストを描画する
	/// </summary>
	private void PrefabListGUI()
	{
		EditorGUILayout.BeginVertical();
		textureWidth = EditorGUILayout.Slider("TextureWidth ",textureWidth, 30.0f, 70.0f);

		prefabScrollPos = EditorGUILayout.BeginScrollView(prefabScrollPos, GUI.skin.box, GUILayout.Height(windowHeight - dragAreaHeight));
		{
			Event evt = Event.current;
			int widthDisplayCount = Mathf.FloorToInt(windowRightWidth / (textureWidth+TextureOffset));
			int heightDisplayCount = Mathf.CeilToInt((float)prefabObjectList.Count / (float)widthDisplayCount);
			prefabListHeight = (heightDisplayCount * textureWidth) + (heightDisplayCount * TextureOffset);
			Rect defaultRect = GUILayoutUtility.GetRect(textureWidth, textureWidth);

			for (int i = 0; i < heightDisplayCount; i++)
			{
				EditorGUILayout.BeginHorizontal();
				for (int j = 0; j < widthDisplayCount; j++)
				{
					EditorGUILayout.BeginVertical();
					int idx = (i * widthDisplayCount) + j;
					if (prefabObjectList.Count > idx && prefabObjectList[idx] != null)
					{
						if (idx == SelectIdx)
							GUI.color = new Color(0.8f, 0.8f, 1.0f);
						else
							GUI.color = Color.white;

						Rect drawTextureRect = new Rect(defaultRect);
						drawTextureRect.x = defaultRect.x + (j * textureWidth) + (j * TextureOffset);
						drawTextureRect.y = defaultRect.y + (i * textureWidth) + (i * TextureOffset);
						drawTextureRect.width = textureWidth;
						drawTextureRect.height = textureWidth;

						if (evt.type == EventType.MouseDown && drawTextureRect.Contains(evt.mousePosition))
						{
							SelectIdx = idx;
							if (window != null)
								window.Repaint();
						}

						Rect buttonRect = new Rect(drawTextureRect)
						{
							height = textureWidth / 4.0f,
							y = defaultRect.y + (i * textureWidth) + (i * TextureOffset) + textureWidth,
						};

						var previewTex = AssetPreview.GetAssetPreview(prefabObjectList[idx]);
						if (previewTex)
						{
							EditorGUI.DrawPreviewTexture(drawTextureRect, previewTex);
						}

						if (GUI.Button(buttonRect, "Del"))
						{
							prefabObjectList.RemoveAt(idx);
							SelectIdx = Mathf.Clamp(SelectIdx - 1, 0, int.MaxValue);
							CheckTmpObject();
						}
					}
					EditorGUILayout.EndVertical();

				}
				EditorGUILayout.EndHorizontal();
			}
		}
		EditorGUILayout.EndScrollView();
		EditorGUILayout.EndVertical();
	}
	
	/// <summary>
	/// ドラッグするエリア
	/// </summary>
	private void DragAreaGUI()
	{
		GUI.color = Color.white;
		EditorGUILayout.BeginVertical();
		Event evt = Event.current;

		Rect dropArea = GUILayoutUtility.GetRect(0.0f, dragAreaHeight - 30.0f, GUILayout.ExpandWidth(true));
		GUI.Box(dropArea, "Prefab drop area");

		switch (evt.type)
		{
			case EventType.DragUpdated:
			case EventType.DragPerform:
				if (dropArea.Contains(evt.mousePosition))
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();
						foreach (Object dragObj in DragAndDrop.objectReferences)
						{
							if (dragObj.GetType() != typeof(GameObject))
								continue;
							Debug.Log("dragobject" + dragObj.name);
							AddPrefabObj(dragObj);

						}
					}
				}
				break;
		}
		EditorGUILayout.EndVertical();
	}

	void CheckTmpObject()
	{
		if (isCreateMode)
		{
			if (tmpObj != null)
				DestroyImmediate(tmpObj);
			tmpObj = CreateTmpObj();
		}
		else
		{
			if (tmpObj != null)
			{
				DestroyImmediate(tmpObj);
			}
		}
	}



	/// <summary>
	/// プレビュー用オブジェクトを生成する
	/// </summary>
	/// <returns></returns>
	GameObject CreateTmpObj()
	{
		var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefabObjectList[SelectIdx]);
		
		DestroyImmediate(obj.GetComponent<Collider>());
		foreach (Transform child in obj.transform)
		{
			DestroyImmediate(child.gameObject.GetComponent<Collider>());
		}
		return obj;
	}

	void AlignGhostToSurface(Transform tmpObj, Vector3 hitNormal)
	{
		tmpObj.rotation = Quaternion.FromToRotation(Vector3.up, hitNormal) * Quaternion.Euler(new Vector3(0.0f, SelectTmpRotation, 0.0f));
	}

	private Vector3 GridToPosition(Vector3 _pos)
	{
		if (selectInfoGrid != 0.0f)
		{
			_pos -= Vector3.one * selectInfoOffset;
			_pos /= selectInfoGrid;
			_pos = new Vector3(Mathf.Round(_pos.x), Mathf.Round(_pos.y), Mathf.Round(_pos.z));
			_pos *= selectInfoGrid;
			_pos += Vector3.one * selectInfoOffset;
		}
		return _pos;
	}

	/// <summary>
	/// プレハブリストに追加
	/// </summary>
	/// <param name="_obj"></param>
	private void AddPrefabObj(Object _obj)
	{
		if (!prefabObjectList.Contains(_obj))
		{
			prefabObjectList.Add(_obj);
			prefabObjectList.Sort((a, b) => a.name.GetHashCode() - b.name.GetHashCode());
		}
	}

	/// <summary>
	/// パスの型をUnity用に変換
	/// </summary>
	/// <param name="_path"></param>
	/// <returns></returns>
	string ConvertSystemPathToUnityPath(string _path)
	{
		int index = _path.IndexOf("Assets");
		if (index > 0)
		{
			_path = _path.Remove(0, index);
		}
		_path.Replace("\\", "/");
		return _path;
	}


}
