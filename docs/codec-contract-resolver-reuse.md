# Codec contract resolver reuse

The private required-scalar contract resolver is shared across codec instances. It retains only Json.NET type/property contracts; each operation still creates fresh settings, serializers and readers. Strict duplicate/unknown/required/type/closed-carrier checks, safe errors and limits are unchanged. No decoded DTO, session, validity certificate or content is cached.

Concurrent mixed legacy physical, Format11 and Format12 round trips exercise detached roots and independent sessions; warm malformed nested scalar, null, type, unknown and duplicate inputs remain fail closed. Exact external Human49 codec-only proof is recorded separately, without replay or a permanent private fixture dependency. This is source/CPU correctness work, with no timing or native improvement claim.
