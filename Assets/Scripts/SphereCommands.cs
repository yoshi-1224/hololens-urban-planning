using UnityEngine;

public class SphereCommands : MonoBehaviour
{
    public float RotationSensitivity = 10.0f;
    private float rotationFactor;
    Vector3 originalPosition;
    Vector3 originalScale;
    Vector3 NewPosition;
    Vector3 tempPosition;
    float yRotation = 0f;

    // Use this for initialization
    void Start()
    {
        // Grab the original local position of the sphere when the app starts.
        originalPosition = this.transform.localPosition;
        originalScale = this.transform.localScale;

    }

    // Called by GazeGestureManager when the user performs a Select gesture


    // Called by SpeechManager when the user says the "Reset world" command
    void OnReset()
    {
        // If the sphere has a Rigidbody component, remove it to disable physics.
        var rigidbody = this.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            DestroyImmediate(rigidbody);
        }

        // Put the sphere back into its original local position.
        this.transform.localPosition = originalPosition;
        this.transform.localScale = originalScale;
    }


    void Right()
    {
      
        yRotation = 90.0f;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
    }
    void Behind()
    {
        
        yRotation = 180.0f;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
    }
    void Left()
    {
      
        yRotation = 270.0f;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
    }
    void Front()
    {
     
        yRotation = 0.0f;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
    }

    void Grow()
    {
        transform.localScale = new Vector3(2, 2, 2);
        //transform.localPosition = new Vector3(0, -10, 80);
       
    }
    void Bigger()
    {
        transform.localScale = new Vector3(5, 5, 5);
        //transform.localPosition = new Vector3(0, -10, 80);

    }
    void Shrink()
    {
        //float yRotation = 90.0f;
        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
    }
}