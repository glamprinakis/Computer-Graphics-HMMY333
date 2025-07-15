using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    float speed = 0.06f;
    float zoomSpeed = 10.0f;
    float rotateSpeed = 0.1f;

    public float maxHeight = 500f;
    float minHeight = 4f;

    public Vector2 uLim;
    public Vector2 lLim;

    //Rotation vectors
    Vector2 p1;
    Vector2 p2;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        // Check if user pressed Shift+T
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T))
        {
            // Reset camera position and rotation to view map from directly above
            transform.position = new Vector3(transform.position.x, maxHeight + 1f, transform.position.z);
            transform.rotation = Quaternion.Euler(0, 0, 0);
            return;
        }

        // Implementing a fast mode when holding shift key 
        if (Input.GetKey(KeyCode.LeftShift))//if shift is pushed then we update the zoom speed 
        {
            speed = 0.06f;
            zoomSpeed = 20.0f;
        }
        else//we reduce speed 
        {
            speed = 0.035f;
            zoomSpeed = 10.0f;
        }
        //boundary
        Vector3 pos = transform.position;//Save current possition to a vactor3 because we cant edit transform.position
        pos.x = Mathf.Clamp(pos.x, -lLim.x, uLim.x);//Place boundary on x axis 
        pos.z = Mathf.Clamp(pos.z, -lLim.y, uLim.y);//Place boundary on z axis 
        transform.position = pos;//update position


        float hsp = transform.position.y * speed * Input.GetAxis("Horizontal"); //Horizontal speed, we multiply them by transform.position.y so as we get more hight the speed will increase 
        float vsp = transform.position.y * speed * Input.GetAxis("Vertical"); //Vertical speed, we multiply them by transform.position.y so as we get more hight the speed will increase
        float scrollsp = Mathf.Log(transform.position.y) * -zoomSpeed * Input.GetAxis("Mouse ScrollWheel"); //Zoom speed, we multiply them by transform.position.y so as we get more hight the speed will increase but for zoom we want it to scale linearly not expenensialy 

        //hight limiter
        if ((transform.position.y >= maxHeight) && (scrollsp > 0))
        {
            scrollsp = 0;
        }
        else if ((transform.position.y <= minHeight) && (scrollsp < 0))
        {
            scrollsp = 0;
        }

        //if we scroll to fast maybe we will pass the hight barrier so with that if we aren't able to overshoot nomater the scroling speed
        if ((transform.position.y + scrollsp) > maxHeight)
        {
            scrollsp = maxHeight - transform.position.y;
        }
        else if ((transform.position.y + scrollsp) < minHeight)
        {
            scrollsp = minHeight - transform.position.y;
        }

        Vector3 verticalMove = new Vector3(0, scrollsp, 0);  //All movement in y axis 
        Vector3 lateralMove = hsp * transform.right; //Move sideways based on the direction that camera is facing(not left and right based in world space)
        Vector3 forwardMove = transform.forward; //Forawd move (because camera is facing down if we use the regular transform it will move us torwards the ground)
        forwardMove.y = 0; //We make y = 0 so we dont move down anymore
        forwardMove.Normalize(); //Normalize, make magnitude = 1
        forwardMove *= vsp; //We muliply it by vertical speed

        Vector3 move = verticalMove + lateralMove + forwardMove; //Summ of all 3 movement vector to final move

        transform.position += move; //Add the move to position to establise new position 

        getCameraRotation();

        // Edge scrolling
        /*
        float edgeSize = 20.0f; // size of the scrolling area at the edge of the screen
        float edgeSpeed = 0.01f; // speed of scrolling
        float mouseX = Input.mousePosition.x;
        float mouseY = Input.mousePosition.y;

        Vector3 rightMovement = transform.right * edgeSpeed * transform.position.y;
        Vector3 upMovement = transform.forward * edgeSpeed * transform.position.y;

        if (mouseX < edgeSize)
        {
            transform.position -= rightMovement;
        }
        else if (mouseX > Screen.width - edgeSize)
        {
            transform.position += rightMovement;
        }

        if (mouseY < edgeSize)
        {
            transform.position -= upMovement;
        }
        else if (mouseY > Screen.height - edgeSize)
        {
            transform.position += upMovement;
        }         */
        float edgeSize = 20.0f; // size of the scrolling area at the edge of the screen
        float edgeSpeed = 0.01f; // speed of scrolling

        Vector3 rightMovement = transform.right * edgeSpeed * transform.position.y;
        Vector3 upMovement = transform.forward * edgeSpeed * transform.position.y;

        Rect gameScreen = new Rect(0, 0, Screen.width, Screen.height); // define the game screen rectangle

        if (gameScreen.Contains(Input.mousePosition)) // check if the mouse is within the game screen
        {
            if (Input.mousePosition.x < edgeSize)
            {
                transform.position -= rightMovement;
            }
            else if (Input.mousePosition.x > Screen.width - edgeSize)
            {
                transform.position += rightMovement;
            }

            if (Input.mousePosition.y < edgeSize)
            {
                transform.position -= upMovement;
            }
            else if (Input.mousePosition.y > Screen.height - edgeSize)
            {
                transform.position += upMovement;
            }
        }


    }

    void getCameraRotation()
    {
        if (Input.GetMouseButtonDown(2))//chek if middle mouse button in pressed 
        {
            p1 = Input.mousePosition; //we want the possition that the mouse was when midle mouse button was pressed
        }
        if (Input.GetMouseButton(2)) //check if the middle mouse button is still pressed
        {
            p2 = Input.mousePosition; //the possition that mouse was when we let off midle mouse button

            float dx = (p2 - p1).x * rotateSpeed; //how far the mouse is moved in x axis since the last frame and its scaled by rotation speed
            float dy = (p2 - p1).y * rotateSpeed; //how far the mouse is moved in x axis since the last frame and its scaled by rotation speed

            transform.rotation *= Quaternion.Euler(new Vector3(0, dx, 0)); //update the rotation of camera 
            transform.GetChild(0).transform.rotation *= Quaternion.Euler(new Vector3(-dy, 0, 0)); //tilt the camera up and down 
            p1 = p2;
        }
    }
}