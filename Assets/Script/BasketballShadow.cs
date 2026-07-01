using UnityEngine;

public class BasketballShadow : MonoBehaviour
{
    public GameObject shadowPrefab; // Reference to the shadow sprite prefab
    public float maxHeight = 1f; // Maximum height of the basketball

    private GameObject shadow; // Reference to the instantiated shadow sprite
    private SpriteRenderer shadowRenderer; // Reference to the shadow sprite renderer

    private void Start()
    {
        // Instantiate the shadow prefab and make it a child of the basketball
        shadow = Instantiate(shadowPrefab);
        shadow.transform.localPosition = Vector3.zero;
        //shadow.transform.localScale = Vector3.one;

        // Get the shadow sprite renderer
        shadowRenderer = shadow.GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        // Update the position and size of the shadow based on the basketball's position and height
        Vector3 basketballPosition = new Vector3(transform.position.x, -6f,transform.position.z);
        basketballPosition.z = shadow.transform.position.z; // Maintain the same z position
        shadow.transform.position = basketballPosition;

        // Calculate the current height percentage based on the basketball's height
        //float heightPercentage = Mathf.Clamp01(basketballPosition.y / maxHeight);

        // Adjust the scale of the shadow based on the height percentage
        //float shadowScale = 1f - heightPercentage * 0.5f; // Scale down the shadow as the basketball goes higher
        //shadow.transform.localScale = new Vector3(shadowScale, shadowScale, 1f);

        // Adjust the transparency of the shadow based on the height percentage
        //Color shadowColor = shadowRenderer.color;
        //shadowColor.a = 1f - heightPercentage * 0.5f; // Make the shadow more transparent as the basketball goes higher
        //shadowRenderer.color = shadowColor;
    }
}
