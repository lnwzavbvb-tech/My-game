using UnityEngine;

namespace CounterArena
{
    public sealed class PixelActor : MonoBehaviour
    {
        public Sprite Idle, Attack, Guard, Hurt;
        public bool Hunter;
        SpriteRenderer spriteRenderer;
        Camera view;
        ArenaGame game;
        float elapsed;
        string pose = "idle";
        float progress = 1;
        void Awake() { spriteRenderer = GetComponent<SpriteRenderer>(); view = Camera.main; game = FindAnyObjectByType<ArenaGame>(); }
        public void Pose(string name, float p) { pose = name; progress = p; }
        public void ResetPresentation()
        {
            pose = "idle"; progress = 1; elapsed = 0;
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = Idle; spriteRenderer.color = Color.white;
            transform.localPosition = new Vector3(0, .025f, 0);
        }
        void LateUpdate()
        {
            if (!view) return;
            if (game && game.Paused) return;
            elapsed += Time.deltaTime;
            transform.rotation = view.transform.rotation;
            bool active = progress < 1;
            Sprite frame = Idle;
            if (active)
            {
                if ((pose == "guard" || pose == "parry") && Guard) frame = Guard;
                else if (pose == "hurt" && Hurt) frame = Hurt;
                else if (pose != "dash" && Attack) frame = Attack;
            }
            spriteRenderer.sprite = frame;
            float bob = Mathf.Floor(Mathf.Sin(elapsed * 3) * 1.5f) / 64f;
            transform.localPosition = new Vector3(0, .025f + bob, 0);
            spriteRenderer.color = active && pose == "hurt" ? new Color(1, .6f, .65f) : Color.white;
        }
    }
}
