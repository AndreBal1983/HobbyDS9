#nullable enable
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace HobbyDS9.UnityClient
{
    public static class ShipDesignFixtureLoader
    {
        public static FixtureShipDesignPreviewProvider Load()
        {
            var asset=Resources.Load<TextAsset>("ship-design-fixture") ?? throw new InvalidOperationException("Run PrepareUnity to copy the ship-design fixture.");
            var fixture=JsonConvert.DeserializeObject<DesignerFixture>(asset.text,new JsonSerializerSettings
            {
                MissingMemberHandling=MissingMemberHandling.Error, TypeNameHandling=TypeNameHandling.None, MaxDepth=128
            }) ?? throw new ArgumentException("Empty ship-design fixture.");
            return new FixtureShipDesignPreviewProvider(fixture);
        }
    }
}
