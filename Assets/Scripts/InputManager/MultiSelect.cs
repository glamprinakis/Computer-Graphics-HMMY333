using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MODZ.RTS.InputManager
{
    // The MultiSelect class contains methods for creating and managing multi-unit selection in the game.
    public static class MultiSelect
    {
        // A private static field to hold a texture used for GUI.
        private static Texture2D _whiteTecture;

        // A public static property to access the white texture. 
        // If the texture does not exist, it will create a new one.
        public static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTecture == null)
                {
                    _whiteTecture = new Texture2D(1, 1); // Create a new 1x1 texture.
                    _whiteTecture.SetPixel(0, 0, Color.white); // Set the pixel color to white.
                    _whiteTecture.Apply(); // Apply all SetPixel and SetPixels changes.
                }
                return _whiteTecture;
            }
        }

        // Draw a colored rectangle on the screen.
        public static void DrawScreenRect(Rect rect, Color color)
        {
            GUI.color = color; // Set the color for drawing.
            GUI.DrawTexture(rect, WhiteTexture); // Draw a texture within the specified rectangle.
        }

        // Draw borders of a rectangle on the screen.
        public static void DrawScreenRectBoarder(Rect rect, float thickness, Color color)
        {
            // Top border.
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            // Bottom border.
            DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            // Left border.
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            // Right border.
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        // Create a rectangle using two vector3 screen positions.
        public static Rect GetScreenRect(Vector3 screenPos1, Vector3 screenPos2)
        {
            // Flip Y coordinates as GUI space starts from top left while screen space starts from bottom left.
            screenPos1.y = Screen.height - screenPos1.y;
            screenPos2.y = Screen.height - screenPos2.y;

            // Get bottom right and top left corners of the rectangle.
            Vector3 bR = Vector3.Max(screenPos1, screenPos2);
            Vector3 tL = Vector3.Min(screenPos1, screenPos2);

            // Create and return the rectangle.
            return Rect.MinMaxRect(tL.x, tL.y, bR.x, bR.y);
        }

        // Get the viewport bounds based on two screen positions.
        public static Bounds GetVPBounds(Camera cam, Vector3 screenPos1, Vector3 screenPos2)
        {
            // Convert screen positions to viewport positions.
            Vector3 pos1 = cam.ScreenToViewportPoint(screenPos1);
            Vector3 pos2 = cam.ScreenToViewportPoint(screenPos2);

            // Find min and max viewport positions.
            Vector3 min = Vector3.Min(pos1, pos2);
            Vector3 max = Vector3.Max(pos1, pos2);

            // Set the z values to the near and far clipping plane.
            min.z = cam.nearClipPlane;
            max.z = cam.farClipPlane;

            // Create a new bounds object and set its min and max.
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);

            return bounds;
        }
    } 

}
