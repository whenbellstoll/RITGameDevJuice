using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class PixelFilter : MonoBehaviour
{
	public RawImage Renderer;
	public int PixelsHeight;

	new private Camera camera;
	private RenderTexture rt;

	private void Awake()
	{
		camera = GetComponent<Camera>();

		rt = new RenderTexture(
			height: PixelsHeight,
			width: Mathf.RoundToInt(camera.aspect * PixelsHeight),
			depth: 16
		);
		rt.filterMode = FilterMode.Point;

		camera.targetTexture = rt;
		Renderer.texture = rt;
	}
}