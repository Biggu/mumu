using UnityEngine;
using UnityEditor;

namespace mumu
{
    public class MapDataEditor : EditorWindow
    {
        private MapData mapData;
        private Vector2 scroll;

        private enum EditMode
        {
            None,
            SetType,
            Waypoint
        }

        private EditMode mode;

        private eBlockType currentType = eBlockType.Walkable;
        private Vector2Int lastCell = new Vector2Int(-1, -1);

        private int editW;
        private int editH;

        const float BTN_SMALL = 60f;
        const float BTN_MED = 100f;

        [MenuItem("Map/Edit")]
        static void Open()
        {
            GetWindow<MapDataEditor>("Map Editor");
        }

        private void OnGUI()
        {
            mapData = (MapData)EditorGUILayout.ObjectField("MapData", mapData, typeof(MapData), false);
            if (!mapData) return;

            GUILayout.Space(8);

            DrawSize();
            DrawMode();
            DrawGrid();
        }

        // ---------- SIZE ----------
        private void DrawSize()
        {
            EditorGUILayout.BeginVertical("box");

            if (editW == 0)
            {
                editW = mapData.width;
                editH = mapData.height;
            }

            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("W", GUILayout.Width(15));
            editW = EditorGUILayout.IntField(editW, GUILayout.Width(80));

            GUILayout.Space(10);

            GUILayout.Label("H", GUILayout.Width(15));
            editH = EditorGUILayout.IntField(editH, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Apply", GUILayout.Width(BTN_SMALL)))
            {
                Undo.RecordObject(mapData, "Resize");
                Resize(editW, editH);
                EditorUtility.SetDirty(mapData);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ---------- MODE ----------
        private void DrawMode()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);

            // Row 1
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(mode == EditMode.SetType, "Type", "Button", GUILayout.Width(BTN_SMALL)))
                mode = EditMode.SetType;

            if (mode == EditMode.SetType)
                currentType = (eBlockType)EditorGUILayout.EnumPopup(currentType);

            EditorGUILayout.EndHorizontal();

            // Row 2
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(mode == EditMode.Waypoint, "Waypoint", "Button", GUILayout.Width(BTN_MED)))
                mode = EditMode.Waypoint;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6); // ✅ space before validate

