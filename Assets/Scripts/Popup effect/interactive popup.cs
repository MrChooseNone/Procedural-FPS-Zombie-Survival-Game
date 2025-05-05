using TMPro;
using UnityEngine;

public class InteractivePopup : MonoBehaviour
{
    public GameObject popupPrefab; // The popup GameObject (containing sprite and TextMeshPro)
    private GameObject popupInstance; // Instance of the popup that will be shown
    private Transform player; // The player object
    public float displayDistance = 3f; // The distance at which the popup becomes visible
    private TextMeshPro popupText; // TextMeshPro component inside the popup
    public float popupHeight = 2f;  // Height offset for the popup above the gun
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string text;
    private Animator animator;
    private string animation;
    void Start()
    {
        // Instantiate the object at a specified position and rotation
        popupInstance = Instantiate(popupPrefab, transform.position, Quaternion.identity);

        // Set the parent of the instantiated object (make it a child of the current gameObject)
        popupInstance.transform.SetParent(transform);

        // Optionally, reset the position of the instantiated object relative to its new parent (this will reset its local position)
        //popupInstance.transform.localPosition = Vector3.zero; // This can be customized based on your needs
        
        popupInstance.SetActive(false); // Initially, hide the popup

        // Get the TextMeshPro component for changing the text if needed
        popupText = popupInstance.GetComponentInChildren<TextMeshPro>();
        popupText.text = text;
        animator = popupInstance.GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {

            
            // Always rotate the popup to face the player (billboarding effect)
            popupInstance.transform.LookAt(player.position);

            // Ensure the popup stays above the gun
            Vector3 gunPosition = transform.position;
            // Raycast downwards to get the height from the ground
            RaycastHit hit;
            if (Physics.Raycast(gunPosition + Vector3.up * 10f, Vector3.down, out hit, 20f))  // Raycast from above the gun
            {
                // Set the popup's position slightly above the ground
                popupInstance.transform.position = new Vector3(gunPosition.x, hit.point.y + popupHeight, gunPosition.z);
            }
        }
    }

     // Show the popup and animate it
    public void ShowPopup(Transform currPlayer)
    {
        Debug.Log("show in interactive popup");
        popupInstance.SetActive(true); // Show the popup
        if(currPlayer != null){
            player = currPlayer;
        }
        if(animation != "popup" ){
            animation  = "popup";
            animator.CrossFade(animation,0.2f, 0);
        }
    }

    // Hide the popup
    public void HidePopup()
    {
        if(animation != "hide" ){
            animation  = "hide";
            animator.CrossFade(animation,0.2f, 0);
        }
        Debug.Log("hide in interactive popup");
        popupInstance.SetActive(false); // Hide the popup
        player = null;
        
    }

    
}
