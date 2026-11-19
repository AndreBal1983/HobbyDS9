# Display and acceptance policy

Owner decision: 19 November 2026.

- Remove 1280×720 / 720p as a required project design, native test, screenshot or release-acceptance target.
- Default ongoing UI design and native review target: 1920×1080. This is the working default, not a newly enforced operating-system resolution restriction.
- Smaller windows are best-effort. Do not block a build, create special compact layouts, or spend additional acceptance effort solely to satisfy 720p.
- Retain useful existing resizing/scrolling behavior and historical screenshots/results. Do not delete tests that cover real behavior merely because their fixture uses 1280×720; remove only their status as a compulsory resolution gate. Do not remove a display setting or change sealed packages solely for this policy.
- Wider-screen checks remain relevant where specifically scoped; this decision does not add new mandatory resolutions.

This policy supersedes conflicting 720p requirements in older briefs, art references and acceptance plans. Historical observations remain accurate evidence of what was tested, not future obligations. Active owners must update current receiving checklists accordingly.
