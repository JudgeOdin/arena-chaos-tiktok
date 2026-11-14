using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float arenaLimit = 7.4f;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;

        Vector3 movement = new Vector3(input.x, 0f, input.y).normalized;
        transform.position += movement * speed * Time.deltaTime;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -arenaLimit, arenaLimit),
            transform.position.y,
            Mathf.Clamp(transform.position.z, -arenaLimit, arenaLimit));
    }
}
