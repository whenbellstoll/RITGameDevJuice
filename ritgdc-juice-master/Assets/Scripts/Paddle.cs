using UnityEngine;

public class Paddle : MonoBehaviour
{
	public SpriteRenderer SpriteRenderer;

	[Range(0.01f, 1f)]
	public float PosLerpSpeed = 1f;

	private Vector2 size;
	private GameManager manager => GameManager.Instance;

	private Vector3 targetPos;
	private AudioSource audioSource;

	private void Awake()
	{
		size = transform.Find("Sprite").localScale;
		audioSource = GetComponent<AudioSource>();
	}

	private void Update()
	{
		FollowMouse();
		LerpToPos();
		Squish();
	}

	/// <summary>
	/// Make our position the same as the mouse X position in world coordinates
	/// </summary>
	private void FollowMouse()
	{
		// get mouse pos in world space
		Vector3 mousePos = Input.mousePosition;
		mousePos.z = -manager.MouseCamera.transform.position.z;
		Vector2 worldMousePos = manager.MouseCamera.ScreenToWorldPoint(mousePos);

		// set our x position to mouse x, clamped to field dimensions
		Vector3 position = transform.position;
		position.x = Mathf.Clamp(
			value: worldMousePos.x,
			min: (-manager.FieldWidth + size.x) / 2f,
			max: (manager.FieldWidth - size.x) / 2f
		);
		targetPos = position;
	}

	/// <summary>
	/// Move to <see cref="targetPos"/> with a smooth animation
	/// </summary>
	private void LerpToPos()
	{
		if (!manager.PaddleLerp)
		{
			transform.position = targetPos;
			return;
		}

		// original formula: x += (target - x) * .1;
		// source: Martin Jonasson & Petri Purho
		transform.position += (targetPos - transform.position) * PosLerpSpeed;

		// you can also use lerp, but it behaves a little different
		//transform.position = Vector3.Lerp(transform.position, targetPos, PosLerpSpeed);
	}

	/// <summary>
	/// Stretch according to the distance between the target position and our 
	/// current position
	/// </summary>
	private void Squish()
	{
		if (!manager.PaddleSquish) return;

		Vector3 scale = Vector3.one;

		// get absolute distance between target and current
		scale.x += Mathf.Abs(transform.position.x - targetPos.x) / 2f;

		// y scale as inverse of x (to preserve the paddle's volume)
		scale.y = 1f / scale.x;

		transform.localScale = scale;
	}

	public void OnBallHit()
	{
		if (manager.PaddleSFX)
		{
			audioSource.PlayOneShot(audioSource.clip);
		}
	}
}
