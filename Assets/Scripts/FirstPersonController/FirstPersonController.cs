﻿// CHANGE LOG
// 
// CHANGES || version VERSION
//
// "Enable/Disable Headbob, Changed look rotations - should result in reduced camera jitters" || version 1.0.1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Mirror;
using Mirror.Examples.Common;
using Unity.Cinemachine;
using Mirror.BouncyCastle.Tls;







#if UNITY_EDITOR
using UnityEditor;
    using System.Net;
#endif

public class FirstPersonController : NetworkBehaviour
{
    private Rigidbody rb;

    #region Camera Movement Variables

    public Camera playerCamera;

    public float fov = 60f;
    public bool invertCamera = false;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 50f;

    // Crosshair
    public bool lockCursor = true;
    public bool crosshair = true;
    public Sprite crosshairImage;
    public Color crosshairColor = Color.white;

    // Internal Variables
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private Image crosshairObject;

    #region Camera Zoom Variables

    public bool enableZoom = true;
    public bool holdToZoom = false;
    public KeyCode zoomKey = KeyCode.Mouse1;
    public float zoomFOV = 30f;
    public float zoomStepTime = 5f;

    // Internal Variables
    private bool isZoomed = false;

    #endregion
    #endregion

    #region Movement Variables

    public bool playerCanMove = true;
    public float walkSpeed = 5f;
    public float maxVelocityChange = 10f;

    // Internal Variables
    private bool isWalking = false;

    #region Sprint

    public bool enableSprint = true;
    public bool unlimitedSprint = false;
   
    public float sprintSpeed = 7f;
    public float sprintDuration = 5f;
    public float sprintCooldown = .5f;
    public float sprintFOV = 80f;
    public float sprintFOVStepTime = 10f;

    // Sprint Bar
    public bool useSprintBar = true;
    public bool hideBarWhenFull = true;
    public Image sprintBarBG;
    public Image sprintBar;
    public float sprintBarWidthPercent = .3f;
    public float sprintBarHeightPercent = .015f;

    // Internal Variables
    private CanvasGroup sprintBarCG;
    private bool isSprinting = false;
    private float sprintRemaining;
    private float sprintBarWidth;
    private float sprintBarHeight;
    private bool isSprintCooldown = false;
    private float sprintCooldownReset;

    #endregion

    #region Jump

    public bool enableJump = true;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 5f;

    // Internal Variables
    private bool isGrounded = false;
    

    #endregion

    #region Crouch

    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .75f;
    public float speedReduction = .5f;

    // Internal Variables
    private bool isCrouched = false;
    private Vector3 originalScale;

    #endregion
    #endregion

    #region Head Bob

    public bool enableHeadBob = true;
    public Transform joint;
    public float bobSpeed = 10f;
    public Vector3 bobAmount = new Vector3(.15f, .05f, 0f);

    // Internal Variables
    private Vector3 jointOriginalPos;
    private float timer = 0;

    #endregion

    
    [SerializeField]
    public Animator playerAnimator;
    float camRotation;
    private bool inspecting;
    private int layerHolster;
    private int layerActions;
    private int layerOverlay;
    private bool isIdle;
    private string currAnimation;
    public bool isJumping = false;
    private bool canLand = false;
    
    private WeaponPickupController weaponController;

