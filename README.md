# KnowledgeRPG

A MonoGame port of the Godot 4.7 / C# game **KnowledgeDungeon** (`godot_rpg`).

KnowledgeRPG is a small top-down RPG slice where **answering an English quiz is
the attack roll**. Explore a forest, touch a slime, and answer a four-choice
question. A correct answer lands a hit; a wrong answer misses but still spends
the turn. Faster answers deal more damage and soften the enemy counterattack.

The port preserves the original gameplay slice rather than rebuilding a
skeleton:

- 30 × 30 tile forest map (480 × 480 px) converted from the Godot TileMapLayer
  and now editable in Tiled (`Content/Maps/forest.tmj`)
- Player and slime sprite animation, four-way facing, attack one-shots, death
- Three roaming slimes with detection, line-of-sight, contact engagement, and
  per-axis collision sliding
- In-map turn-based battles with intro, attack, counterattack, victory, defeat,
  and outro phases
- Quiz-gated attacks with response-time damage scaling
- Persistent HUD: enemy counter, run timer, HP panels, damage numbers
- Dismissible map-clear summary with elapsed time and damage totals
- Defeat fully resets the map, player, enemies, timer, and run stats
- 24-question `forest_level1` grammar pack with a no-repeat shuffle bag

## Requirements

- .NET SDK 9 or newer
- A desktop platform supported by MonoGame DesktopGL (Windows, macOS, Linux)
- The MonoGame content tools, restored through the local tool manifest:

```bash
dotnet tool restore
```

The game targets `net9.0` with `MonoGame.Framework.DesktopGL` and
`MonoGame.Content.Builder.Task` 3.8.x.

## Build and run

```bash
dotnet build KnowledgeRPG.sln
dotnet run --project KnowledgeRPG.csproj
```

The game opens a 1280 × 720 window. Assets are built from
`Content/Content.mgcb`; quiz content is copied from `Data/` to the output
directory.

## Controls

| Context | Input | Action |
| --- | --- | --- |
| Exploring | `WASD` / arrow keys | Move |
| Battle | `Enter` / click | Start the attack quiz |
| Quiz | `1`–`4` | Pick an answer |
| Quiz | Arrow keys + `Enter`, or click | Pick an answer / continue |
| Map cleared | `Enter`, `Escape`, or click | Dismiss the summary |
| Anywhere | `Escape` (outside the quiz) | Quit |

The quiz clock starts when the question appears and stops on the first answer.
Reading the explanation afterwards costs nothing.

## Tests

```bash
dotnet test KnowledgeRPG.sln
```

The xUnit suite covers:

- response-time damage math for player and enemy, including untimed and
  disabled-bonus cases
- quiz pack validation, duplicate IDs, and release-mode invalid-question removal
- shuffle-bag exhaustion/refill and safe pack IDs
- the shipped `forest_level1` pack structure and topic labels
- Tiled map dimensions, full ground coverage, solid tree layer, and GID resolution
- HUD duration formatting

Result: **45 tests passing**.

## Project layout

| Path | Purpose |
| --- | --- |
| `KnowledgeRPGGame.cs` | Game loop, asset loading, camera, world/battle/HUD orchestration |
| `Core/` | Camera, input, rectangle helpers, sprite animation |
| `Maps/` | Tiled map loader (`.tmj`), collision queries, layer rendering |
| `Entities/` | Shared entity movement/animation, player, enemy AI, slime |
| `Battle/` | Battle state machine, HUD, quiz gate, damage calculator |
| `Quiz/` | Pack DTOs, JSON loader, validator, shuffle-bag service, quiz panel |
| `Scenes/` | Map session: run stats, victory counting, clear summary, reset |
| `UI/` | Persistent map HUD and drawing helpers |
| `Data/forest_level1.json` | Shipped quiz pack (24 questions, 2 topics) |
| `Content/Maps/forest.tmj` | Editable Tiled map (JSON format) |
| `Content/` | Sprite sheets, tilesets, and SpriteFont definitions |
| `KnowledgeRPG.Tests/` | xUnit test project |

## Quiz content

Quiz packs are JSON files in `Data/`. A pack needs:

