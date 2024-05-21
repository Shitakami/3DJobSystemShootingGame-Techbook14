using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // マウスの移動を取得
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 垂直方向（X軸）の回転を制御
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 45f);

        // 水平方向（Y軸）の回転を制御
        yRotation += mouseX;
        
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}