            // Row 3
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Validate", GUILayout.Width(BTN_MED)))
                Validate();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ---------- GRID ----------
        private void DrawGrid()
        {
            EditorGUILayout.BeginVertical("box");

            scroll = EditorGUILayout.BeginScrollView(scroll);

            float cell = 28f;
            float spacing = 2f;

            float gridWidth = mapData.width * (cell + spacing);
            float pad = Mathf.Max(0, (position.width - gridWidth) * 0.5f);

            for (int y = 0; y < mapData.height; y++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(pad);

                for (int x = 0; x < mapData.width; x++)
                {
                    int i = y * mapData.width + x;
                    var sub = mapData.blockSubTypes[i];

                    Color old = GUI.backgroundColor;
                    GUI.backgroundColor = GetColor(mapData.blockTypes[i]);

                    string label = "";
                    if (sub.waypointIndex >= 0)
                        label = (sub.waypointIndex + 1).ToString();

                    GUILayout.Box(label, GUILayout.Width(cell), GUILayout.Height(cell));
                    Rect r = GUILayoutUtility.GetLastRect();

                    GUI.backgroundColor = old;

                    HandleInput(x, y, r);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.MouseUp)
                lastCell = new Vector2Int(-1, -1);
        }

       private void HandleInput(int x, int y, Rect r)
{
    if (mode == EditMode.None) return;

    Event e = Event.current;
    if (!r.Contains(e.mousePosition)) return;

    Vector2Int cell = new Vector2Int(x, y);

    // ---------- WAYPOINT (drag support) ----------
    if (mode == EditMode.Waypoint)
    {
        if (e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
        {
            if (cell == lastCell) return;

            Undo.RecordObject(mapData, "Waypoint");
            HandleWaypoint(x, y);

            EditorUtility.SetDirty(mapData);
            lastCell = cell;

            e.Use();
        }

        // right click delete (single click only)
        if (e.button == 1 && e.type == EventType.MouseDown)
        {
            Undo.RecordObject(mapData, "Waypoint Delete");
            HandleWaypoint(x, y);

            EditorUtility.SetDirty(mapData);
            e.Use();
        }

        return;
    }

    // ---------- TYPE ----------
    if (mode == EditMode.SetType && e.button == 0)
    {
        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (cell == lastCell) return;

            Undo.RecordObject(mapData, "SetType");
            mapData.SetBlockType(x, y, currentType);

            if (currentType != eBlockType.Walkable)
                TrimFromCell(x, y);

            EditorUtility.SetDirty(mapData);
            lastCell = cell;

            e.Use();
        }
    }
}

        // ---------- WAYPOINT ----------
        private void HandleWaypoint(int x, int y)
        {
            int i = y * mapData.width + x;
            var cell = mapData.blockSubTypes[i];

            if (mapData.GetBlockType(x, y) != eBlockType.Walkable)
                return;

            // delete
            if (Event.current.button == 1)
            {
                if (cell.waypointIndex >= 0)
                    RemoveFromIndex(cell.waypointIndex);
                return;
            }

            if (cell.waypointIndex >= 0) return;

            int max = GetMaxWaypoint();
            Vector2Int pos = new Vector2Int(x, y);

            if (max < 0)
            {
                Vector2Int start = FindStart();
                if (!IsAdj(pos, start)) return;

                cell.waypointIndex = 0;
                return;
            }

            Vector2Int last = FindWaypoint(max);
            if (!IsAdj(pos, last)) return;

            cell.waypointIndex = max + 1;
        }

        // ---------- TRIM ----------
        private void RemoveFromIndex(int index)
        {
            foreach (var c in mapData.blockSubTypes)
                if (c.waypointIndex >= index)
                    c.waypointIndex = -1;
        }

        private void TrimFromCell(int x, int y)
        {
            int i = y * mapData.width + x;
            int index = mapData.blockSubTypes[i].waypointIndex;

            if (index >= 0)
                RemoveFromIndex(index);
        }

        // ---------- HELPERS ----------
        private int GetMaxWaypoint()
        {
            int max = -1;
            foreach (var c in mapData.blockSubTypes)
                if (c.waypointIndex > max)
                    max = c.waypointIndex;
            return max;
        }

        private Vector2Int FindWaypoint(int index)
        {
            for (int y = 0; y < mapData.height; y++)
                for (int x = 0; x < mapData.width; x++)
                    if (mapData.blockSubTypes[y * mapData.width + x].waypointIndex == index)
                        return new Vector2Int(x, y);

            return new Vector2Int(-1, -1);
        }

        private Vector2Int FindStart()
        {
            for (int y = 0; y < mapData.height; y++)
                for (int x = 0; x < mapData.width; x++)
                    if (mapData.GetBlockType(x, y) == eBlockType.Start)
                        return new Vector2Int(x, y);

            return new Vector2Int(-1, -1);
        }

        private Vector2Int FindGoal()
        {
            for (int y = 0; y < mapData.height; y++)
                for (int x = 0; x < mapData.width; x++)
                    if (mapData.GetBlockType(x, y) == eBlockType.Goal)
                        return new Vector2Int(x, y);

            return new Vector2Int(-1, -1);
        }

        private bool IsAdj(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private void Validate()
        {
            int max = GetMaxWaypoint();
            if (max < 0)
            {
                EditorUtility.DisplayDialog("Error", "No waypoint", "OK");
                return;
            }

            Vector2Int start = FindStart();
            Vector2Int goal = FindGoal();

            if (!IsAdj(FindWaypoint(0), start))
            {
                EditorUtility.DisplayDialog("Error", "Start not connected", "OK");
                return;
            }

            for (int i = 1; i <= max; i++)
            {
                if (!IsAdj(FindWaypoint(i - 1), FindWaypoint(i)))
                {
                    EditorUtility.DisplayDialog("Error", "Broken path", "OK");
                    return;
                }
            }

            if (!IsAdj(FindWaypoint(max), goal))
            {
                EditorUtility.DisplayDialog("Error", "Goal not connected", "OK");
                return;
            }

            EditorUtility.DisplayDialog("OK", "Valid", "Nice");
        }

        private void Resize(int w, int h)
        {
            int oldW = mapData.width;
            int oldH = mapData.height;

            var oldT = mapData.blockTypes;
            var oldS = mapData.blockSubTypes;

            mapData.width = w;
            mapData.height = h;

            mapData.blockTypes = new eBlockType[w * h];
            mapData.blockSubTypes = new MapData.BlockSubTypeList[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int ni = y * w + x;

                    if (x < oldW && y < oldH)
                    {
                        int oi = y * oldW + x;
                        mapData.blockTypes[ni] = oldT[oi];
                        mapData.blockSubTypes[ni] = oldS[oi];
                    }
                    else
                    {
                        // ✅ FIX: default Walkable
                        mapData.blockTypes[ni] = eBlockType.Walkable;
                        mapData.blockSubTypes[ni] = new MapData.BlockSubTypeList();
                    }
                }
            }
        }

        private Color GetColor(eBlockType t)
        {
            switch (t)
            {
                case eBlockType.Empty: return Color.gray;
                case eBlockType.Walkable: return Color.green;
                case eBlockType.Obstruct: return Color.black;
                case eBlockType.Start: return Color.cyan;
                case eBlockType.Goal: return Color.red;
            }
            return Color.white;
        }
    }
}