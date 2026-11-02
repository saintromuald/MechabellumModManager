# Top-player content watchlist

`mb-watch` polls high-signal sources twice a day and keeps a de-duplicated
watchlist of **top-level** Mechabellum content: tournament replays that get
converted to native `.grbr`, plus pro/tournament videos and posts.

## Quality gate

The default gate is **Elo ≥ 1800**, which is roughly the in-game top 200
(corpus MMR labels come from the game's own tournament records). On top of
that:

- **Corpus replays** must involve a player at or above the gate, or a name from
  `extra_pros` (community-recognised pros).
- **YouTube** only admits results from a curated list of top-player/caster
  channels **or** videos naming a qualified pro. Tournament keywords raise the
  score but no longer admit unknown channels.
- **Reddit** posts must mention a pro, a tournament, or a replay.
- Videos must be at least 8 minutes and **published in the last 30 days**
  (flat search results have no dates, so metadata is fetched before admission).

Everything is configurable in `~/.config/mbmm/watch.json`:
`min_elo`, `extra_pros`, `trusted_channels`, `event_keywords`,
`youtube_queries`, `min_video_seconds`, `max_age_days`.

## Sources

| Source | What it watches | Action |
| --- | --- | --- |
| `corpus` | qbisi/mechcore-replay, new tournament YAML | convert to `.grbr`, deploy to the game, archive |
| `youtube` | tournament/pro searches | record link, metadata, score |
| `reddit` | r/Mechabellum new posts (RSS) | record link/post |
| `fogmoe` | FogMoe/Mechabellum-Replays releases | record archive assets (dormant until releases exist) |

## De-duplication

- Replays: `BattleID` + SHA-256 in SQLite; existing archive files are skipped.
- Videos/posts: stable IDs (`youtube:<id>`, `reddit:<id>`, `corpus:<BattleID>`)
  in SQLite; an item is admitted once.
- The corpus is baselined on the first run, so the existing archive is not
  re-imported.

## Schedule

A systemd user timer runs **twice daily** (08:00 and 20:00, randomised by up to
20 minutes, catch-up enabled). Unit copies live in `linux/systemd/`:

```bash
systemctl --user list-timers mb-watch.timer
systemctl --user start mb-watch.service     # run now
journalctl --user -u mb-watch.service -n 20
```

## Use

```bash
mb-watch run --notify          # poll all sources now
mb-watch list                  # unwatched items
mb-watch list --all --source youtube
mb-watch seen youtube:abc123   # mark one watched
mb-watch seen --all            # clear the list
mb-watch sources               # last success / error per source
```

Outputs:

- `~/.local/share/mbmm/watch/watchlist.md` — checkboxes, newest/highest score first
- `~/.local/share/mbmm/watch/digest.md` — what the last run added
- `~/.local/share/mbmm/watch/run.log` — history
- `~/.local/share/mbmm/watch.db` — SQLite state

New top-player corpus matches are also converted and copied into
`<game>/ProjectDatas/Replay/`, so they show up in the in-game replay browser
with `replay-reset` / `replay-tool` without any extra step.

## Limits

- There is no public MMR API; the in-game leaderboard is client-side. The Elo
  gate uses the game's own labels attached to tournament replays, which only
  covers players who appear in those events.
- "Top 200" is therefore an approximation (Elo ≥ 1800 plus a curated pro list),
  not a live official ranking. Adjust `min_elo` / `extra_pros` if you disagree.
- Reddit may rate-limit (HTTP 429); the watcher retries with backoff and records
  the error in `mb-watch sources`.
- YouTube search ordering changes; new IDs are admitted once and old uploads are
  rejected by the recency gate.
