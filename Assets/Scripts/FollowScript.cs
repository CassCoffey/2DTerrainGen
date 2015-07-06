using UnityEngine;
using System.Collections;

public class FollowScript : MonoBehaviour {

    public GameObject subject;
    public float zPos;
    public float yAdd;
    public float xAdd;

	void Update () {
        transform.position = new Vector3(subject.transform.position.x + xAdd, subject.transform.position.y + yAdd, zPos);
	}
}
