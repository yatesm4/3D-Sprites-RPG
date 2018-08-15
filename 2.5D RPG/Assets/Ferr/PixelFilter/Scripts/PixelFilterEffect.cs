using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ferr {
	[RequireComponent(typeof(Camera)), ExecuteInEditMode, AddComponentMenu("Ferr/Pixel/Pixel Filter Effect")]
	public class PixelFilterEffect : MonoBehaviour {
		#region Supporting Types
		enum SizeMethod {
			ScreenVerticalPixels,
			ScreenHorizontalPixels,
			PixelSize,
		}
		#endregion
		
		#region Static Fields/Properties
		// for PixelSnapper to get the active camera's pixel snapping values
		static Vector3 sSnapAxes;
		static Camera  sActiveCamera;
		static Dictionary<Camera, PixelFilterEffect> sEffectLookup = new Dictionary<Camera, PixelFilterEffect>();
		/// <summary>
		/// Gets the pixel filter snapping increments for the active camera! If there is no pixel filter on
		/// the camera, then (0,0,0) is returned.
		/// </summary>
		public static Vector3 SnapAxes {
			get {
				if (sActiveCamera == Camera.current)
					return sSnapAxes;
				else {
					sActiveCamera = Camera.current;
					
					// get the effect, cache it in a lookup to prevent memory allocations
					PixelFilterEffect effect;
					if (!sEffectLookup.TryGetValue(sActiveCamera, out effect)) {
						effect = sActiveCamera.GetComponent<PixelFilterEffect>();
						sEffectLookup.Add(sActiveCamera, effect);
					}
					
					if (effect != null && effect.enabled) {
						sSnapAxes = effect._snapAxes;
					} else {
						sSnapAxes = Vector3.zero;
					}
					return sSnapAxes;
				}
			}
		}
		#endregion
		
		#region Inspector Values
		[Header("Pixel Sizing")]
		[Tooltip("How Ferr Pixel will calculate the number of pixels to use for width & height")]
		[SerializeField] SizeMethod _sizeMethod = SizeMethod.ScreenVerticalPixels;
		[Tooltip("Number of pixels related to the Size Method!")]
		[SerializeField] int        _sizeValue  = 240;
		
		[Header("Pixel Colors")]
		[Tooltip("Draws everything to a 16bit color buffer, reduces the number of possible colors. Not supported on all platforms, will fall back to default if not available.")]
		[HideInInspector]
		[SerializeField] bool _16BitColor = false;
		
		[Header("Snap Info")]
		[Tooltip("Should the camera snap to pixel increments? This can help reduce flickering artifacts by ensuring the same pixels are selected every frame.")]
		[SerializeField] bool  _useSnapping = false;
		[Tooltip("For perspective cameras only! What part of the view frustum will Ferr Pixel use to calculate pixel snapping on? This should be the z distance from your camera to the main scene content.")]
		[SerializeField] float _snapPlane   = 5;
		#endregion
		
		#region Private Fields
		Camera              _camera;
		RenderTexture       _offscreenPixelSurface;
		RenderTextureFormat _offscreenFormat;
		Vector3             _snapAxes;
		Material            _postMaterial;
		int _renderWidth  = -1;
		int _renderHeight = -1;
		
		Vector3       _oldPosition;
		Rect          _oldViewport;
		RenderTexture _oldSurface;
		
		// for updating in the editor
		Vector2 _screenPrev;
		#endregion
		
		#region Public Properties
		/// <summary>
		/// For perspective cameras only! What part of the view frustum will Ferr Pixel use to calculate pixel 
		/// snapping on? This should be the z distance from your camera to the main scene content.
		/// </summary>
		public float SnapPlane {
			get{ return _snapPlane; }
			set{ if (_snapPlane != value) {
				_snapPlane = value;
				RecalculateSettings();
			}}
		}
		#endregion
		
		#region Unity Events
		void OnEnable() {
			_camera = GetComponent<Camera>();
			
			if (_postMaterial == null) {
				_postMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
				_postMaterial.hideFlags = HideFlags.DontSave;
			}
			
			RecalculateSettings();
		}
		
		void OnDisable() {
			RenderTexture.ReleaseTemporary(_offscreenPixelSurface);
			_offscreenPixelSurface = null;
		}
		
		void OnValidate() {
			RecalculateSettings();
		}
		
		void Update() {
			// check if the screen resolution has changed, can happen in editor and on mobile device portrait->landscape
			if (Screen.width != _screenPrev.x || Screen.height != _screenPrev.y) {
				RecalculateSettings();
			}
		}
		
		void OnPreRender() {
			// switch the camera to render to the smaller texture right off the bat
			_oldSurface           = _camera.targetTexture;
			_camera.targetTexture = _offscreenPixelSurface;
			
			// do any pixel snapping! Mostly for 2D stuff.
			_oldPosition = transform.position;
			if (_useSnapping) {
				Vector3 newPosition = Vector3.zero;
				if (_snapAxes.x != 0) newPosition.x = (_oldPosition.x % _snapAxes.x) - _snapAxes.x/2f;
				if (_snapAxes.y != 0) newPosition.y = (_oldPosition.y % _snapAxes.y) - _snapAxes.y/2f;
				if (_snapAxes.z != 0) newPosition.z =  _oldPosition.z % _snapAxes.z;
				transform.position = _oldPosition - newPosition;
			}
			
			// don't double use the viewport!
			_oldViewport = _camera.rect;
			_camera.rect = new Rect(0,0,1,1);
		}
		
		void OnPostRender() {
			// reset the target so we're drawing to the proper surface
			_camera.targetTexture = _oldSurface;
			// and reset the viewport, so we're drawing to the player's viewport settings!
			_camera.rect          = _oldViewport;
			// and put back any pixel snapping we did in PreRender
			transform.position    = _oldPosition;
			
			// ensure we're rendering to the right place!
			Graphics.SetRenderTarget((RenderTexture)null);
			
			// unfortunately, Graphics.Blit doesn't seem to properly obey the viewport!
			GL.Viewport(_camera.pixelRect);
			GL.PushMatrix();
			_postMaterial.SetPass(0);
			GL.LoadOrtho();
			GL.Begin(GL.QUADS);
			GL.TexCoord2(0,0);
			GL.Vertex3(0, 0, 0);
			GL.TexCoord2(0,1);
			GL.Vertex3(0, 1, 0);
			GL.TexCoord2(1,1);
			GL.Vertex3(1, 1, 0);
			GL.TexCoord2(1,0);
			GL.Vertex3(1, 0, 0);
			GL.End();
			GL.PopMatrix();
		}
		#endregion
		
		#region Public Interface
		/// <summary>
		/// This will recalculate offscreen surface size, as well as snap settings. You shouldn't need to call this manually, except in 
		/// cases where internal data is no longer correct because of outside changes, such as switching the camera from perspective to
		/// orthographic during runtime.
		/// </summary>
		public void RecalculateSettings() {
			if (_camera) {
				CalcScreen  ();
				CalcSnapAxes();
			}
			
			// get the proper color format for the surface
			_offscreenFormat = RenderTextureFormat.Default;
			if (_16BitColor) {
				if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565))
					_offscreenFormat = RenderTextureFormat.RGB565;
				else
					Debug.Log("No support for texture format RGB565! Using default instead.");
			}
			
			_screenPrev.x = Screen.width;
			_screenPrev.y = Screen.height;
		}
		#endregion
		
		#region Private Methods
		void CalcScreen() {
			int                 oldWidth  = _renderWidth;
			int                 oldHeight = _renderHeight;
			RenderTextureFormat oldFormat = _offscreenPixelSurface == null ? RenderTextureFormat.RInt : _offscreenPixelSurface.format;
			
			if (_sizeMethod == SizeMethod.PixelSize) {
				_renderWidth  = (int)(_camera.pixelWidth )/_sizeValue;
				_renderHeight = (int)(_camera.pixelHeight)/_sizeValue;
			} else if (_sizeMethod == SizeMethod.ScreenVerticalPixels) {
				float pixelSize = (float)_camera.pixelHeight / _sizeValue;
				_renderHeight = _sizeValue;
				_renderWidth  = (int)(_camera.pixelWidth / pixelSize);
			} else if (_sizeMethod == SizeMethod.ScreenHorizontalPixels) {
				float pixelSize = (float)_camera.pixelWidth / _sizeValue;
				_renderWidth  = _sizeValue;
				_renderHeight = (int)(_camera.pixelHeight / pixelSize);
			}
			_renderHeight = Mathf.Max(2,_renderHeight);
			_renderWidth  = Mathf.Max(2,_renderWidth);
			
			if (oldWidth != _renderWidth || oldHeight != _renderHeight || oldFormat != _offscreenFormat || _offscreenPixelSurface == null) {
				if (_offscreenPixelSurface != null)
					RenderTexture.ReleaseTemporary(_offscreenPixelSurface);
				
				_offscreenPixelSurface = RenderTexture.GetTemporary(_renderWidth, _renderHeight, 24, _offscreenFormat);
				_offscreenPixelSurface.filterMode = FilterMode.Point;
				_postMaterial.mainTexture = _offscreenPixelSurface;
			}
		}
		
		void CalcSnapAxes() {
			// if we're not snapping, set it all to zero and ditch out early!
			if (!_useSnapping) {
				_snapAxes = Vector3.zero;
				return;
			}
			
			float frustumHeight = 1;
			float frustumWidth  = 1;
			if (_camera.orthographic) {
				frustumHeight = _camera.orthographicSize * 2;
				frustumWidth  = frustumHeight * _camera.aspect;
			} else {
				// Equations from here: https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html
				frustumHeight = 2.0f * _snapPlane * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
				frustumWidth  = frustumHeight * _camera.aspect;
			}
			
			_snapAxes = new Vector3( frustumWidth / _renderWidth, frustumHeight / _renderHeight, 0 );
		}
		#endregion
		
		#region Editor Visualization
		void OnDrawGizmosSelected() {
			if (_useSnapping) {
				Gizmos.color = new Color(0.17f, 0.61f, 0.72f);
				
				Vector3 fwd   = (transform.forward.z > 0 ? 1 : -1) * Vector3.forward;
				Vector3 pt    = transform.position + fwd * _snapPlane;
				Vector3 up    = Vector3.up;
				Vector3 right = Vector3.right;
				float   size  = 0.3f;
				
				Gizmos.DrawLine(transform.position, pt);
				Gizmos.DrawLine(pt-up*size, pt+up*size);
				Gizmos.DrawLine(pt-right*size, pt+right*size);
			}
		}
		#endregion
	}
}
