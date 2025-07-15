using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MODZ.RTS.Units.Player;

namespace MODZ.RTS.InputManager
{
    // This class manages the input events in the game like selecting, deselecting units, and their movements.
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler instance;

        private RaycastHit hit;//what we hit with our ray

        // This list is used to store all selected units in the game.
        public List<Transform> selectedUnits = new List<Transform>();

        // This flag is used to check if the player is currently dragging the mouse to select units.
        private bool isDragging = false;

        // The position where the mouse was clicked down. 
        private Vector3 mousePos;

        private void Awake()
        {
            // Singleton instance of this class.
            instance = this;
        }

        private void OnGUI()
        {
            // If the user is dragging, draw a selection box on the screen.
            if (isDragging)
            {
                Rect rect = MultiSelect.GetScreenRect(mousePos, Input.mousePosition);
                MultiSelect.DrawScreenRect(rect, new Color(0f, 0f, 0f, 0.25f));
                MultiSelect.DrawScreenRectBoarder(rect, 3, Color.green);
            }
        }

        public void HandleUnitMovement()
        {
            // On left mouse button press, handle unit selection or begin drag.
            if (Input.GetMouseButtonDown(0))
            {
                mousePos = Input.mousePosition;
                //create a ray
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //shoot that ray to see if we hit our unit\
                if (Physics.Raycast(ray, out hit))
                {
                    //if we do, do something
                    LayerMask layerHit = hit.transform.gameObject.layer;

                    switch (layerHit.value)
                    {
                        case 8://unit layer
                               //do something
                            SelectUnit(hit.transform, Input.GetKey(KeyCode.LeftShift));
                            break;
                        default://if none of the above happens
                            isDragging = true;
                            DeselectUnits();
                            break;
                    }
                }
            }

            // On left mouse button release, handle multi-unit selection using the selection box.
            if (Input.GetMouseButtonUp(0))
            {
                foreach (Transform child in Player.PlayerManager.instance.playerUnits)
                {
                    foreach (Transform unit in child)
                    {
                        if (isWithinSelectionBounds(unit))
                        {
                            SelectUnit(unit, true);
                        }
                    }
                }
                isDragging = false;
            }

            // On right mouse button press, move selected units to the point clicked.
            if (Input.GetMouseButton(1) && HaveSelectedUnits())
            {
                //create a ray
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //shoot that ray to see if we hit our unit\
                if (Physics.Raycast(ray, out hit))
                {
                    LayerMask layerHit = hit.transform.gameObject.layer;

                    switch (layerHit.value)
                    {
                        case 8://unit layer
                            break;//enemy unit
                        case 9:
                            //attack or target
                            break;
                        default://if none of the above happens
                            foreach (Transform unit in selectedUnits)
                            {
                                PlayerUnit pU = unit.gameObject.GetComponent<PlayerUnit>();
                                pU.MoveUnit(hit.point);
                            }
                            break;
                    }
                }
            }
        }

        // This method selects a unit, while handling the case of multi-selection using shift key.
        private void SelectUnit(Transform unit, bool canMultiselect = false)
        {
            if (unit == null)
            {
                return;
            }

            if (!canMultiselect)
            {
                DeselectUnits();
            }

            selectedUnits.Add(unit);
            Transform highlight = unit.Find("Highlight");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(true);
            }
        }

        // This method deselects all currently selected units.
        private void DeselectUnits()
        {
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                Transform unit = selectedUnits[i];
                if (unit != null)
                {
                    Transform highlight = unit.Find("Highlight");
                    if (highlight != null)
                    {
                        highlight.gameObject.SetActive(false);
                    }
                }
            }

            selectedUnits.Clear();
        }

        // This method checks if the unit is within the selection box boundaries.
        private bool isWithinSelectionBounds(Transform tf)
        {
            if (!isDragging)
            {
                return false;
            }

            Camera cam = Camera.main;
            Bounds vpBounds = MultiSelect.GetVPBounds(cam, mousePos, Input.mousePosition);
            return vpBounds.Contains(cam.WorldToViewportPoint(tf.position));
        }

        // This method checks if there are any units currently selected.
        private bool HaveSelectedUnits()
        {
            if (selectedUnits.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}
