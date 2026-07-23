using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private int countdown;
    private GameControls controls;
    public float moveSpeed = 5f;
    public TextMeshPro display;
     

    void Awake()
    {
        controls = new GameControls();
        countdown = 100;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    public int getCountdown()
    {
        return countdown;
    }

    public void decreaseCountdown()
    {
        countdown--;
    }

    void Update()
    {
        display.text = "" + countdown;
        Vector2 movement = controls.Player.Move.ReadValue<Vector2>();

                transform.position += new Vector3(
            movement.x,
            movement.y,
            0f
        ) * moveSpeed * Time.deltaTime;
        Debug.Log(countdown);
    }

}