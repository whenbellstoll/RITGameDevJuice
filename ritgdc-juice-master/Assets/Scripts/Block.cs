using UnityEngine;

public class Block : MonoBehaviour
{
	public int Value;
	public SpriteRenderer SpriteRenderer;
	public Collider2D Collider;
	public ParticleSystem ParticleSystem;

	public float EaseInDuration = 1f;
	public float EaseInDelay = 0f;
	public EasingFunction.Ease EaseFunc;

	public int MinParticles = 10;
	public int MaxParticles = 30;

	[Range(0f, 1f)]
	public float BallVelocityIntensity = 0.2f;

	public float PitchRange = 0.2f;

	private Vector3 startPos;

	private GameManager manager => GameManager.Instance;
	private AudioSource audioSource;

	private EasingFunction.Function ease;
	private float easeInTime;

	private struct ShakeParams
	{
		public float Amp;
		public float Phase;
		public float Decay;
		public float Speed;
	}

	private ShakeParams shake;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	private void Start()
	{
		startPos = transform.position;
	}

	private void OnEnable()
	{
		ease = EasingFunction.GetEasingFunction(EaseFunc);
		easeInTime = -EaseInDelay;
	}

	private void Update()
	{
		UpdateEasing();
		UpdateShake();
	}

	/// <summary>
	/// Animate block scale to make it zoom into place
	/// </summary>
	private void UpdateEasing()
	{
		if (!manager.EaseInBlocks) return;

		if (easeInTime >= EaseInDuration) return;

		easeInTime += Time.deltaTime;

		float t = Mathf.Clamp(easeInTime / EaseInDuration, 0f, 1f);

		float scale = transform.localScale.magnitude;
		scale = ease(0f, 1f, t);

		transform.localScale = Vector3.one * scale;
	}

	private void UpdateShake()
	{
		if (shake.Amp == 0f) return;

		Vector3 shakeOffset = Vector2.one * Mathf.Sin(shake.Phase) * shake.Amp;

		shake.Phase += Time.deltaTime * shake.Speed * 2f * Mathf.PI;
		shake.Phase %= 2f * Mathf.PI;

		// a variation on x += (target - x) * 0.1;
		shake.Amp -= shake.Amp * shake.Decay;

		transform.position = startPos + shakeOffset;
	}

	public void Respawn()
	{
		gameObject.SetActive(false);
		SpriteRenderer.gameObject.SetActive(true);
		Collider.gameObject.SetActive(true);
		gameObject.SetActive(true);
	}

	/// <summary>
	/// Called when the ball hits the block
	/// </summary>
	public void Destroy(Ball from)
	{
		// hide block
		SpriteRenderer.gameObject.SetActive(false);
		Collider.gameObject.SetActive(false);

		// emit particles
		EmitParticles(from.Velocity * BallVelocityIntensity);

		// sfx
		if (manager.BlockSFX)
		{
			if(manager.RandomizePitch)
			{
				audioSource.pitch = 1f + Random.Range(-PitchRange, PitchRange);
			}

			audioSource.PlayOneShot(audioSource.clip);
		}

		manager.Score += Value;
	}

	private void EmitParticles(Vector2 direction)
	{
		if (!manager.BlockParticles) return;

		// set color
		ParticleSystem.MainModule main = ParticleSystem.main;
		main.startColor = SpriteRenderer.color;

		// set collision module
		ParticleSystem.CollisionModule collision = ParticleSystem.collision;
		collision.enabled = manager.BlockParticlesCollide;
		collision.mode = ParticleSystemCollisionMode.Collision2D;
		collision.type = ParticleSystemCollisionType.World;
		collision.bounce = 0.2f;

		// set velocity
		if (manager.BlockParticlesVelocity)
		{
			ParticleSystem.VelocityOverLifetimeModule velocity = ParticleSystem.velocityOverLifetime;
			ParticleSystem.MinMaxCurve x = velocity.x;
			x.constantMin = Mathf.Abs(x.constantMin) * Mathf.Sign(direction.x) + direction.x;
			x.constantMax = Mathf.Abs(x.constantMax) * Mathf.Sign(direction.x) + direction.x;
			ParticleSystem.MinMaxCurve y = velocity.y;
			y.constantMin = Mathf.Abs(y.constantMin) * Mathf.Sign(direction.y) + direction.y;
			y.constantMax = Mathf.Abs(y.constantMax) * Mathf.Sign(direction.y) + direction.y;

			velocity.x = x;
			velocity.y = y;
		}

		ParticleSystem.Emit(Random.Range(MinParticles, MaxParticles));
	}

	public void Shake(float intensity, float decay, float speed)
	{
		shake = new ShakeParams()
		{
			Amp = intensity,
			Phase = 0f,
			Decay = decay,
			Speed = speed
		};
	}
}
