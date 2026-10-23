using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// How the end of the path connects back to the start.
//   Loop     - after the last point, go back to the first (1-2-3-4-1-2...)
//   PingPong - walk to the end, then walk back the same way (1-2-3-4-3-2-1...)
public enum PathConnectType
{
    Loop,
    PingPong
}

// ONE script, ONE file. Put it on one empty GameObject called "Patrol Path".
//
// You make the waypoints yourself: click Add Point in the Inspector,
// then drag the point in the Scene view. The Add Point button and the
// drag handles are at the bottom of this file, so you do NOT need an
// Editor folder or a second script.
//
// The points are world positions. Move the POINTS, not this object.
public class PatrolPath : MonoBehaviour
{
    [SerializeField] private PathConnectType connectType = PathConnectType.Loop;

    // Starts empty on purpose. The points are yours to make.
    [SerializeField] private List<Vector3> waypoints = new List<Vector3>();

    public List<Vector3> Waypoints
    {
        get { return waypoints; }
    }

    public int Count
    {
        get { return waypoints.Count; }
    }

    public Vector3 GetPoint(int index)
    {
        return waypoints[index];
    }

    // Works out which point comes next. The enemy keeps track of its own
    // index and direction, so many enemies can share one path.
    public int GetNextIndex(int currentIndex, ref int direction)
    {
        if (waypoints.Count <= 1)
        {
            return 0;
        }

        if (connectType == PathConnectType.Loop)
        {
            return (currentIndex + 1) % waypoints.Count;
        }

        // PingPong: turn around at either end.
        int nextIndex = currentIndex + direction;

        if (nextIndex >= waypoints.Count || nextIndex < 0)
        {
            direction = -direction;
            nextIndex = currentIndex + direction;
        }

        return nextIndex;
    }

    // Draws the path in the Scene view. Green ball = the first point.
    // Cyan lines connect the points.
    private void OnDrawGizmos()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.color = (i == 0) ? Color.green : Color.cyan;
            Gizmos.DrawSphere(waypoints[i], 0.4f);

            bool isLastPoint = (i == waypoints.Count - 1);

            if (isLastPoint && connectType != PathConnectType.Loop)
            {
                continue;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(waypoints[i], waypoints[(i + 1) % waypoints.Count]);
        }
    }
}

// =====================================================================
// EDITOR TOOLS (Add Point button + drag handles)
//
// Everything below only exists inside the Unity Editor. The
// "#if UNITY_EDITOR" lines make Unity leave it out when you build the
// game, so it can safely live in this same file.
// =====================================================================
#if UNITY_EDITOR
[CustomEditor(typeof(PatrolPath))]
public class PatrolPathInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PatrolPath path = (PatrolPath)target;

        EditorGUILayout.Space();

        if (path.Count == 0)
        {
            EditorGUILayout.HelpBox("No points yet. Click Add Point. Then drag the point in the Scene view.", MessageType.Info);
        }

        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(path, "Add Waypoint");
            path.Waypoints.Add(GetNewPointPosition(path));
            EditorUtility.SetDirty(path);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Remove Last Point"))
        {
            if (path.Count > 0)
            {
                Undo.RecordObject(path, "Remove Waypoint");
                path.Waypoints.RemoveAt(path.Count - 1);
                EditorUtility.SetDirty(path);
                SceneView.RepaintAll();
            }
        }
    }

    private void OnSceneGUI()
    {
        PatrolPath path = (PatrolPath)target;

        for (int i = 0; i < path.Count; i++)
        {
            Handles.Label(path.Waypoints[i] + Vector3.up * 1f, "Point " + (i + 1));

            EditorGUI.BeginChangeCheck();
            Vector3 movedPosition = Handles.PositionHandle(path.Waypoints[i], Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(path, "Move Waypoint");
                path.Waypoints[i] = movedPosition;
                EditorUtility.SetDirty(path);
            }
        }
    }

    // A new point appears 3 units to the side of the last one,
    // so it never lands on top of it.
    private Vector3 GetNewPointPosition(PatrolPath path)
    {
        if (path.Count > 0)
        {
            return path.Waypoints[path.Count - 1] + new Vector3(3f, 0f, 0f);
        }

        return path.transform.position;
    }
}
#endif