using UnityEditor;
using UnityEngine;

/// <summary>
/// StageData 인스펙터에서 셀 경계를 클릭해
/// 벽을 배치하거나 제거할 수 있게 표시한다.
/// </summary>
[CustomEditor(typeof(StageData))]
public sealed class StageDataEditor : Editor
{
    private const float CellSize = 42f;
    private const float WallButtonSize = 8f;

    private SerializedProperty widthProperty;
    private SerializedProperty heightProperty;
    private SerializedProperty paintBucketsProperty;
    private SerializedProperty wallPositionsProperty;

    private void OnEnable()
    {
        widthProperty = serializedObject.FindProperty("width");
        heightProperty = serializedObject.FindProperty("height");
        paintBucketsProperty = serializedObject.FindProperty("paintBuckets");
        wallPositionsProperty = serializedObject.FindProperty("wallPositions");
    }

    /// <summary>
    /// 기본 스테이지 데이터와 클릭형 벽 편집 격자를 그린다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(widthProperty);
        EditorGUILayout.PropertyField(heightProperty);
        EditorGUILayout.PropertyField(paintBucketsProperty, true);

        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wall Editor", EditorStyles.boldLabel);

        DrawWallGrid();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(wallPositionsProperty.arraySize == 0))
        {
            if (GUILayout.Button("모든 벽 제거"))
            {
                ClearWalls();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWallGrid()
    {
        int width = Mathf.Max(1, widthProperty.intValue);
        int height = Mathf.Max(1, heightProperty.intValue);

        Rect gridRect = GUILayoutUtility.GetRect(
            width * CellSize,
            height * CellSize,
            GUILayout.ExpandWidth(false)
        );

        DrawCells(gridRect, width, height);
        DrawVerticalWalls(gridRect, width, height);
        DrawHorizontalWalls(gridRect, width, height);
    }

    private void DrawCells(Rect gridRect, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            // GUI의 Y축은 아래로 증가하므로 게임 격자의 Y축과
            // 같은 방향으로 보이도록 행 순서를 뒤집는다.
            int displayY = height - 1 - y;

            for (int x = 0; x < width; x++)
            {
                Rect cellRect = new(
                    gridRect.x + x * CellSize,
                    gridRect.y + displayY * CellSize,
                    CellSize,
                    CellSize
                );

                GUI.Box(cellRect, $"{x}, {y}");
            }
        }
    }

    private void DrawVerticalWalls(Rect gridRect, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            int displayY = height - 1 - y;

            for (int x = 0; x < width - 1; x++)
            {
                // 셀 (x, y)와 (x + 1, y) 사이 경계를
                // 2배 좌표계의 홀수 x 좌표로 변환한다.
                Vector2Int wallPosition = new Vector2Int(x * 2 + 1, y * 2);

                Rect buttonRect = new(
                    gridRect.x + (x + 1) * CellSize - WallButtonSize * 0.5f,
                    gridRect.y + displayY * CellSize + WallButtonSize * 0.5f,
                    WallButtonSize,
                    CellSize - WallButtonSize
                );

                DrawWallButton(buttonRect, wallPosition);
            }
        }
    }

    private void DrawHorizontalWalls(Rect gridRect, int width, int height)
    {
        for (int y = 0; y < height - 1; y++)
        {
            int displayBoundaryY = height - 1 - y;

            for (int x = 0; x < width; x++)
            {
                // 셀 (x, y)와 (x, y + 1) 사이 경계를
                // 2배 좌표계의 홀수 y 좌표로 변환한다.
                Vector2Int wallPosition = new Vector2Int(x * 2, y * 2 + 1);

                Rect buttonRect = new(
                    gridRect.x + x * CellSize + WallButtonSize * 0.5f,
                    gridRect.y + displayBoundaryY * CellSize - WallButtonSize * 0.5f,
                    CellSize - WallButtonSize,
                    WallButtonSize
                );

                DrawWallButton(buttonRect, wallPosition);
            }
        }
    }

    private void DrawWallButton(Rect buttonRect, Vector2Int wallPosition)
    {
        bool hasWall = HasWall(wallPosition);
        Color previousColor = GUI.backgroundColor;

        GUI.backgroundColor = hasWall ? Color.white : new Color(0.35f, 0.35f, 0.35f);

        if (GUI.Button(buttonRect, GUIContent.none))
        {
            ToggleWall(wallPosition);
        }

        GUI.backgroundColor = previousColor;
    }

    private bool HasWall(Vector2Int wallPosition)
    {
        return FindWallIndex(wallPosition) >= 0;
    }

    private int FindWallIndex(Vector2Int wallPosition)
    {
        for (int index = 0; index < wallPositionsProperty.arraySize; index++)
        {
            if (wallPositionsProperty.GetArrayElementAtIndex(index).vector2IntValue == wallPosition)
            {
                return index;
            }
        }

        return -1;
    }

    private void ToggleWall(Vector2Int wallPosition)
    {
        // ScriptableObject 편집도 일반 인스펙터처럼
        // Ctrl+Z로 되돌릴 수 있도록 변경 전에 기록한다.
        Undo.RecordObject(target, "Toggle Stage Wall");

        int index = FindWallIndex(wallPosition);

        if (index >= 0)
        {
            wallPositionsProperty.DeleteArrayElementAtIndex(index);
        }
        else
        {
            int newIndex = wallPositionsProperty.arraySize;

            wallPositionsProperty.arraySize++;
            wallPositionsProperty.GetArrayElementAtIndex(newIndex).vector2IntValue = wallPosition;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void ClearWalls()
    {
        Undo.RecordObject(target, "Clear Stage Walls");

        wallPositionsProperty.arraySize = 0;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
