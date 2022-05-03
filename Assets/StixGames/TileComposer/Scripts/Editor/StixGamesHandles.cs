using UnityEditor;
using UnityEngine;

namespace StixGames.TileComposer
{
    public static class StixGamesHandles
    {
        public static void DrawConnectionCircle(int controlId, Vector3 position, Vector3 normal, float radius,
            Color borderColor, Color backgroundColor)
        {
            // Background circle
            Handles.color = backgroundColor;
            Handles.DrawSolidDisc(position, normal,
                radius);

            // Outer border circle
            Handles.color = borderColor;
            Handles.CircleHandleCap(controlId, position, Quaternion.LookRotation(normal),
                radius, EventType.Repaint);
        }
    }
}