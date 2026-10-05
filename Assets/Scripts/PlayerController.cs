using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [Header("Collision Checks")]
    public Transform GroundCheck;
    public Transform[] SinkChecks;
    public Transform NextPositionCheck;
    private Vector2 inputVector = Vector2.zero;
    private Vector2 horizontalVelocity = Vector2.zero;
    private float verticalVelocity = 0f;

    [Header("Horizontal Movement")]
    [SerializeField] private float acceleration = 2; // units per second
    [SerializeField] private float maxVelocity = 10; // units per second
    [SerializeField] private float friction = 2; // units per second
    [SerializeField] private float wallCollisionRadius = 0.4f;

    [Header("Vertical Movement")]
    [SerializeField] private float gravity = 10; // units per second
    [SerializeField] private float maxFallSpeed = 50;
    [SerializeField] private float jumpStrength = 50;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Debug Visibility")]
    [SerializeField] private float magnitude = 0f;
    [SerializeField] private bool moving = false;
    [SerializeField] private bool grounded = false;

    // Update is called once per frame
    void Update()
    {
        float delta = Time.deltaTime;

        // adjust the velocity based on the input vector, acceleration and delta
        horizontalVelocity += inputVector * acceleration * delta; 

        if (horizontalVelocity.magnitude > maxVelocity) horizontalVelocity = horizontalVelocity.normalized * maxVelocity; // cap velocity to the max

        // apply friction
        horizontalVelocity -= horizontalVelocity.normalized * friction * delta;
        if (horizontalVelocity.magnitude < friction*0.05f && !moving) horizontalVelocity = Vector2.zero; // set velocity to zero when "basically" stopped with no player input

        // check if player is on the ground by checking collisions with their feet 
        grounded = Physics.CheckSphere(GroundCheck.position, groundCheckRadius, groundLayer);

        // apply gravity if player isn't grounded
        if (grounded) verticalVelocity = Mathf.Max(0f, verticalVelocity);
        else verticalVelocity -= gravity * delta;

        // Prevent falling velocity from going beyond the max fall speed
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);

        // change position based on the current velocity
        Vector3 position = transform.position;
        Vector3 velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.y);

        // fancy little methods used to stop the player from going into walls
        velocity = WallDetection(position, velocity, delta);
        FixWallSticking(position);

        // after checking that the player won't go into a wall / isn't already in a wall, move the player according to the velocity
        position += velocity * delta;
        transform.position = position;

        // sometimes the player can fall through the floor if the delta time is too big when falling
        // this function basically checks if they're sunk into the floor and pushes them back on top of it every frame
        foreach(Transform SinkCheck in SinkChecks)
            if (Physics.CheckSphere(SinkCheck.position, groundCheckRadius, groundLayer)) FixFloorSinking(SinkCheck);

        // show magnitude for debug purposes
        magnitude = velocity.magnitude;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>().normalized; // input vector will be set to the normalized vector of input

        if (context.started) moving = true;
        else if (context.canceled) moving = false;
    }

    public void OnJump(InputAction.CallbackContext context) 
    {
        if (context.started && grounded)
        {
            verticalVelocity += jumpStrength;
            grounded = false;
            Debug.Log("Jump activated!");
        }
    }

    // This method is called if the player is detected to be inside of an object and just puts them on top of it if so
    private void FixFloorSinking(Transform SinkCheck) 
    {
        Debug.Log("Fixing floor sinking");

        Collider[] hitColliders = Physics.OverlapSphere(SinkCheck.position, groundCheckRadius, groundLayer);

        Collider groundCollider = hitColliders[0];

        float sinkDepth = groundCollider.bounds.max.y - GroundCheck.position.y;

        if (sinkDepth > 0)
        {
            Vector3 newPosition = transform.position;
            newPosition.y += sinkDepth;
            transform.position = newPosition;
        }
    }

    // This method is checked when the player is trying to move and checks each axis of movement of their next intended position
    // For each axis of movement, if moving in that direction/distance would result in a collision, nullify that specific axis (this lets you slide along walls)
    private Vector3 WallDetection(Vector3 position, Vector3 velocity, float delta) 
    {
        NextPositionCheck.position = position + new Vector3(horizontalVelocity.x, 0f, 0f) * delta;

        if (Physics.CheckSphere(NextPositionCheck.position, wallCollisionRadius, groundLayer))
        {
            velocity.x = 0f;
            horizontalVelocity.x = 0f;
        }

        NextPositionCheck.position = position + new Vector3(0f, 0f, horizontalVelocity.y) * delta;

        if (Physics.CheckSphere(NextPositionCheck.position, wallCollisionRadius, groundLayer))
        {
            velocity.z = 0f;
            horizontalVelocity.y = 0f;
        }

        NextPositionCheck.position = position + new Vector3(0f, verticalVelocity, 0f) * delta;

        if (Physics.CheckSphere(NextPositionCheck.position, wallCollisionRadius, groundLayer))
        {
            velocity.y = 0f;
            verticalVelocity = 0f;
        }

        return velocity;
    }

    // This method checks for overlapping colliders and pushes the player away from them slightly to prevent getting stuck
    // (This might seem redundant because of the above method but it's not, you could still get stuck on walls)
    private Vector3 FixWallSticking(Vector3 position)
    {
        Collider[] overlappedColliders = Physics.OverlapSphere(position, wallCollisionRadius, groundLayer);

        foreach (Collider wallCollider in overlappedColliders)
        {
            if (wallCollider.isTrigger) continue;

            Vector3 pushDirection;
            float pushDistance;

            bool isOverlapping = Physics.ComputePenetration(
                this.GetComponent<Collider>(), position, transform.rotation, 
                wallCollider, wallCollider.transform.position, wallCollider.transform.rotation,
                out pushDirection, out pushDistance
            );

            if (isOverlapping)
            {
                Vector3 separationVector = pushDirection * (pushDistance + 5f);
                transform.position += separationVector;

                position += separationVector;
            }
        }

        return position;
    }

    private void OnDrawGizmos()
    {
        if (GroundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GroundCheck.position, groundCheckRadius);
        }

        if (SinkChecks.Length >= 0)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform SinkCheck in SinkChecks) 
            {
                Gizmos.DrawSphere(SinkCheck.position, groundCheckRadius);
            }
        }

        if (NextPositionCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(NextPositionCheck.position, wallCollisionRadius);
        }
    }
}