    public float MaxHealth { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public float CurrentHealth { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    //animations
    private float lastSpeed = 0f;
    private float lastStrafe = 0f;
    private bool lastSprinting = false;
    private bool lastWalking = false;
    public Animator bodyAnimator;
    public Animator handAnimator;

    public GameObject rightArm;
    public GameObject leftArm;
    public GameObject head;

    //push
    public float pushStrength = 10f;
    public float pushRange = 2f;
    public float pushDelay =1f;
    private bool canPush = true;
    public PunchComboSystem punchSystem;
    public AudioSource walkSource;
    public AudioSource sprintSource;
    public SoundEmitter soundEmitter;
    public AudioSource audioSource;
    public AudioClip crouch;
    public AudioClip standUp;
    public AudioClip jump;
    public AudioClip push;

    //crouch
    private Vector3 originalCameraPos;





    // private void Awake()
    // {
    //     if(!isLocalPlayer){return;}
    //     Debug.Log("awake is local");
    //     rb = GetComponent<Rigidbody>();

    //     crosshairObject = GetComponentInChildren<Image>();

    //     // Set internal variables
    //     playerCamera.fieldOfView = fov;
    //     originalScale = transform.localScale;
    //     jointOriginalPos = joint.localPosition;

    //     if (!unlimitedSprint)
    //     {
    //         sprintRemaining = sprintDuration;
    //         sprintCooldownReset = sprintCooldown;
    //     }
    // }

    void Start()
    {
        if (!isLocalPlayer)
        {
            playerCamera.enabled = false;
            return;
        }
        //remove arms for local player

        // rightArm.transform.localScale = Vector3.zero;
        // leftArm.transform.localScale = Vector3.zero;
        head.transform.localScale = Vector3.zero;


        MeshRenderer mesh = GetComponent<MeshRenderer>();
        if (mesh != null)
        {
            mesh.enabled = false;
        }



        soundEmitter = gameObject.GetComponent<SoundEmitter>();

        rb = GetComponent<Rigidbody>();

        crosshairObject = GetComponentInChildren<Image>();

        // Set internal variables
        playerCamera.fieldOfView = fov;
        originalScale = transform.localScale;
        jointOriginalPos = joint.localPosition;

        if (!unlimitedSprint)
        {
            sprintRemaining = sprintDuration;
            sprintCooldownReset = sprintCooldown;
        }
        Debug.Log("start is local");
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (crosshair)
        {
            crosshairObject.sprite = crosshairImage;
            crosshairObject.color = crosshairColor;
        }
        else
        {
            crosshairObject.gameObject.SetActive(false);
        }

        #region Sprint Bar



        if (useSprintBar)
        {
            sprintBarBG.gameObject.SetActive(true);
            sprintBar.gameObject.SetActive(true);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            sprintBarWidth = screenWidth * sprintBarWidthPercent;
            sprintBarHeight = screenHeight * sprintBarHeightPercent;

            // sprintBarBG.rectTransform.sizeDelta = new Vector3(sprintBarWidth, sprintBarHeight, 0f);
            // sprintBar.rectTransform.sizeDelta = new Vector3(sprintBarWidth - 2, sprintBarHeight - 2, 0f);


        }
        else
        {
            sprintBarBG.gameObject.SetActive(false);
            sprintBar.gameObject.SetActive(false);
        }

        #endregion

        weaponController = GetComponent<WeaponPickupController>();

        originalCameraPos = playerCamera.transform.localPosition;
    }



    private void Update()
    {
        #region Camera
        if (!isLocalPlayer) { return; }
        Debug.Log("update is local");
        // Control camera movement
        if (cameraCanMove)
        {
            Mouse mouse = Mouse.current;
            yaw = transform.localEulerAngles.y + mouse.delta.x.ReadValue() * mouseSensitivity;

            if (!invertCamera)
            {
                pitch -= mouseSensitivity * mouse.delta.y.ReadValue();
            }
            else
            {
                pitch += mouseSensitivity * mouse.delta.y.ReadValue();
            }


            // Clamp pitch between lookAngle
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            transform.localEulerAngles = new Vector3(0, yaw, 0);
            playerCamera.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        #region Camera Zoom

        if (enableZoom)
        {
            // Changes isZoomed when key is pressed
            // Behavior for toogle zoom
            if (Input.GetKeyDown(zoomKey) && !holdToZoom && !isSprinting)
            {
                if (!isZoomed)
                {
                    isZoomed = true;
                }
                else
                {
                    isZoomed = false;
                }
            }

            // Changes isZoomed when key is pressed
            // Behavior for hold to zoom
            if (holdToZoom && !isSprinting)
            {
                if (Input.GetKeyDown(zoomKey))
                {
                    isZoomed = true;
                }
                else if (Input.GetKeyUp(zoomKey))
                {
                    isZoomed = false;
                }
            }

            // Lerps camera.fieldOfView to allow for a smooth transistion
            if (isZoomed)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, zoomFOV, zoomStepTime * Time.deltaTime);
            }
            else if (!isZoomed && !isSprinting)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomStepTime * Time.deltaTime);
            }
        }

        #endregion
        #endregion

        #region Sprint

