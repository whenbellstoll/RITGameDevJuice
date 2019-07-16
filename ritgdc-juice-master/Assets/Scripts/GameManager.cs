using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[Header("Juice Toggle")]
	public bool PaddleLerp;
	public bool PaddleSquish;
	public bool RotateBall;
	public bool BallSquish;
	public bool EaseInBlocks;
	public bool ShakeBlocks;
	public bool ShakeCamera;
	public bool CameraFollow;
	public bool BallParticles;
	public bool BlockParticles;
	public bool BlockParticlesCollide;
	public bool BlockParticlesVelocity;
	public bool PaddleSFX;
	public bool BallSFX;
	public bool BlockSFX;
	public bool RandomizePitch;
	public bool Music;

	[Header("Field")]
	public float FieldWidth;
	public Vector2 BlockSize;
	public int BlockColumns;
	public int BlockRows;
	public Vector2 BlockGridPadding;

	[Header("Color")]
	public PaletteType ActivePalette;
	public Palette NoColor;
	public Palette ColoredBlocks;

	[Header("Animation")]
	public ShakeParams BlockShake;
	public ShakeParams CameraShake;

	[Header("Prefabs")]
	public Block BlockPrefab;
	public Ball BallPrefab;

	[Header("Scene References")]
	public Camera MainCamera;
	public Camera MouseCamera;
	public Paddle Paddle;
	public Transform BallSpawn;
	public GameObject[] Walls;
	public AudioSource MusicSource;

	[HideInInspector]
	public List<Block> Blocks;
	public List<Ball> Balls;

	[HideInInspector]
	public int Score;

	[System.Serializable]
	public struct Palette
	{
		public Color Ball;
		public Color Paddle;
		public Color Walls;
		public Color Blocks;
		public Color Background;
	}

	[System.Serializable]
	public struct ShakeParams
	{
		[Range(0f, 1f)]
		public float Intensity;

		[Range(0f, 1f)]
		public float Decay;

		public float Speed;
	}

	public enum PaletteType
	{
		NoColor,
		Color,
		BlockHues
	}

	private PaletteType currentPalette = PaletteType.NoColor;

	/// <summary>
	/// Called when the ball hits something
	/// </summary>
	public void OnBallHit(Vector2 direction)
	{
		if (ShakeBlocks)
		{
			// shake blocks
			for (int i = 0; i < Blocks.Count; i++)
			{
				Blocks[i].Shake(
					intensity: BlockShake.Intensity,
					decay: BlockShake.Decay,
					speed: BlockShake.Speed
				);
			}
		}

		if (ShakeCamera)
		{
			// shake camera
			MainCamera.GetComponent<CameraManager>().Shake(
				intensity: CameraShake.Intensity,
				speed: CameraShake.Speed,
				decay: CameraShake.Decay,
				direction: -direction // shake against ball direction (as if it moved the field with its hit)
			);
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Debug.LogError("[GameManager] More than one instance of " +
				"GameManager in the scene! Destroying self...");
			DestroyImmediate(this);
			return;
		}

		BuildField();
	}

	private void BuildField()
	{
		// make blocks
		Blocks = new List<Block>();
		Vector2 top = MainCamera.ViewportToWorldPoint(
			new Vector3(
				x: 0.5f, 
				y: 1f,
				z: -MainCamera.transform.position.z
			)
		);
		Vector2 blockOffset = BlockSize + BlockGridPadding;
		Vector2 gridSize = new Vector2(BlockColumns - 1, BlockRows - 1) * blockOffset;
		Vector2 startPos = top - new Vector2(gridSize.x / 2f, BlockSize.y * 2f);
		for (int r = 0; r < BlockRows; r++)
		{
			for (int c = 0; c < BlockColumns; c++)
			{
				Block b = MakeBlock(
						position: startPos + new Vector2(c, -r) * blockOffset,
						size: BlockSize
				);
				b.EaseInDelay = Blocks.Count * 0.01f;
				Blocks.Add(b);
				b.Respawn(); // restart ease
			}
		}

		AddBall();
		SetPalette(ActivePalette);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			ResetBlocks();
		}

		if (currentPalette != ActivePalette)
		{
			SetPalette(ActivePalette);
		}

		MusicSource.mute = !Music;
	}

	/// <summary>
	/// Bring blocks back to life
	/// </summary>
	private void ResetBlocks()
	{
		for (int i = 0; i < Blocks.Count; i++)
		{
			Blocks[i].Respawn();
		}
	}

	private void SetPalette(PaletteType t)
	{
		// select palette
		Palette palette = default;
		switch (t)
		{
			case PaletteType.NoColor:
				palette = NoColor;
				break;
			case PaletteType.BlockHues:
			case PaletteType.Color:
				palette = ColoredBlocks;
				break;
		}

		// balls
		for (int i = 0; i < Balls.Count; i++)
		{
			Balls[i].SpriteRenderer.color = palette.Ball;
		}

		// blocks
		Color blockColor = palette.Blocks;
		for (int i = 0; i < Blocks.Count; i++)
		{
			if (t == PaletteType.BlockHues && i % BlockColumns == 0) // color blocks by row
			{
				// shift hue a bit
				float h = 0f;
				float s = 0f;
				float v = 0f;
				Color.RGBToHSV(blockColor, out h, out s, out v);
				h = (h + 0.05f) % 1f;
				blockColor = Color.HSVToRGB(h, s, v);
			}

			Blocks[i].SpriteRenderer.color = blockColor;
		}

		// walls
		for (int i = 0; i < Walls.Length; i++)
		{
			Transform wall = Walls[i].transform;
			wall.GetComponentInChildren<SpriteRenderer>().color = palette.Walls;
		}

		// paddle
		Paddle.SpriteRenderer.color = palette.Paddle;

		// background
		MainCamera.backgroundColor = palette.Background;

		currentPalette = t;
	}

	private void AddBall()
	{
		Balls.Add(MakeBall(BallSpawn.position));
	}

	private T MakeObject<T>(T prefab, Vector2 position, Vector3 size)
		where T : Component
	{
		// instantiate prefab
		T obj = Instantiate(prefab);

		// set position
		obj.transform.position = position;

		// set size of sprite and collider 
		// (this is a bit dirty but it's an example, not production code :^) )
		Transform sprite = obj.transform.Find("Sprite");
		Transform collider = obj.transform.Find("Collider");
		sprite.transform.localScale = collider.transform.localScale = size;

		return obj;
	}

	private Block MakeBlock(Vector2 position, Vector3 size) =>
		MakeObject(BlockPrefab, position, size);

	private Ball MakeBall(Vector2 position) =>
		MakeObject(BallPrefab, position, Vector3.one * BallPrefab.Size);
}
