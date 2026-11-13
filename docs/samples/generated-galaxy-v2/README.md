# Scattered-region M1 correction: actual v2 samples

Owner feedback on20October2026 requested more pronounced region irregularity because v1's jittered grid still looked square. These exports use the portable **irregular-regions-v2** generator and **provisional-scattered-sites-30-40-50-v2** tuning. Format1/profile and DTO shapes remain unchanged. Sites are sampled without grid slots, with bounded minimum spacing; the same exact Voronoi clipping/shared-edge validation supplies the real graph.

| Seed | Systems / regions | Canonical content SHA256 |
| --- | --- | --- |
| 42 | 40 / 80 | `b7f6c3041a330eac4dee6cc2e2e1ad3cfb0e6b77d3c81351835d6a1e6cf0938e` |
| 43 | 40 / 80 | `6f2ae2943bb9dd9e69425dbe5956cbca93958e8e3837d8958414c12aa17b3a9c` |
| 12345 | 30 / 60 | `e86eed4273cfd59419844a6bfb83ccbc86d59b5d825a5306b74bb924c072e781` |
| 20261020 | 50 / 100 | `25a1cc65053574d71648097c6a5d97e4832e5d9112ebb33e51aae71a20b84a25` |

Each `.galaxy.json` is actual generated authority with a matched `.route.json` from FindRoute between its candidate homes. Draw origin plus returned steps; costs come from graph edges. Compare against [retained v1 samples](../generated-galaxy-v1/README.md) using identical framing and styling. The extra original43/40 comparison is retained in `E:/HobbyDS9/artifacts/generated-galaxy-backend-771a3de-20261020/` and Art's frozen first handoff; its canonical hash is `8339df2ce3569c60407620a83a604bd73046bdd1da1c1c8749055247c3c9a46a`.

Reproduce with `dotnet run --project src/HobbyDS9.Headless -c Release -- --generated-galaxy 42 40` and `--generated-galaxy-route 42 40`. `--generated-galaxy-open <path>` accepts either exact supported version/tuning pair and preserves that map's identities, geometry and digest. Unsupported or mixed version pairs reject; new generation never rewrites old exports.

Half the regions are genuinely empty. Candidate homes remain candidates, not owners. Extents/axes, normal unit edge costs, home-separation policy, admitted civilization definitions and campaign isolation are unchanged. Placement spacing/margins/density remain **provisional review tuning**. No sensors, exploration progression, responsive AI, campaign activation or M2 work.
