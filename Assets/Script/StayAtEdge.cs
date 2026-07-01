using UnityEngine;

public class StayAtEdge : MonoBehaviour
{
    public GameObject Point;

    public bool stayAtLeftEdge = true;
    public bool stayAtRightEdge = true;

    private Camera mainCamera;
    private float objectWidth;
    private Vector3 desiredPosition;

    private bool _loggedOnce = false;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Get the width of the object
        objectWidth = GetComponent<Renderer>().bounds.size.x;
        Debug.Log($"[StayAtEdge] {gameObject.name} | Point={(Point != null ? Point.name : "NULL")} | leftEdge={stayAtLeftEdge} | rightEdge={stayAtRightEdge} | objectWidth={objectWidth}");
    }

    private void Update()
    {
        if (stayAtLeftEdge)
        {
            // Calculate the desired position at the center left edge of the camera
            float leftViewportX = 0f;
            float desiredX = mainCamera.ViewportToWorldPoint(new Vector3(leftViewportX, 0.5f, Mathf.Abs(mainCamera.transform.position.z))).x + (objectWidth / 2f);
            desiredPosition = new Vector3(desiredX, transform.position.y, transform.position.z);
            transform.position = desiredPosition;
        }

        if (stayAtRightEdge)
        {
            // Calculate the desired position at the center right edge of the camera
            float rightViewportX = 1f;
            float desiredX = mainCamera.ViewportToWorldPoint(new Vector3(rightViewportX, 0.5f, Mathf.Abs(mainCamera.transform.position.z))).x - (objectWidth / 2f);
            desiredPosition = new Vector3(desiredX, transform.position.y, transform.position.z);
            transform.position = desiredPosition;
        }

        if (!_loggedOnce)
        {
            Debug.Log($"[StayAtEdge] {gameObject.name} first Update | pos={transform.position} | Point={(Point != null ? Point.name : "NULL")}");
            _loggedOnce = true;
        }
    }
}
