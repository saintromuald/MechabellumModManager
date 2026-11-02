# Watching Mechabellum replays on Linux

Three different problems, three different tools.

## 1. Your own replays, in the game

The game re-simulates a match from its recorded decisions, so a replay is only
as good as the ruleset it was recorded on: a balance patch can mark old replays
expired or make them diverge. Keep that in mind before relying on the in-game
list as an archive.

- Enable **Settings → Other → Auto-save replays**. Files land in
  `<game>/ProjectDatas/Replay/*.grbr` (and version folders).
- Two catalog mods make the in-game replay browser usable:
  - `replay-reset` — rebuilds the replay list with search, custom categories,
    pins, rename, date groups and 1v1 / 2v2 / 4-FFA filters.
  - `replay-tool` — enables the lab tools while watching a replay.
- Both are installed in the `default` profile:

  ```bash
  mbmm list | grep replay
  mbmm install replay-reset replay-tool   # already done here
  ```

Expired-replay cleanup still follows the game's own rules; the mod only
organizes what is still on disk.

## 2. The archive: cross-patch replays that still play

The game client cannot reliably play a replay from another build. The way
around that is **mechcore**, a deterministic simulator with a standalone HTML
battle player. It reads *match documents* (YAML), not `.grbr`, so playback does
not depend on the installed game version.

- Source: <https://github.com/qbisi/mechcore>
- Corpus: <https://github.com/qbisi/mechcore-replay>
- Player: `mechcore play <match.yaml> [page.html] [--no-open]` writes one
  self-contained HTML file. Space pauses, the timeline seeks, the wheel zooms,
  and the download button exports the current frame as SVG.

### The corpus

`mechcore-replay/tournaments/` is downloaded from the game's **own tournament
servers**, grouped by date and region. At the time of writing: **1,503 matches**
across 9 tournament days (2026-10-24 → 2026-11-01), all pinned to build
`2.0.0.5.2340` — the same build installed here. Headers carry player names and
Elo, so you can filter for the top of the ladder (the highest in the archive is
2,488).

```bash
mb-replay sync                                   # clone / update corpus
mb-replay list --region europe --min-elo 2000
mb-replay list --player Mathismight --limit 10
mb-replay info 20261101--1714
mb-replay play 20261101--1714                    # opens the HTML player
mb-replay random --min-elo 2200
```

`mb-replay` lives next to `mbmm` and expects the built binary at
`~/Github/mechcore/target/release/mechcore` (override with `MECHCORE_BIN`).

Build mechcore once:

```bash
git clone --depth 1 https://github.com/qbisi/mechcore
cd mechcore
cargo build --release -p mechcore --no-default-features
```

### Collecting new tournament replays

`scripts/corpus/tournament-replays.py` talks to the game through mechcore's
adapter and downloads every subdivision and ranking page of a tournament. It
needs the game running online; `list` and `download` do not work offline. The
result is the same `tournaments/<date>-<region>/<BattleID>.yaml` layout, and
`download --publish` / `publish --source` pushes it to the corpus.

### Exporting into the game's replay folder

`mb-replay export` converts match documents back into native `.grbr` replays
(the same transcode `mechcore convert --to grbr` performs), keeps a standalone
copy with an index, and deploys it to the game's replay folder:

```bash
mb-replay export --top-players 100          # 440 matches, top-100 by corpus Elo
mb-replay export --min-elo 2000 --region europe
mb-replay export --date 2026-11-01 --limit 50 --no-deploy
```

- Standalone archive: `~/.local/share/mbmm/replays/` (`.grbr` + `index.tsv`)
- In-game: `<game>/ProjectDatas/Replay/`, which is what the official replay
  list and the `replay-reset` / `replay-tool` mods read.
- The `.grbr` files also work with the RhinoWaspCult analyzer (drag and drop)
  and the BigPhysh viewer.

`--top-players N` ranks players by their best Elo **in the corpus**, not the
global ladder, so it means "top N among the players in these tournaments".

## 3. Other community replay tools

| Tool | What it does | Link |
| --- | --- | --- |
| mechcore | Simulator + offline HTML battle player, match conversion, verification | <https://github.com/qbisi/mechcore> |
| mechcore-replay | 1,500+ tournament match documents + collector scripts | <https://github.com/qbisi/mechcore-replay> |
| FogMoe replay corpus | Community-contributed `.grbr` files, archived by version | <https://github.com/FogMoe/Mechabellum-Replays> |
| RhinoWaspCult Replay Analyzer | Browser-local `.grbr` stats, match breakdown, tournament leaderboards | <https://rhinowaspcult.com/tools/replay-analyzer/> |
| BigPhysh viewer | Per-round plots + GIF from `.grbr` (up to 6 rounds) | <https://github.com/BigPhysh/mechabellum-replays> |
| ShotgunCrocodile parser | Per-round action table from `.grbr` | <https://github.com/ShotgunCrocodile/mechabellum_replay_parser> |
| IcyIcyD parser | Minimal `.grbr` parser | <https://github.com/IcyIcyD/MechabellumReplayParser> |

The FogMoe corpus accepts `.grbr` uploads through a GitHub issue form and stores
them per version; it is the practical place to preserve your own old replays.
The RhinoWaspCult analyzer reads files locally in the browser (nothing uploaded)
and is the quickest way to get stats out of a replay you already have.

## 4. Pro games and VODs

- The in-game tournament replays are the same events the corpus collects:
  weekly tournaments in Asia, Europe and the Americas.
- **Mechacademy** (community coach/student league) publishes a media hub of
  casted matches and coaching VODs tagged `#mechacademy`:
  <https://mecha.academy/media>. Its replay library is the intended learning
  resource for the season.
- RhinoWaspCult keeps tournament leaderboards and per-player pages:
  <https://rhinowaspcult.com/tools/replay-analyzer/tournaments/>.

## 5. Limits

- `mechcore convert --to match` accepts the **current build** and standard 1v1;
  older-version or 2v2 `.grbr` inputs are refused rather than half-converted.
  Archived match YAML in the corpus is already converted and plays fine.
- The simulator plays the rules it carries; a match recorded on another build
  warns and can diverge. The corpus pins the build in every header for that
  reason.
- Downloading new tournament replays needs an online game session; the local
  player does not.
