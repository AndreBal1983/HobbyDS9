/* Received EngineeringApplications at Backend fd0bb6c; PR85 lineage with admitted utility namespace. Design data only. */
const catalogue = [
  {
    "DefinitionId": "engineering.advanced-starship-engineering",
    "DisplayName": "Advanced Starship Engineering",
    "BaseEngineeringCost": 36000,
    "ScienceRequirements": [
      {
        "Discipline": 3,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 5,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 1,
    "EffectSummary": "Unlocks Advanced Scout production.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.compact-reactor",
    "DisplayName": "Compact Reactor",
    "BaseEngineeringCost": 24000,
    "ScienceRequirements": [
      {
        "Discipline": 3,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 4,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Reactor space 18 → 15 at the same 60 output; credits 6 → 8 and industry 9 → 12. Unlocks component.utility.reactor.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.high-output-reactor",
    "DisplayName": "High-output Reactor",
    "BaseEngineeringCost": 45000,
    "ScienceRequirements": [
      {
        "Discipline": 4,
        "IntervalStartLevel": 2,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.compact-reactor"
    ],
    "Kind": 1,
    "EffectSummary": "Reactor output 60 → 72; space rises to 20, credits 10 / industry 16. Hull distribution still limits usable power. Unlocks component.utility.reactor.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.efficient-drive",
    "DisplayName": "Efficient Drive",
    "BaseEngineeringCost": 27000,
    "ScienceRequirements": [
      {
        "Discipline": 5,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Cruise full/minimum power 30/15 → 24/12 at the same movement 4; credits 2 → 4 / industry 3 → 6. Unlocks component.utility.propulsion.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.high-speed-drive",
    "DisplayName": "High-speed Drive",
    "BaseEngineeringCost": 48000,
    "ScienceRequirements": [
      {
        "Discipline": 5,
        "IntervalStartLevel": 2,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.efficient-drive"
    ],
    "Kind": 1,
    "EffectSummary": "Cruise movement 4 → 5 at full allocation; space 16 → 20, cruise full/minimum power 32/16, credits 6 / industry 10. Unlocks component.utility.propulsion.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.maneuver-drive",
    "DisplayName": "Maneuver Drive",
    "BaseEngineeringCost": 33000,
    "ScienceRequirements": [
      {
        "Discipline": 5,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      },
      {
        "Discipline": 2,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Full-power combat evasion 1000 → 1500 basis points; combat full/minimum power 12/6 → 16/8, credits 5 / industry 8. Unlocks component.utility.propulsion.v4 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.resilient-shield",
    "DisplayName": "Resilient Shield",
    "BaseEngineeringCost": 30000,
    "ScienceRequirements": [
      {
        "Discipline": 4,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 1,
    "EffectSummary": "A new higher-capacity shield variant: full-power capacity 20 → 28; space 12 → 15, full/minimum power 18/9, credits 5 / industry 8. Unlocks component.utility.escort-shield.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.efficient-shield",
    "DisplayName": "Efficient Shield",
    "BaseEngineeringCost": 21000,
    "ScienceRequirements": [
      {
        "Discipline": 4,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Shield full/minimum power 8/4 → 6/3 at capacity 8; credits 2 → 4 / industry 3 → 6. Unlocks component.utility.small-shield.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.multilayer-shield",
    "DisplayName": "Multilayer Shield",
    "BaseEngineeringCost": 51000,
    "ScienceRequirements": [
      {
        "Discipline": 4,
        "IntervalStartLevel": 2,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.resilient-shield"
    ],
    "Kind": 1,
    "EffectSummary": "Full-power shield capacity 28 → 36; space 15 → 18, full/minimum power 22/11, credits 8 / industry 12. Uses existing shield rules, with no new resistance or regeneration mechanic. Unlocks component.utility.escort-shield.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.compact-energy-bank",
    "DisplayName": "Compact Energy Bank",
    "BaseEngineeringCost": 24000,
    "ScienceRequirements": [
      {
        "Discipline": 3,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 6,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Energy-bank space 8 → 6, unchanged firepower and mounts; credits 2 → 4 / industry 3 → 6. Unlocks component.utility.energy-bank.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.long-range-energy-bank",
    "DisplayName": "Long-range Energy Bank",
    "BaseEngineeringCost": 39000,
    "ScienceRequirements": [
      {
        "Discipline": 6,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.compact-energy-bank"
    ],
    "Kind": 1,
    "EffectSummary": "Full-power long-range attack 4 → 7; close attack 12 → 8, full/minimum power 10/5, credits 5 / industry 8. Unlocks component.utility.energy-bank.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.improved-launcher",
    "DisplayName": "Improved Launcher",
    "BaseEngineeringCost": 33000,
    "ScienceRequirements": [
      {
        "Discipline": 6,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 2,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Full-power long-range attack 8 → 11; space 8 → 10, full/minimum power 10/3, credits 5 / industry 8. Retains the campaign v1 aggregate combat model. Unlocks component.utility.launcher.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.compact-life-support",
    "DisplayName": "Compact Life Support",
    "BaseEngineeringCost": 18000,
    "ScienceRequirements": [
      {
        "Discipline": 1,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 3,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Life-support space 12 → 9 at crew support 40; credits 2 → 4 / industry 3 → 6. Unlocks component.utility.life-support.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.efficient-life-support",
    "DisplayName": "Efficient Life Support",
    "BaseEngineeringCost": 30000,
    "ScienceRequirements": [
      {
        "Discipline": 1,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.compact-life-support"
    ],
    "Kind": 2,
    "EffectSummary": "Essential life-support power 6 → 4 in each profile; space 9 → 11, credits 5 / industry 8. Unlocks component.utility.life-support.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.expanded-computing",
    "DisplayName": "Expanded Computing",
    "BaseEngineeringCost": 24000,
    "ScienceRequirements": [
      {
        "Discipline": 2,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Computer support 60 → 90 in all profiles; space 8 → 10, essential power 4 → 5, credits 4 / industry 6. Unlocks component.utility.command.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.precision-fire-control",
    "DisplayName": "Precision Fire Control",
    "BaseEngineeringCost": 39000,
    "ScienceRequirements": [
      {
        "Discipline": 2,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      },
      {
        "Discipline": 6,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.expanded-computing"
    ],
    "Kind": 2,
    "EffectSummary": "Command combat accuracy output 7000 → 8000 basis points; essential power 5 → 7 in all profiles, credits 6 / industry 10. Final accuracy still follows compiler aggregation and caps. Unlocks component.utility.command.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.long-range-sensors",
    "DisplayName": "Long-range Sensors",
    "BaseEngineeringCost": 27000,
    "ScienceRequirements": [
      {
        "Discipline": 2,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 4,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 1,
    "EffectSummary": "Survey sensor rating 10 → 15; space 8 → 10, Survey full/minimum power 10 → 12, credits 4 / industry 7. Sensor rating is a designer preview; no campaign detection bonus is implemented. Unlocks component.utility.survey-sensors.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.compact-laboratories",
    "DisplayName": "Compact Laboratories",
    "BaseEngineeringCost": 27000,
    "ScienceRequirements": [
      {
        "Discipline": 3,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 2,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 1
      }
    ],
    "PrerequisiteDefinitionIds": [],
    "Kind": 2,
    "EffectSummary": "Laboratory space 22 → 18 at Science rating 100; credits 2 → 4 / industry 3 → 7. Science rating remains a designer preview, not colony R&D income. Unlocks component.utility.labs.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.reinforced-hull",
    "DisplayName": "Reinforced Utility Hull",
    "BaseEngineeringCost": 36000,
    "ScienceRequirements": [
      {
        "Discipline": 3,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.advanced-starship-engineering"
    ],
    "Kind": 2,
    "EffectSummary": "New hull version: strength 30 → 40; usable capacity 100 → 95, credits 10 → 14 / industry 20 → 28. Mount architecture unchanged. Unlocks hull.utility.v2 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  },
  {
    "DefinitionId": "engineering.expanded-hull",
    "DisplayName": "Expanded Utility Hull",
    "BaseEngineeringCost": 45000,
    "ScienceRequirements": [
      {
        "Discipline": 3,
        "IntervalStartLevel": 2,
        "RequiredBreakthroughOrdinal": 1
      },
      {
        "Discipline": 4,
        "IntervalStartLevel": 1,
        "RequiredBreakthroughOrdinal": 2
      }
    ],
    "PrerequisiteDefinitionIds": [
      "engineering.advanced-starship-engineering",
      "engineering.high-output-reactor"
    ],
    "Kind": 1,
    "EffectSummary": "New hull version: capacity 100 → 110 and distribution 60 → 72; credits 18 / industry 34 / maintenance 3. Mount architecture unchanged. Unlocks hull.utility.v3 for new designs; existing ships and published revisions are unchanged.",
    "ParameterChange": null
  }
];
