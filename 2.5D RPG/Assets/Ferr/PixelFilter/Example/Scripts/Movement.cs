using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ferr.Example {
	public class Movement : MonoBehaviour {
		[SerializeField] Vector3 _speed    = new Vector3(1,1,1);
		[SerializeField] Vector3 _distance = new Vector3(1,1,1);
		[SerializeField] bool    _sin      = false;
		
		Vector3 _start;
		
		void Start () {
			_start = transform.position;
		}
		
		void Update () {
			float time = Time.time * Mathf.PI;
			
			if (_sin) {
				transform.position = new Vector3(
					_start.x + Mathf.Sin(time*_speed.x)*_distance.x, 
					_start.y + Mathf.Sin(time*_speed.y)*_distance.y, 
					_start.z + Mathf.Sin(time*_speed.z)*_distance.z);
			} else {
				transform.position = new Vector3(
					_start.x + Mathf.Cos(time*_speed.x)*_distance.x, 
					_start.y + Mathf.Cos(time*_speed.y)*_distance.y, 
					_start.z + Mathf.Cos(time*_speed.z)*_distance.z);
			}
		}
	}
}