        if (enableSprint)
        {
            if (isSprinting)
            {
                isZoomed = false;

                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, sprintFOV, sprintFOVStepTime * Time.deltaTime);

                // Drain sprint remaining while sprinting
                if (!unlimitedSprint)
                {
                    sprintRemaining -= 1 * Time.deltaTime;
                    if (sprintRemaining <= 0)
                    {
                        isSprinting = false;
                        isSprintCooldown = true;
                    }
                }
                soundEmitter.EmittSound(15);
            }
            else
            {
                // Regain sprint while not sprinting
                sprintRemaining = Mathf.Clamp(sprintRemaining += 1 * Time.deltaTime, 0, sprintDuration);
            }

            // Handles sprint cooldown 
            // When sprint remaining == 0 stops sprint ability until hitting cooldown
            if (isSprintCooldown)
            {
                sprintCooldown -= 1 * Time.deltaTime;
                if (sprintCooldown <= 0)
                {
                    isSprintCooldown = false;
                }
            }
            else
            {
                sprintCooldown = sprintCooldownReset;
            }

            // Handles sprintBar 
            if (useSprintBar && !unlimitedSprint)
            {
                float sprintRemainingPercent = sprintRemaining / sprintDuration;
                sprintBar.fillAmount = sprintRemainingPercent;
            }
        }

        #endregion

        #region Jump

        // Gets input and calls jump method
        if (enableJump && Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
            playerCanMove = false;
        }
        if (isJumping)
        {
            playerCanMove = false;
        }
        else
        {
            playerCanMove = true;
        }
        if (isGrounded)
        {
            isJumping = false;
        }

        #endregion

        #region Crouch

        if (enableCrouch)
        {
            if (Input.GetKeyDown(crouchKey) && !holdToCrouch)
            {
                Crouch();
            }

            if (Input.GetKeyDown(crouchKey) && holdToCrouch)
            {
                isCrouched = false;
                Crouch();
            }
            else if (Input.GetKeyUp(crouchKey) && holdToCrouch)
            {
                isCrouched = true;
                Crouch();
            }
        }

        #endregion

        CheckGround();

        if (enableHeadBob)
        {
            HeadBob();
        }

        // if(Input.GetKey(KeyCode.X)){
        //     Inspect();
        // }


        if (!isSprinting && !isWalking)
        {
            isIdle = true;
        }
        else
        {
            isIdle = false;
        }

        UpdateAnimator();
        #region Push
        if (Input.GetKeyDown(KeyCode.F) && canPush) // Change key as needed
        {
            TryPush();

        }
        #endregion
        if (isGrounded)
        {
            if (isWalking)
            {
                if (!walkSource.isPlaying)
                {
                    sprintSource.Stop();     // stop sprint if it's playing
                    walkSource.Play();
                }
            }
            else if (isSprinting)
            {
                if (!sprintSource.isPlaying)
                {
                    walkSource.Stop();      // stop walk if it's playing
                    sprintSource.Play();
                }
            }
            else
            {
                // if grounded but standing still
                walkSource.Stop();
                sprintSource.Stop();
            }
        }
        else
        {
            // not grounded, stop both
            if (walkSource.isPlaying) walkSource.Stop();
            if (sprintSource.isPlaying) sprintSource.Stop();
        }

        if (isWalking && !isCrouched)
        {
            soundEmitter.EmittSound(8);
        }

    }

    void TryPush()
    {
        canPush = false;
        ChangeAnimation("Push", 0.1f);
        RaycastHit hit;
        Debug.DrawRay(transform.position, transform.forward * pushRange, Color.red, 2f);
        if (Physics.Raycast(transform.position, transform.forward, out hit, pushRange))
        {
            Pushable pushable = hit.collider.GetComponentInParent<Pushable>();
            if (pushable != null)
            {
                Vector3 pushDirection = hit.collider.transform.position - transform.position;
                pushDirection.y = 0; // Ensure no vertical push
                pushDirection.Normalize();

                pushable.CmdApplyPush(pushDirection * pushStrength);
                audioSource.PlayOneShot(push);
                
                
            }
        }
        StartCoroutine(PushColdown());
    }
    IEnumerator PushColdown(){
        yield return new WaitForSeconds(pushDelay);
        canPush = true;
    }

    private void Inspect()
		{
			//State.
			inspecting = true;
			//Play.
			playerAnimator.CrossFade("Inspect", 0.0f, layerActions, 0);
		}
    private void UpdateAnimator()
		{
            if(punchSystem.canPunch && canPush){

                //no gun animations
                if(isIdle && isGrounded && !weaponController.hasGun && !canLand){

                    const string boolNameIdle = "Idle";
                    ChangeAnimation(boolNameIdle, 0.2f);
                } else if(isWalking && isGrounded && !weaponController.hasGun){

                    const string boolNameWalk = "walking";
                    ChangeAnimation(boolNameWalk, 0.2f);
                }else if(isSprinting && isGrounded && !weaponController.hasGun){

                    const string boolNameRun = "NoWeaponRunning";
                    ChangeAnimation(boolNameRun, 0.1f);
                }else if(!isGrounded && !weaponController.hasGun){
                    const string boolNameJump = "NoWeaponAir";
                    ChangeAnimation(boolNameJump, 0.2f);
                }else if(canLand && isGrounded && !weaponController.hasGun){
                    ChangeAnimation("NoWeaponLand", 0.01f);
                    canLand = false;
                }
                // gun animations
                else if(weaponController.hasGun && isIdle && !isZoomed) {//weapon idle animation
                    ChangeAnimation(weaponController.weaponName + "Idle", 0.2f);
                }
                else if(weaponController.hasGun && isWalking && isGrounded && !isZoomed){    //Weapon walk
                    ChangeAnimation(weaponController.weaponName + "Walk", 0.2f);
                }
                else if(weaponController.hasGun && isSprinting && isGrounded && !isZoomed){   //Weapon Run
                    ChangeAnimation(weaponController.weaponName + "Run", 0.2f);
                }else if(weaponController.hasGun && isZoomed && isGrounded){   //Weapon Run
                    ChangeAnimation(weaponController.weaponName + "Aim", 0.01f);
                }
            }
            
            
			// const string boolNameAim = "Aim";
			// ChangeAnimation(boolNameAim, 0.2f);
		}
        
        private void ChangeAnimation(string name, float time = 0.2f, bool body = false){
            if(currAnimation != name){
                CmdChangeAnimation(name, time, body);
            }
        }

    [Command]
    public void CmdChangeAnimation(string name, float time, bool body)
    {
        
        
        RpcChangeAnimation(name, time, body); 
    }

    [ClientRpc]
    void RpcChangeAnimation(string name, float time, bool body){
        Debug.Log(name);
        if (body)
        {
            currAnimation = name;
            
            bodyAnimator.CrossFade(name, time, 2);
        }
        else
        {
            currAnimation = name;
            playerAnimator.CrossFade(name, time, 0);
            string tempHands = name + "Hands";
            handAnimator.CrossFade(tempHands, time, 1);
        }
            
    }
    void FixedUpdate()
    {
        #region Movement
        if(!isLocalPlayer){return;}
        if (playerCanMove)
        {
            // Get movement input using the new Input System
        Vector2 moveInput = Keyboard.current != null
            ? new Vector2(
                Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0,
                Keyboard.current.sKey.isPressed ? -1 : Keyboard.current.wKey.isPressed ? 1 : 0)
            : Vector2.zero;

        // Forward and strafe movement
        Vector3 forwardMovement = transform.forward * moveInput.y;
        Vector3 strafeMovement = transform.right * moveInput.x;

        // Combine movement and normalize
        Vector3 targetVelocity = (forwardMovement + strafeMovement).normalized;

        // Check if the player is walking and grounded (for animations or effects)
        isWalking = (targetVelocity.x != 0 || targetVelocity.z != 0) && isGrounded && !isSprinting;

        // Sprinting logic
        if (enableSprint && Keyboard.current[Key.LeftShift].isPressed && sprintRemaining > 0f && !isSprintCooldown)
        {
            targetVelocity *= sprintSpeed;
            isSprinting = true;
        }
        else
        {
            targetVelocity *= walkSpeed;
            isSprinting = false;
        }

        // Apply movement force
        Vector3 velocity = rb.velocity;
        Vector3 velocityChange = (targetVelocity - velocity);
        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0; // Prevent gravity interference

        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // Only send RPC if animation states change
        if (Mathf.Abs(targetVelocity.magnitude - lastSpeed) > 0.01f || 
            Mathf.Abs(targetVelocity.x - lastStrafe) > 0.01f || 
            lastSprinting != isSprinting || 
            lastWalking != isWalking)
        {
            CmdUpdateAnimation(transform.InverseTransformDirection(targetVelocity).x, transform.InverseTransformDirection(targetVelocity).z, isSprinting, isWalking);
            lastSpeed = targetVelocity.magnitude;
            lastStrafe = targetVelocity.x;
            lastSprinting = isSprinting;
            lastWalking = isWalking;
        }
        }
        #endregion
    }
    [Command]
    void CmdUpdateAnimation(float strafe, float speed, bool sprinting, bool walking)
    {
        RpcUpdateAnimation(strafe, speed, sprinting, walking);
    }

    [ClientRpc]
    void RpcUpdateAnimation(float strafe, float speed, bool sprinting, bool walking)
    {
        if (sprinting)
        {
            bodyAnimator.CrossFade("RunningPlayer", 0.15f, 0);
        }
        else if (walking)
        {
            // Debug.Log("is walking animation" + strafe);
            if (strafe < -0.1f) {
            // Debug.Log("is walking animation called strafe");
                bodyAnimator.CrossFade("LeftStrafePlayer", 0.1f, 0);
            }
            else if (strafe > 0.1f) {
            // Debug.Log("is walking animation called stafe right");
                bodyAnimator.CrossFade("RightStrafePlayer", 0.1f, 0);
            }
            else {

                // Debug.Log("is walking animation called");
                bodyAnimator.CrossFade("WalkingPlayer", 0.1f, 0);
            }
        }
        else
        {
            bodyAnimator.CrossFade("IdlePlayer", 0.2f, 0);
        }
    }
    

    // Sets isGrounded based on a raycast sent straigth down from the player object
    private void CheckGround()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (transform.localScale.y * .5f), transform.position.z);
        Vector3 direction = transform.TransformDirection(Vector3.down);
        float distance = .75f;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            Debug.DrawRay(origin, direction * distance, Color.red);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            canLand = true;
        }
    }

    private void Jump()
    {
        if(!weaponController.hasGun){

            ChangeAnimation("NoWeaponJump", 0.2f);
        }

        // Adds force to the player rigidbody to jump
        if (isGrounded)
        {
            rb.AddForce(0f, jumpPower, 0f, ForceMode.Impulse);
            isGrounded = false;
            isJumping = true;
            audioSource.PlayOneShot(jump);
        }
        soundEmitter.EmittSound(20);
        // When crouched and using toggle system, will uncrouch for a jump
        if(isCrouched && !holdToCrouch)
        {
            Crouch();
        }
    }

    private void Crouch()
    {
        // Stands player up to full height
        // Brings walkSpeed back up to original speed
        if (isCrouched)
        {
            //transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z);
            walkSpeed /= speedReduction;
            ChangeAnimation("standing", 0.2f, true);
            //Vector3 currentPos = playerCamera.transform.localPosition;
            playerCamera.transform.localPosition = new Vector3(originalCameraPos.x, originalCameraPos.y, originalCameraPos.z);
            isCrouched = false;
            audioSource.PlayOneShot(standUp);
        }
        // Crouches player down to set height
        // Reduces walkSpeed
        else
        {
            //transform.localScale = new Vector3(originalScale.x, crouchHeight, originalScale.z);
            walkSpeed *= speedReduction;
            ChangeAnimation("crouch", 0.2f, true);
            //Vector3 currentPos = playerCamera.transform.localPosition;
            playerCamera.transform.localPosition = new Vector3(originalCameraPos.x, -0.45f, 0.35f);
            
            isCrouched = true;
            audioSource.PlayOneShot(crouch);

        }
    }

    private void HeadBob()
    {
        if(isWalking)
        {
            // Calculates HeadBob speed during sprint
            if(isSprinting)
            {
                timer += Time.deltaTime * (bobSpeed + sprintSpeed);
            }
            // Calculates HeadBob speed during crouched movement
            else if (isCrouched)
            {
                timer += Time.deltaTime * (bobSpeed * speedReduction);
            }
            // Calculates HeadBob speed during walking
            else
            {
                timer += Time.deltaTime * bobSpeed;
            }
            // Applies HeadBob movement
            joint.localPosition = new Vector3(jointOriginalPos.x + Mathf.Sin(timer) * bobAmount.x, jointOriginalPos.y + Mathf.Sin(timer) * bobAmount.y, jointOriginalPos.z + Mathf.Sin(timer) * bobAmount.z);
        }
        else
        {
            // Resets when play stops moving
            timer = 0;
            joint.localPosition = new Vector3(Mathf.Lerp(joint.localPosition.x, jointOriginalPos.x, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.y, jointOriginalPos.y, Time.deltaTime * bobSpeed), Mathf.Lerp(joint.localPosition.z, jointOriginalPos.z, Time.deltaTime * bobSpeed));
        }
    }

    
}



