using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float acceleration = 2; // units per second
    [SerializeField] private float maxVelocity = 10; // units per second
    [SerializeField] private float friction = 2;
    private Vector2 inputVector = Vector2.zero;
    private Vector2 velocity = Vector2.zero;

    [SerializeField] private float magnitude = 0f;
    [SerializeField] private bool moving = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float delta = Time.deltaTime;

        velocity += inputVector * acceleration * delta; // adjust the velocity based on the input vector, acceleration and delta

        if (velocity.magnitude > maxVelocity) velocity = velocity.normalized * maxVelocity; // cap velocity to the max

        // apply friction
        velocity -= velocity.normalized * friction * delta;
        if (velocity.magnitude < friction*0.05f && !moving) velocity = Vector2.zero; // set velocity to zero when "basically" stopped with no player input

        // change position based on the current velocity:
        Vector3 position = transform.position;
        position.x += velocity.x * delta;
        position.z += velocity.y * delta;
        transform.position = position;

        magnitude = velocity.magnitude;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        inputVector = context.ReadValue<Vector2>().normalized; // input vector will be set to the normalized vector of input

        if (context.started) moving = true;
        else if (context.canceled) moving = false;
    }
}
