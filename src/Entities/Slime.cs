using Microsoft.Xna.Framework;
using KnowledgeRPG.Core;

namespace KnowledgeRPG.Entities
{
    public sealed class Slime : Enemy
    {
        public Slime(SpriteAnimator sprite)
            : base(sprite, new Vector2(16f, 13f), new Vector2(0f, 1.5f))
        {
            DisplayName = "Slime";
            Speed = 25f;
            MaxHp = 30;
            SetHp(MaxHp);

            // Sheet layout: 32x32 frames, seven columns.
            sprite.Add("front_idle", 0, 4, 5f);
            sprite.Add("side_idle", 1, 4, 5f);
            sprite.Add("back_idle", 2, 4, 5f);
            sprite.Add("front_walk", 3, 6, 5f);
            sprite.Add("side_walk", 4, 6, 5f);
            sprite.Add("back_walk", 5, 6, 5f);
            sprite.Add("front_attack", 6, 7, 5f, loop: false);
            sprite.Add("side_attack", 7, 7, 5f, loop: false);
            sprite.Add("back_attack", 8, 7, 5f, loop: false);
            sprite.Add("death", 12, 5, 7f, loop: false);

            sprite.Play("front_idle");
        }
    }
}
