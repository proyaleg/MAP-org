using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5.0f;
    private Rigidbody rb;

    private float moveHorizontal;
    private float moveVertical;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //transform.position = new Vector3(0, 0, 4);
    }

    // Update is called once per frame
    void Update()
    {
        

        //moveHorizontal = Input.GetAxis("Horizontal");
        //moveVertical = Input.GetAxis("Vertical");

        


        //transform.Translate(Vector3.right * Input.GetAxis("Horizontal") * speed * Time.deltaTime);
        //transform.Translate(Vector3.up * Input.GetAxis("Vertical") * speed * Time.deltaTime);
        
        moveHorizontal = 0.0f;
        moveVertical = 0.0f;

        if (Input.GetKey(KeyCode.D) ||  Input.GetKey(KeyCode.RightArrow))
        {
            moveHorizontal = 1.0f;
            Debug.Log("Painat OIKEALLE");
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveHorizontal = -1.0f;
            Debug.Log("Painat Vasemmalle");
        }

        if (Input.GetKey(KeyCode.W)  || Input.GetKey(KeyCode.UpArrow))
        {
            Jump();
        }
        

        //Vector3 movement = new Vector3(moveHorizontal, 0.0f, 0.0f);
        //transform.Translate(movement * speed * Time.deltaTime);

    }

    public void moveForward()
    {

        moveHorizontal = 0.0f;
        moveVertical = 0.0f;

            moveHorizontal = 1.0f;
            Debug.Log("Painat OIKEALLE");
       

        /*
        moveHorizontal = 0.0f;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.position.x > Screen.width / 2)
            {
                moveHorizontal = 1.0f;
            }
            else if (touch.position.x < Screen.width / 2)
            {
                moveHorizontal = -1.0f;
            }
        }

        */
    }

    public void Jump()
    {
        moveVertical = 0f;
        moveVertical = 2.0f;
        Debug.Log("Hyppäsit");
    }

    void FixedUpdate()
    {
        // Create movement vector on the X and Z axes (Y stays 0 so gravity works)
        Vector3 movement = new Vector3(moveHorizontal, moveVertical, 0.0f);


        // Move the Rigidbody safely through the physics engine
        rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);

        //Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        //rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Este") 
        { Debug.Log("Osuit esteeseen"); }
    }

    
}
