using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ferr {
	[AddComponentMenu("Ferr/Pixel/Pixel Snapper"), ExecuteInEditMode, RequireComponent(typeof(Renderer))]
	public class PixelSnapper : MonoBehaviour {
		#region Private Fields
		Vector3 _oldPosition;
		bool    _visible;
		#endregion
		
		#region Unity Events
		// force the enable/disable checkbox in the inspector
		void OnEnable() { }
		
		// track the object's visibility so we don't update position when offscreen
		void OnBecameVisible  () {_visible = true;}
		void OnBecameInvisible() {_visible = false;}
		
		void OnWillRenderObject() {
			if (!_visible)
				return;
			
			Transform t = transform;
			
			_oldPosition = t.position;
			Vector3 newPosition = Vector3.zero;
			Vector3 snapAxes    = PixelFilterEffect.SnapAxes;
			
			if (snapAxes.x != 0) newPosition.x = (_oldPosition.x % snapAxes.x) - snapAxes.x/2f;
			if (snapAxes.y != 0) newPosition.y = (_oldPosition.y % snapAxes.y) - snapAxes.y/2f;
			if (snapAxes.z != 0) newPosition.z =  _oldPosition.z % snapAxes.z;
			t.position = _oldPosition - newPosition;
		}
		
		void OnRenderObject() {
			if (!_visible)
				return;
			
			transform.position = _oldPosition;
		}
		#endregion
	}
}