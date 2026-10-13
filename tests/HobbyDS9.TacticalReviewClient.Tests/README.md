# Actual Application review-client tests

**Real gated review sessions; production activation disabled.**

Read [the integration evidence and limitations](../../docs/tactical-review-client-integration.md). This project uses the authored package through ReviewHost and actual CampaignTacticalReviewApplication factories. No mocked session, manually constructed observation/receipt or toy authority is substituted.

51 cases cover fleet menus, real chronology, own stocks, lawful enemy views, cloak, same-fleet screening/focus fallback, private JSON checkpoints and rejected/obsolete callbacks. These are separate from the preserved 115 mock tests. Package variants test the proposed pinned rules.

Run dotnet restore/build/test against this csproj. It is outside the main solution and uses dedicated portable CI. No Unity/native/physical-travel/durable-file acceptance is implied.