// // Custom Editor
// #if UNITY_EDITOR
//     [CustomEditor(typeof(FirstPersonController)), InitializeOnLoadAttribute]
//     public class FirstPersonControllerEditor : Editor
//     {
//     FirstPersonController fpc;
//     SerializedObject SerFPC;

//     private void OnEnable()
//     {
//         fpc = (FirstPersonController)target;
//         SerFPC = new SerializedObject(fpc);
//     }

    // public override void OnInspectorGUI()
    // {
    //     SerFPC.Update();

    //     EditorGUILayout.Space();
    //     GUILayout.Label("Modular First Person Controller", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 16 });
    //     GUILayout.Label("By Jess Case", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
    //     GUILayout.Label("version 1.0.1", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal, fontSize = 12 });
    //     EditorGUILayout.Space();

    //     #region Camera Setup

    //     EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    //     GUILayout.Label("Camera Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
    //     EditorGUILayout.Space();

    //     fpc.playerCamera = (Camera)EditorGUILayout.ObjectField(new GUIContent("Camera", "Camera attached to the controller."), fpc.playerCamera, typeof(Camera), true);
    //     fpc.fov = EditorGUILayout.Slider(new GUIContent("Field of View", "The camera’s view angle. Changes the player camera directly."), fpc.fov, fpc.zoomFOV, 179f);
    //     fpc.cameraCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Camera Rotation", "Determines if the camera is allowed to move."), fpc.cameraCanMove);

    //     GUI.enabled = fpc.cameraCanMove;
    //     fpc.invertCamera = EditorGUILayout.ToggleLeft(new GUIContent("Invert Camera Rotation", "Inverts the up and down movement of the camera."), fpc.invertCamera);
    //     fpc.mouseSensitivity = EditorGUILayout.Slider(new GUIContent("Look Sensitivity", "Determines how sensitive the mouse movement is."), fpc.mouseSensitivity, .1f, 10f);
    //     fpc.maxLookAngle = EditorGUILayout.Slider(new GUIContent("Max Look Angle", "Determines the max and min angle the player camera is able to look."), fpc.maxLookAngle, 40, 90);
    //     GUI.enabled = true;

    //     fpc.lockCursor = EditorGUILayout.ToggleLeft(new GUIContent("Lock and Hide Cursor", "Turns off the cursor visibility and locks it to the middle of the screen."), fpc.lockCursor);

    //     fpc.crosshair = EditorGUILayout.ToggleLeft(new GUIContent("Auto Crosshair", "Determines if the basic crosshair will be turned on, and sets is to the center of the screen."), fpc.crosshair);

    //     // Only displays crosshair options if crosshair is enabled
    //     if(fpc.crosshair) 
    //     { 
    //         EditorGUI.indentLevel++; 
    //         EditorGUILayout.BeginHorizontal(); 
    //         EditorGUILayout.PrefixLabel(new GUIContent("Crosshair Image", "Sprite to use as the crosshair.")); 
    //         fpc.crosshairImage = (Sprite)EditorGUILayout.ObjectField(fpc.crosshairImage, typeof(Sprite), false);
    //         EditorGUILayout.EndHorizontal();

    //         EditorGUILayout.BeginHorizontal();
    //         fpc.crosshairColor = EditorGUILayout.ColorField(new GUIContent("Crosshair Color", "Determines the color of the crosshair."), fpc.crosshairColor);
    //         EditorGUILayout.EndHorizontal();
    //         EditorGUI.indentLevel--; 
    //     }

    //     EditorGUILayout.Space();

    //     #region Camera Zoom Setup

    //     GUILayout.Label("Zoom", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

    //     fpc.enableZoom = EditorGUILayout.ToggleLeft(new GUIContent("Enable Zoom", "Determines if the player is able to zoom in while playing."), fpc.enableZoom);

    //     GUI.enabled = fpc.enableZoom;
    //     fpc.holdToZoom = EditorGUILayout.ToggleLeft(new GUIContent("Hold to Zoom", "Requires the player to hold the zoom key instead if pressing to zoom and unzoom."), fpc.holdToZoom);
    //     fpc.zoomKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Zoom Key", "Determines what key is used to zoom."), fpc.zoomKey);
    //     fpc.zoomFOV = EditorGUILayout.Slider(new GUIContent("Zoom FOV", "Determines the field of view the camera zooms to."), fpc.zoomFOV, .1f, fpc.fov);
    //     fpc.zoomStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while zooming in."), fpc.zoomStepTime, .1f, 10f);
    //     GUI.enabled = true;

    //     #endregion

    //     #endregion

    //     #region Movement Setup

    //     EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    //     GUILayout.Label("Movement Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
    //     EditorGUILayout.Space();

    //     fpc.playerCanMove = EditorGUILayout.ToggleLeft(new GUIContent("Enable Player Movement", "Determines if the player is allowed to move."), fpc.playerCanMove);

    //     GUI.enabled = fpc.playerCanMove;
    //     fpc.walkSpeed = EditorGUILayout.Slider(new GUIContent("Walk Speed", "Determines how fast the player will move while walking."), fpc.walkSpeed, .1f, fpc.sprintSpeed);
    //     GUI.enabled = true;

    //     EditorGUILayout.Space();

    //     #region Sprint

    //     GUILayout.Label("Sprint", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

    //     fpc.enableSprint = EditorGUILayout.ToggleLeft(new GUIContent("Enable Sprint", "Determines if the player is allowed to sprint."), fpc.enableSprint);

    //     GUI.enabled = fpc.enableSprint;
    //     fpc.unlimitedSprint = EditorGUILayout.ToggleLeft(new GUIContent("Unlimited Sprint", "Determines if 'Sprint Duration' is enabled. Turning this on will allow for unlimited sprint."), fpc.unlimitedSprint);
        
    //     fpc.sprintSpeed = EditorGUILayout.Slider(new GUIContent("Sprint Speed", "Determines how fast the player will move while sprinting."), fpc.sprintSpeed, fpc.walkSpeed, 20f);

    //     //GUI.enabled = !fpc.unlimitedSprint;
    //     fpc.sprintDuration = EditorGUILayout.Slider(new GUIContent("Sprint Duration", "Determines how long the player can sprint while unlimited sprint is disabled."), fpc.sprintDuration, 1f, 20f);
    //     fpc.sprintCooldown = EditorGUILayout.Slider(new GUIContent("Sprint Cooldown", "Determines how long the recovery time is when the player runs out of sprint."), fpc.sprintCooldown, .1f, fpc.sprintDuration);
    //     //GUI.enabled = true;

    //     fpc.sprintFOV = EditorGUILayout.Slider(new GUIContent("Sprint FOV", "Determines the field of view the camera changes to while sprinting."), fpc.sprintFOV, fpc.fov, 179f);
    //     fpc.sprintFOVStepTime = EditorGUILayout.Slider(new GUIContent("Step Time", "Determines how fast the FOV transitions while sprinting."), fpc.sprintFOVStepTime, .1f, 20f);

    //     fpc.useSprintBar = EditorGUILayout.ToggleLeft(new GUIContent("Use Sprint Bar", "Determines if the default sprint bar will appear on screen."), fpc.useSprintBar);

    //     // Only displays sprint bar options if sprint bar is enabled
    //     if(fpc.useSprintBar)
    //     {
    //         EditorGUI.indentLevel++;

    //         EditorGUILayout.BeginHorizontal();
    //         fpc.hideBarWhenFull = EditorGUILayout.ToggleLeft(new GUIContent("Hide Full Bar", "Hides the sprint bar when sprint duration is full, and fades the bar in when sprinting. Disabling this will leave the bar on screen at all times when the sprint bar is enabled."), fpc.hideBarWhenFull);
    //         EditorGUILayout.EndHorizontal();

    //         EditorGUILayout.BeginHorizontal();
    //         EditorGUILayout.PrefixLabel(new GUIContent("Bar BG", "Object to be used as sprint bar background."));
    //         fpc.sprintBarBG = (Image)EditorGUILayout.ObjectField(fpc.sprintBarBG, typeof(Image), true);
    //         EditorGUILayout.EndHorizontal();

    //         EditorGUILayout.BeginHorizontal();
    //         EditorGUILayout.PrefixLabel(new GUIContent("Bar", "Object to be used as sprint bar foreground."));
    //         fpc.sprintBar = (Image)EditorGUILayout.ObjectField(fpc.sprintBar, typeof(Image), true);
    //         EditorGUILayout.EndHorizontal();


    //         EditorGUILayout.BeginHorizontal();
    //         fpc.sprintBarWidthPercent = EditorGUILayout.Slider(new GUIContent("Bar Width", "Determines the width of the sprint bar."), fpc.sprintBarWidthPercent, .1f, .5f);
    //         EditorGUILayout.EndHorizontal();

    //         EditorGUILayout.BeginHorizontal();
    //         fpc.sprintBarHeightPercent = EditorGUILayout.Slider(new GUIContent("Bar Height", "Determines the height of the sprint bar."), fpc.sprintBarHeightPercent, .001f, .025f);
    //         EditorGUILayout.EndHorizontal();
    //         EditorGUI.indentLevel--;
    //     }
    //     GUI.enabled = true;

    //     EditorGUILayout.Space();

    //     #endregion

    //     #region Jump

    //     GUILayout.Label("Jump", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

    //     fpc.enableJump = EditorGUILayout.ToggleLeft(new GUIContent("Enable Jump", "Determines if the player is allowed to jump."), fpc.enableJump);

    //     GUI.enabled = fpc.enableJump;
    //     fpc.jumpKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Jump Key", "Determines what key is used to jump."), fpc.jumpKey);
    //     fpc.jumpPower = EditorGUILayout.Slider(new GUIContent("Jump Power", "Determines how high the player will jump."), fpc.jumpPower, .1f, 20f);
    //     GUI.enabled = true;

    //     EditorGUILayout.Space();

    //     #endregion

    //     #region Crouch

    //     GUILayout.Label("Crouch", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));

    //     fpc.enableCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Enable Crouch", "Determines if the player is allowed to crouch."), fpc.enableCrouch);

    //     GUI.enabled = fpc.enableCrouch;
    //     fpc.holdToCrouch = EditorGUILayout.ToggleLeft(new GUIContent("Hold To Crouch", "Requires the player to hold the crouch key instead if pressing to crouch and uncrouch."), fpc.holdToCrouch);
    //     fpc.crouchKey = (KeyCode)EditorGUILayout.EnumPopup(new GUIContent("Crouch Key", "Determines what key is used to crouch."), fpc.crouchKey);
    //     fpc.crouchHeight = EditorGUILayout.Slider(new GUIContent("Crouch Height", "Determines the y scale of the player object when crouched."), fpc.crouchHeight, .1f, 1);
    //     fpc.speedReduction = EditorGUILayout.Slider(new GUIContent("Speed Reduction", "Determines the percent 'Walk Speed' is reduced by. 1 being no reduction, and .5 being half."), fpc.speedReduction, .1f, 1);
    //     GUI.enabled = true;

    //     #endregion

    //     #endregion

    //     #region Head Bob

    //     EditorGUILayout.Space();
    //     EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    //     GUILayout.Label("Head Bob Setup", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 }, GUILayout.ExpandWidth(true));
    //     EditorGUILayout.Space();

    //     fpc.enableHeadBob = EditorGUILayout.ToggleLeft(new GUIContent("Enable Head Bob", "Determines if the camera will bob while the player is walking."), fpc.enableHeadBob);
        

    //     GUI.enabled = fpc.enableHeadBob;
    //     fpc.joint = (Transform)EditorGUILayout.ObjectField(new GUIContent("Camera Joint", "Joint object position is moved while head bob is active."), fpc.joint, typeof(Transform), true);
    //     fpc.bobSpeed = EditorGUILayout.Slider(new GUIContent("Speed", "Determines how often a bob rotation is completed."), fpc.bobSpeed, 1, 20);
    //     fpc.bobAmount = EditorGUILayout.Vector3Field(new GUIContent("Bob Amount", "Determines the amount the joint moves in both directions on every axes."), fpc.bobAmount);
    //     GUI.enabled = true;

    //     #endregion

    //     //Sets any changes from the prefab
    //     if(GUI.changed)
    //     {
    //         EditorUtility.SetDirty(fpc);
    //         Undo.RecordObject(fpc, "FPC Change");
    //         SerFPC.ApplyModifiedProperties();
    //     }
    // }

// }

// #endif