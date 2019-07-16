using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ball : MonoBehaviour
{
	public float Size = 0.3f;
	public float Speed = 1f;

	[Range(1f, 10f)]
	public float SquishIntensity;

	[Range(0.01f, 1f)]
	public float SquishReturnSpeed;

	[Range(0f, 1f)]
	public float StretchIntensity;

	public float PitchRange;

	public SpriteRenderer SpriteRenderer;
	public CircleCollider2D Collider;
	public ParticleSystem ParticleSystem;

	public Rigidbody2D Body { get; private set; }

	public Vector2 Velocity { get; private set; }

	private GameManager manager => GameManager.Instance;
	private AudioSource audioSource;

	private void Awake()
	{
		// set dimensions
		SpriteRenderer.transform.localScale = Vector3.one * Size;
		Collider.radius = Size / 2f;

		// using a rigidbody just to get collision events becuase I'm lazy
		Body = GetComponent<Rigidbody2D>();

		audioSource = GetComponent<AudioSource>();

		Velocity = -Vector2.one * Speed;
	}

	private void FixedUpdate()
	{
		transform.position += (Vector3)Velocity * Time.fixedDeltaTime;
	}

	private void Update()
	{
		if (manager.RotateBall)
			transform.rotation = Quaternion.LookRotation(Vector3.forward, Velocity);
		else
			transform.rotation = Quaternion.Euler(Vector3.zero);

		// return from squish
		transform.localScale += (Vector3.one - transform.localScale) * SquishReturnSpeed;
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		// check if colliding with block
		Block block = collision.collider.GetComponentInParent<Block>();
		if (block)
		{
			block.Destroy(this);
		}

		Paddle paddle = collision.collider.GetComponentInParent<Paddle>();
		if (paddle)
		{
			paddle.OnBallHit();
		}

		manager.OnBallHit(Velocity.normalized);

		// squash and stretch animation
		Squish();

		// reflect velocity across surface normal
		Velocity = Vector2.Reflect(
			inDirection: Velocity,
			inNormal: collision.GetContact(0).normal
		);

		// surface normals can be a bit unreliable in unity, 
		// preserve 45 degree direction and speed after reflect
		Velocity = new Vector2(
			x: Mathf.Sign(Velocity.x),
			y: Mathf.Sign(Velocity.y)
		) * Speed;

		// rotate before particles
		if (manager.RotateBall)
			transform.rotation = Quaternion.LookRotation(Vector3.forward, Velocity);

		// dust particles
		if (manager.BallParticles)
			ParticleSystem.Emit(5);

		// sfx
		if (manager.BallSFX)
		{
			if(manager.RandomizePitch)
			{
				audioSource.pitch = 1f + Random.Range(-PitchRange, PitchRange);
			}

			audioSource.PlayOneShot(audioSource.clip);
		}
	}

	private void Squish()
	{
		if (!manager.BallSquish) { return; }

		// squash and stretch animation for ball
		Vector3 scale = Vector3.one;

		// squish along X, inverse along Y to preserve volume
		scale.x = 1f / SquishIntensity;
		scale.y = StretchIntensity / scale.x;

		transform.localScale = scale;
	}
}
