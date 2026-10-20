# Actual generated M1 samples

These are exports of the portable generator, not drawings or campaign saves. Source algorithm/profile/tuning identities are inside every `.galaxy.json`. Each matching `.route.json` comes from the public FindRoute API between its two home candidates. Steps exclude origin; draw origin followed by every returned step. The route total equals the explicit sum of graph edge costs, not distance on screen.

| Seed | Systems / regions | Canonical content SHA256 |
| --- | --- | --- |
| 42 | 40 / 80 | `6e306b02c09344db583fd0871f806e42549e81a06697f53dd124fa1305660a8f` |
| 12345 | 30 / 60 | `85dac90f91180327f0fad3b915ca3145fdd8a1d1108d7b4f543c8c300d655884` |
| 20261020 | 50 / 100 | `c6d6dfcbee54c17607d5090f47ad403f5ba158a7d96efeadcc764da7a2d8b6cd` |

Reproduce with `dotnet run --project src/HobbyDS9.Headless -c Release -- --generated-galaxy 42 40`. Use `--generated-galaxy-route 42 40` for its route fixture, or `--generated-galaxy-open <path>` to validate a saved export. Replace seed/size for other examples. The host reads admitted civilization identities from the existing playable scenario resources; it does not start a live campaign.

Center is the system/star position when SystemId is present; otherwise it is a real empty navigation region's site. Boundaries use clockwise screen coordinates (+Y down) with implicit closure. IDs are scoped to the map's content identity; selecting `nav.generated.001` in a different map does not carry selection/state between maps. Candidate homes are possible starts only, with no owners, contacts, fleets or colonies.

See [the contract](../../generated-galaxy-backend-contract.md). Geometry, half-empty ratio, unit costs and candidate separation are provisional M1 review tuning. No bodies/resources, sensors, exploration progress or responsive AI are supplied.
