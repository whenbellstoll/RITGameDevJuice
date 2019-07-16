using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
	public ShakeType ShakeFunction = ShakeType.Classic;

	public Transform JonassonPoint;

	public enum ShakeType
	{
		None,
		Classic,
		Jonasson
	}

	private abstract class ShakeHandler
	{
		protected Camera target;

		protected float intensity;
		protected float speed;
		protected float decay;
		protected Vector2 direction = Vector2.one;

		public Vector3 Offset { get; protected set; }

		public ShakeHandler(Camera target)
		{
			this.target = target;
		}

		public virtual void Start(float intensity, float speed, float decay, Vector2 direction = default)
		{
			this.intensity = intensity;
			this.speed = speed;
			this.decay = decay;

			if (direction == default)
			{
				direction = Random.onUnitSphere; // random direction for shake
			}

			this.direction = direction;
		}

		public virtual void Update(float deltaTime)
		{
			// a variation on x += (target - x) * 0.1;
			intensity -= intensity * decay;
		}
	}

	/// <summary>
	/// Shake in a random direction with some intensity
	/// </summary>
	private class Classic : ShakeHandler
	{
		public Classic(Camera target) : base(target) { }

		private float phase = 0f;

		public override void Start(float intensity, float speed, float decay, Vector2 direction = default)
		{
			phase = 0f;
			base.Start(intensity, speed, decay, direction);
		}

		public override void Update(float deltaTime)
		{
			Offset = direction * Mathf.Sin(phase) * intensity;

			phase += deltaTime * speed * 2f * Mathf.PI;
			phase %= 2f * Mathf.PI;

			base.Update(deltaTime);
		}
	}

	/// <summary>
	/// 3d wobble-style shake
	/// https://twitter.com/grapefrukt/status/1035978262328090625
	/// </summary>
	private class Jonasson : ShakeHandler
	{
		public Quaternion RotationOffset;

		private Vector3 lookPoint;

		/// <summary>
		/// Used to shake the point
		/// </summary>
		private Classic classicShake;

		public Jonasson(Camera target, Transform point) : base(target)
		{
			this.lookPoint = point.position;
			classicShake = new Classic(target);
		}

		public override void Update(float deltaTime)
		{
			// shake point
			classicShake.Update(deltaTime);
			Offset = -classicShake.Offset;

			// look at offset
			Vector3 lookAt = lookPoint + classicShake.Offset;
			RotationOffset = Quaternion.LookRotation(
				forward: lookAt - target.transform.position
			);

			base.Update(deltaTime);
		}

		public override void Start(float intensity, float speed, float decay, Vector2 directon = default)
		{
			classicShake.Start(intensity, speed, decay, direction);
			base.Start(intensity, speed, decay);
		}
	}

	[Range(0f, 1f)]
	public float BallFollowAmount;

	new private Camera camera;
	private Classic classic;
	private Jonasson jonasson;

	private Vector3 originalPos;
	private Quaternion originalRot;

	private Vector3 shakePosOffset;
	private Quaternion shakeRotOffset;

	private Vector2 followOffset;

	private GameManager manager => GameManager.Instance;

	private void Awake()
	{
		originalPos = transform.position;
		originalRot = transform.rotation;

		camera = GetComponent<Camera>();
		classic = new Classic(camera);
		jonasson = new Jonasson(camera, JonassonPoint);
	}

	private void Update()
	{
		UpdateShake();
		UpdateFollow();

		transform.position = originalPos + shakePosOffset + (Vector3)followOffset;
		transform.rotation = shakeRotOffset * originalRot;
	}

	private void UpdateShake()
	{
		shakePosOffset = Vector3.zero;
		shakeRotOffset = Quaternion.identity;
		switch (ShakeFunction)
		{
			case ShakeType.Classic:
				classic.Update(Time.deltaTime);
				shakePosOffset = classic.Offset;
				break;
			case ShakeType.Jonasson:
				jonasson.Update(Time.deltaTime);
				shakeRotOffset = jonasson.RotationOffset;
				shakePosOffset = jonasson.Offset;
				break;
			case ShakeType.None:
				break;
		}
	}

	/// <summary>
	/// Follow the ball a little bit
	/// </summary>
	private void UpdateFollow()
	{
		if (manager.CameraFollow)
		{
			followOffset = manager.Balls[0].transform.position - originalPos;
			followOffset *= BallFollowAmount;
		}
		else
		{
			followOffset = Vector3.zero;
		}
	}

	public void Shake(float intensity, float speed, float decay, Vector2 direction = default)
	{
		switch (ShakeFunction)
		{
			case ShakeType.Classic:
				classic.Start(intensity, speed, decay, direction);
				break;
			case ShakeType.Jonasson:
				jonasson.Start(intensity, speed, decay);
				break;
		}
	}
}