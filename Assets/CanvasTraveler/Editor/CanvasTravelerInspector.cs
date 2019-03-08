using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditorInternal;
using UnityEditor;

namespace AillieoUtils.UI
{
    [CustomEditor(typeof(CanvasTraveler))]
    public class CanvasTravelerInspector : Editor
    {
        private SerializedProperty pathPoints;
        private ReorderableList pathPointsList;
        
        public override void OnInspectorGUI()
        {
            // default
            DrawDefaultInspector();

            EditorGUILayout.Separator();

            // path points
            pathPointsList.DoLayoutList();

            // add new
            if (GUILayout.Button("Append current postion"))
            {
                AppendPosition();
            }

            // apply
            serializedObject.ApplyModifiedProperties();
        }

        void AppendPosition()
        {
            CanvasTraveler canvastTraveler = target as CanvasTraveler;
            Vector3 anchoredPosition = canvastTraveler.GetComponent<RectTransform>().anchoredPosition3D;
            Array.Resize(ref canvastTraveler.pathPoints, canvastTraveler.pathPoints.Length + 1);
            canvastTraveler.pathPoints[canvastTraveler.pathPoints.Length - 1] = anchoredPosition;
        }

        private void OnEnable()
        {
            if (null == pathPointsList)
            {
                pathPoints = serializedObject.FindProperty("pathPoints");
                pathPointsList = new ReorderableList(serializedObject, pathPoints);
                pathPointsList.elementHeight = EditorGUIUtility.singleLineHeight;

                pathPointsList.drawHeaderCallback += DrawHeader;
                pathPointsList.drawElementCallback += DrawPathPointsList;
            }
        }

        private void OnDisable()
        {
            if(null != pathPointsList)
            {
                pathPointsList.drawElementCallback -= DrawPathPointsList;
                pathPointsList.drawHeaderCallback -= DrawHeader;
            }
        }


        void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Path Points");
        }

        private void DrawPathPointsList(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty property = this.pathPoints;

            var element = property.GetArrayElementAtIndex(index);
            var props = new SerializedProperty[]
            {
                element.FindPropertyRelative("x"),
                element.FindPropertyRelative("y"),
                element.FindPropertyRelative("z"),
            };

            var width = rect.width / 3;
            var start = rect.x;
            foreach (var prop in props)
            {
                var oneRect = new Rect(start,
                rect.y,
                width,
                rect.height);
                EditorGUI.PropertyField(oneRect, prop, GUIContent.none);
                start += width;
            }
        }

    }
}
