using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    // INIT vars
    public GameObject player;
    private float smooth_time = 0.3f;
    private Vector3 offset;

    public int distance_from_player;
    public int height_from_player;

    public bool inChat = false;

    public Vector3 goalPos;
    private Vector3 velocity = Vector3.zero;
    private Vector3 up;

    // set rotation for viewing angle
    public int rotation = 0;

	// Use this for initialization
	void Start () {
        // get offset from player pos to camera
        offset = transform.position - player.transform.position;
        // set up var
        up = transform.up;
	}

    private void Update()
    {

    }

    void LateUpdate()
    {
        goalPos = player.transform.position;
        goalPos.y = player.transform.position.y + height_from_player;
        goalPos.z -= distance_from_player;
        transform.position = goalPos;
        Vector3 relative_pos = player.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(relative_pos);
        //transform.position = player.transform.position + offset;
    }
}