- `schemaVersion: 1`, a lowercase `id`, a title, a language, and a level
- at least one topic with a unique `id` and non-empty `label`
- at least one multiple-choice question with a unique id, a known topic,
  difficulty 1–3, a prompt containing `___`, exactly four unique non-empty
  choices, an answer that matches one choice, and a non-empty explanation

Debug builds throw on invalid content through `QuizPackValidator.ThrowIfInvalid`.
Release builds remove invalid questions and continue through
`QuizPackValidator.RemoveInvalid`.

The map selects its pack through `ForestMap.QuizPackId` (`forest_level1`).
`QuizService` caches loaded packs and serves each question once per shuffle-bag
cycle.

## Editing the map in Tiled

The forest map is **Tiled-authored**; there is no generated tile array anymore.
Open this file in [Tiled](https://www.mapeditor.org/):

```text
Content/Maps/forest.tmj
```

The tilesets are embedded in the map and point at the existing PNGs:

```text
Content/tiles/plains.png    (6 × 12 tiles, 16 px)
Content/tiles/objects.png   (16 × 13 tiles, 16 px)
```

### Layers

| Layer | Purpose |
| --- | --- |
| `Ground` | Visual ground tiles |
| `Trees` | Tree tiles; also blocks movement |

Collision is controlled by a **custom boolean layer property** named `solid`:

1. Select a tile layer.
2. Open **Layer → Layer Properties**.
3. Add a boolean property `solid` and set it to `true`.

Any non-empty cell on a `solid` layer blocks movement. Hidden (`visible = false`)
solid layers still collide, so you can use a separate invisible collision layer
if you prefer to keep visuals and collision independent.

Layers draw in the order shown in Tiled. The current order draws trees under the
player and enemies, matching the original game.

### Map settings

- Orientation: **Orthogonal**
- Tile size: **16 × 16**
- Map size: **30 × 30**
- Format: **JSON** (`.tmj`)
- Tile layer format: **CSV or JSON** (base64, gzip, and zlib saves are also supported)
- Tilesets: **embedded** (external `.tsj` tilesets are not currently loaded)

Map properties:

```text
name        = Forest
quizPackId  = forest_level1
```

Changing `quizPackId` switches which JSON quiz pack that map uses. Changing the
map dimensions also updates camera bounds automatically; keep player/enemy spawn
positions inside the new map.

### After editing

Run the game normally:

```bash
dotnet run --project KnowledgeRPG.csproj
```

The build copies `Content/Maps/forest.tmj` to the output directory. If you use
`--no-build`, the old copied map will still be loaded, so rebuild after edits.

If you paint tiles from a new tileset, add its PNG to `Content/Content.mgcb`, load
it in `KnowledgeRPGGame.LoadContent`, and register it in the `ForestMap` texture
dictionary (`plains` and `objects` are the current entries).

## Port notes

The Godot version relied on engine features that have direct but different
MonoGame equivalents:

| Godot | MonoGame port |
| --- | --- |
| `TileMapLayer` + `PackedByteArray` cells | Tiled `.tmj` map loaded by `TiledMap` |
| `CharacterBody2D` / `move_and_slide` | Manual movement with per-axis tile collision |
| `NavigationAgent2D` | Straight-line steering with collision sliding and line-of-sight checks |
| `AnimationPlayer` / `AnimatedSprite2D` | `SpriteAnimator` with frame clips and one-shot locks |
| Async battle sequences / `Tween` | Frame-based `BattleController` state machine and timed phases |
| Godot `Control` UI | Immediate-mode drawing through `UiPainter` |
| `Camera2D` limits | `Camera2D` clamped to the 480 × 480 map |
| Touch controls | Desktop keyboard/mouse input |

The 640 × 360 virtual view is scaled 2× into the 1280 × 720 window with
point sampling, keeping the original pixel-art look crisp.

## Development hooks

`KRPG_SCREENSHOT` captures the framebuffer after one second, writes a PNG, and
exits. It is useful for smoke-testing rendering in CI or a headless environment:

```bash
KRPG_SCREENSHOT=/tmp/knowledgerpg.png dotnet run --project KnowledgeRPG.csproj
```

## Credits

The player, slime, and forest tileset art come from the original
`godot_rpg` project, which uses the **Mystic Woods** free asset pack by
[Game Endeavor](https://game-endeavor.itch.io/mystic-woods). Refer to that pack
for its usage terms.
