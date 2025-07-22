using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteMove : MonoBehaviour
{
    public float speed = 5f;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 15f); 
    }

    // Update is called once per frame
    void Update()
    {
        // Di chuyển NGƯỢC lại hướng Z local của Note (Z-)
        transform.Translate(-transform.forward * speed * Time.deltaTime, Space.World);
    }
}
